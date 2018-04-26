using System;
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
	internal delegate void HostileTargetAquiredDelegate(Shooter shooter, RangeTargetComponent target);
	internal delegate void ShotReloadedDelegate(Shooter shooter);
	internal delegate void ShotFiredDelegate(Shooter shooter, Projectile projectile);

	public class Shooter : DefaultComponent, RangeTargetComponent.IShooter
	{

		public interface INotificationReceiver {
			void OnTargetAcquired(Shooter shooter, RangeTargetComponent target);

			void BeforeShotFired(Shooter shooter, IRangeTarget target);

			void AfterShotFired(Shooter shooter, Projectile projectile);

			void OnShotReloaded(Shooter shooter);

			Vector3 GetSourceOffset(IRangeTarget target);
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

		internal event HostileTargetAquiredDelegate OnTargetAcquired;
		internal event ShotReloadedDelegate OnShotReloaded;
		internal event ShotFiredDelegate OnShotFired;


		private IRangeTarget target;

		private float shotDelay;
		private float searchDelay;

		private ProjectileType projectileType;


		private Entity entity;

		private INotificationReceiver notificationReceiver;

		private IPlayer Player => entity.Player;

		private ILevelManager Level => entity.Level;

		private Map Map => Level.Map;

		private int entityID;

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

		protected Shooter(	ILevelManager level,
							int entityID,
							INotificationReceiver notificationReceiver,
							ProjectileType projectileType,
							float rateOfFire)
			:this(level, notificationReceiver, projectileType, rateOfFire)
		{
			this.entityID = entityID;
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
			sequentialDataReader.MoveNext();
			var entityID = sequentialDataReader.GetCurrent<int>();
			sequentialDataReader.MoveNext();
			var rateOfFire = sequentialDataReader.GetCurrent<float>();
			sequentialDataReader.MoveNext();
			var projectileTypeID = sequentialDataReader.GetCurrent<int>();

			var shooter = new Shooter(level,
									entityID,
									notificationReceiver,
									level.PackageManager.ActiveGame.GetProjectileType(projectileTypeID),
									rateOfFire);

			return shooter;
		}

		internal override void ConnectReferences(ILevelManager level) {
			entity = level.GetEntity(entityID);
		}

		public override PluginData SaveState() {
			var sequentialData = new SequentialPluginDataWriter();

			sequentialData.StoreNext<int>(entity.ID);
			sequentialData.StoreNext<float>(RateOfFire);
			sequentialData.StoreNext<int>(projectileType.ID);

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

			target = newTarget;
			newTarget.AddShooter(this);
			return true;
		}

		public void StopShooting() {
			target?.RemoveShooter(this);
			target = null;
		}

		public void OnTargetDestroy(IRangeTarget target) {
			Debug.Assert(this.target == target);
			this.target = null;
		}

		protected override void OnDeleted() {
			target?.RemoveShooter(this);
		}

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);
			if (!EnabledEffective) return;


			if (shotDelay > 0) {
				shotDelay -= timeStep;
				return;
			}

			OnShotReloaded?.Invoke(this);

			if (SearchForTarget && target == null && searchDelay < 0) {
				searchDelay = TargetSearchDelay;

				//Check for target in range
				var possibleTargets = Player.GetEnemyPlayers()
											.SelectMany(enemy => enemy.GetAllUnits())
											//.AsParallel()
											.Where(unit => projectileType.IsInRange(entity.Position, unit.GetDefaultComponent<RangeTargetComponent>()))
											.OrderBy(unit => Vector3.Distance(entity.Position, unit.Position));


				foreach (var possibleTarget in possibleTargets) {

					var newTarget = possibleTarget.GetDefaultComponent<RangeTargetComponent>();

					target = newTarget;
					target.AddShooter(this);
					OnTargetAcquired?.Invoke(this, newTarget);
					break;
				}

			}
			else if (searchDelay >= 0){
				searchDelay -= timeStep;
				return;
			}

			if (target == null) {
				return;
			}

			var projectile = Level.SpawnProjectile(projectileType, entity.Position + notificationReceiver.GetSourceOffset(target), Player, target);
			//Could not fire on the target
			if (projectile == null) {
				target.RemoveShooter(this);
				target = null;
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
