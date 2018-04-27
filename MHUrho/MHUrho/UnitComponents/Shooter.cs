﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using MHUrho.Helpers;
using MHUrho.Plugins;
using MHUrho.WorldMap;
using Urho.Physics;

namespace MHUrho.UnitComponents
{
	internal delegate void HostileTargetAquiredDelegate(Shooter shooter);
	internal delegate void ShotReloadedDelegate(Shooter shooter);
	internal delegate void ShotFiredDelegate(Shooter shooter, Projectile projectile);

	public class Shooter : DefaultComponent, RangeTargetComponent.IShooter
	{

		public interface INotificationReceiver {
			void OnTargetAcquired(Shooter shooter);

			void BeforeShotFired(Shooter shooter);

			void AfterShotFired(Shooter shooter, Projectile projectile);

			void OnShotReloaded(Shooter shooter);

			Vector3 GetSourceOffset(Shooter shooter);
		}

		public static string ComponentName = nameof(Shooter);
		public static DefaultComponents ComponentID = DefaultComponents.Shooter;
		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		/// <summary>
		/// Shots per minute
		/// </summary>
		public float RateOfFire { get; set; }

		public bool SearchForTarget { get; set; }

		public float TargetSearchDelay { get; set; }

		public IRangeTarget Target { get; private set; }

		internal event HostileTargetAquiredDelegate OnTargetAcquired;
		internal event ShotReloadedDelegate OnShotReloaded;
		internal event ShotFiredDelegate OnShotFired;


		

		float shotDelay;
		float searchDelay;

		ProjectileType projectileType;


		Entity entity;

		INotificationReceiver notificationReceiver;

		IPlayer Player => entity.Player;

		ILevelManager Level => entity.Level;

		Map Map => Level.Map;

		protected Shooter(ILevelManager level,
						INotificationReceiver notificationReceiver,
						ProjectileType projectileType,
						float rateOfFire) {
			this.notificationReceiver = notificationReceiver;


			this.projectileType = projectileType;
			this.RateOfFire = rateOfFire;
			this.shotDelay = 60 / RateOfFire;
			this.searchDelay = 0;
			ReceiveSceneUpdates = true;
		}

		public static Shooter CreateNew<T>(T instancePlugin, 
										   ILevelManager level,
										   ProjectileType projectileType,
										   float rateOfFire)
			where T : InstancePluginBase, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new Shooter(level, 
							   instancePlugin, 
							   projectileType, 
							   rateOfFire);
		}

		internal static Shooter Load(ILevelManager level, InstancePluginBase plugin, PluginData storedData) {
			var notificationReceiver = plugin as INotificationReceiver;
			if (notificationReceiver == null) {
				throw new
					ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
			}

			var sequentialDataReader = new SequentialPluginDataReader(storedData);
			var rateOfFire = sequentialDataReader.GetNext<float>();
			var projectileTypeID = sequentialDataReader.GetNext<int>();

			var shooter = new Shooter(level,
									notificationReceiver,
									level.PackageManager.ActiveGame.GetProjectileType(projectileTypeID),
									rateOfFire);

			shooter.SearchForTarget = sequentialDataReader.GetNext<bool>();
			shooter.TargetSearchDelay = sequentialDataReader.GetNext<float>();
			


			return shooter;
		}

		internal override void ConnectReferences(ILevelManager level) {

		}

		public override PluginData SaveState() {
			var sequentialData = new SequentialPluginDataWriter();

			sequentialData.StoreNext<float>(RateOfFire);
			sequentialData.StoreNext<int>(projectileType.ID);
			sequentialData.StoreNext<bool>(SearchForTarget);
			sequentialData.StoreNext<float>(TargetSearchDelay);
			sequentialData.StoreNext<int>(Target.InstanceID);
			sequentialData.StoreNext<float>(shotDelay);
			sequentialData.StoreNext<float>(searchDelay);

			return sequentialData.PluginData;
		}

		public override void OnAttachedToNode(Node node) {
			base.OnAttachedToNode(node);

			entity = node.GetComponent<Entity>();

			OnShotFired += notificationReceiver.AfterShotFired;
			OnTargetAcquired += notificationReceiver.OnTargetAcquired;
			OnShotReloaded += notificationReceiver.OnShotReloaded;
		}

		public bool ShootAt(IRangeTarget newTarget) {
			StopShooting();

			if (!projectileType.IsInRange(entity.Position, newTarget)) return false;

			Target = newTarget;
			newTarget.AddShooter(this);
			return true;
		}

		public void StopShooting() {
			Target?.RemoveShooter(this);
			Target = null;
		}

		public void OnTargetDestroy(IRangeTarget target) {
			Debug.Assert(this.Target == target);
			this.Target = null;
		}

		protected override void OnDeleted() {
			Target?.RemoveShooter(this);
			base.OnDeleted();
		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);

			if (!EnabledEffective) return;


			if (shotDelay > 0) {
				shotDelay -= timeStep;
				return;
			}

			OnShotReloaded?.Invoke(this);

			if (SearchForTarget && Target == null && searchDelay < 0) {
				searchDelay = TargetSearchDelay;

				//Check for target in range
				var possibleTargets = Player.GetEnemyPlayers()
											.SelectMany(enemy => enemy.GetAllUnits())
											//.AsParallel()
											.Where(unit => projectileType.IsInRange(entity.Position, unit.GetDefaultComponent<RangeTargetComponent>()))
											.OrderBy(unit => Vector3.Distance(entity.Position, unit.Position));


				foreach (var possibleTarget in possibleTargets) {

					var newTarget = possibleTarget.GetDefaultComponent<RangeTargetComponent>();

					Target = newTarget;
					Target.AddShooter(this);
					OnTargetAcquired?.Invoke(this);
					break;
				}

			}
			else if (searchDelay >= 0){
				searchDelay -= timeStep;
				return;
			}

			if (Target == null) {
				return;
			}
			//TODO: Delegate
			notificationReceiver.BeforeShotFired(this);

			var projectile = Level.SpawnProjectile(projectileType, entity.Position + notificationReceiver.GetSourceOffset(this), Player, Target);
			//Could not fire on the target
			if (projectile == null) {
				Target.RemoveShooter(this);
				Target = null;
				return;
			}

			OnShotFired?.Invoke(this, projectile);

			shotDelay = 60 / RateOfFire;

		}


		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			AddedToEntity(typeof(Shooter), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			return RemovedFromEntity(typeof(Shooter), entityDefaultComponents);
		}

	}
}
