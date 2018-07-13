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
	public delegate void TargetAquiredDelegate(Shooter shooter);
	public delegate void ShotReloadedDelegate(Shooter shooter);
	public delegate void BeforeShotFiredDelegate(Shooter shooter);
	public delegate void TargetLostDelegate(Shooter shooter, IRangeTarget target);
	public delegate void TargetDestroyedDelegate(Shooter shooter, IRangeTarget target);
	public delegate void ShotFiredDelegate(Shooter shooter, IProjectile projectile);
	public delegate Vector3 GetSourceOffsetDelegate(Shooter shooter);

	public class Shooter : DefaultComponent, RangeTargetComponent.IShooter
	{
		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => Shooter;

			public Shooter Shooter { get; private set; }

			int targetID;

			public Loader()
			{

			}

			public static PluginData SaveState(Shooter shooter)
			{
				var sequentialData = new SequentialPluginDataWriter(shooter.Level);

				sequentialData.StoreNext<float>(shooter.RateOfFire);
				sequentialData.StoreNext<int>(shooter.projectileType.ID);
				sequentialData.StoreNext<bool>(shooter.SearchForTarget);
				sequentialData.StoreNext<float>(shooter.TargetSearchDelay);
				sequentialData.StoreNext<float>(shooter.shotDelay);
				sequentialData.StoreNext<float>(shooter.searchDelay);
				sequentialData.StoreNext<int>(shooter.Target?.InstanceID ?? 0);
				sequentialData.StoreNext<bool>(shooter.Enabled);
				

				return sequentialData.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData) {
				var user = plugin as IUser;
				if (user == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(IUser)} interface", nameof(plugin));
				}

				var sequentialDataReader = new SequentialPluginDataReader(storedData, level);
				var rateOfFire = sequentialDataReader.GetNext<float>();
				var projectileTypeID = sequentialDataReader.GetNext<int>();
				var searchForTarget = sequentialDataReader.GetNext<bool>();
				var targetSearchDelay = sequentialDataReader.GetNext<float>();
				var shotDelay = sequentialDataReader.GetNext<float>();
				var searchDelay = sequentialDataReader.GetNext<float>();
				targetID = sequentialDataReader.GetNext<int>();
				var enabled = sequentialDataReader.GetNext<bool>();

				user.GetMandatoryDelegates(out GetSourceOffsetDelegate getSourceOffset);

				Shooter = new Shooter(level,
									level.PackageManager.ActiveGame.GetProjectileType(projectileTypeID),
									rateOfFire,
									getSourceOffset)
						{
							SearchForTarget = searchForTarget,
							TargetSearchDelay = targetSearchDelay,
							shotDelay = shotDelay,
							searchDelay = searchDelay,
							Enabled = enabled

						};
			}

			public override  void ConnectReferences(LevelManager level)
			{
				//If shooter had a target
				if (targetID != 0) {
					Shooter.Target = level.GetRangeTarget(targetID);
					Shooter.Target.AddShooter(Shooter);
				} 
				
			}

			public override void FinishLoading()
			{

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
			}
		}

		public interface IUser {
			void GetMandatoryDelegates(out GetSourceOffsetDelegate getSourceOffset);
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

		public event TargetAquiredDelegate OnTargetAcquired;
		public event ShotReloadedDelegate OnShotReloaded;
		public event BeforeShotFiredDelegate OnBeforeShotFired;
		public event TargetLostDelegate OnTargetLost;
		public event TargetDestroyedDelegate OnTargetDestroyed;
		public event ShotFiredDelegate OnShotFired;

		readonly GetSourceOffsetDelegate getSourceOffset;
		

		float shotDelay;
		float searchDelay;

		readonly ProjectileType projectileType;


		protected Shooter(ILevelManager level,
						ProjectileType projectileType,
						float rateOfFire,
						GetSourceOffsetDelegate getSourceOffset) 
			:base(level)
		{

			this.projectileType = projectileType;
			this.RateOfFire = rateOfFire;
			this.shotDelay = 60 / RateOfFire;
			this.searchDelay = 0;
			this.getSourceOffset = getSourceOffset;
			ReceiveSceneUpdates = true;
		}

		public static Shooter CreateNew<T>(T instancePlugin, 
										   ILevelManager level,
										   ProjectileType projectileType,
										   float rateOfFire)
			where T : InstancePlugin, IUser
		{

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			((IUser) instancePlugin).GetMandatoryDelegates(out GetSourceOffsetDelegate getSourceOffset);

			return new Shooter(level,
								projectileType,
								rateOfFire,
								getSourceOffset);
		}

		
		public override PluginData SaveState()
		{
			return Loader.SaveState(this);
		}



		public bool ShootAt(IRangeTarget newTarget) {
			StopShooting();

			if (!projectileType.IsInRange(Entity.Position, newTarget)) return false;

			Target = newTarget;
			newTarget.AddShooter(this);
			return true;
		}

		public bool CanShootAt(IRangeTarget target)
		{
			return projectileType.IsInRange(Entity.Position, target);
		}

		public void StopShooting() {
			Target?.RemoveShooter(this);
			Target = null;
		}

		public void OnTargetDestroy(IRangeTarget target) {
			Debug.Assert(this.Target == target);
			this.Target = null;
			OnTargetDestroyed?.Invoke(this, target);
		}

		protected override void OnDeleted() {
			Target?.RemoveShooter(this);
			base.OnDeleted();
		}

		protected override void OnUpdateChecked(float timeStep)
		{

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
											.Where(unit => projectileType.IsInRange(Entity.Position, unit.GetDefaultComponent<RangeTargetComponent>()))
											.OrderBy(unit => Vector3.Distance(Entity.Position, unit.Position));


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

			OnBeforeShotFired?.Invoke(this);

			var projectile = Level.SpawnProjectile(projectileType, Entity.Position + getSourceOffset(this), Player, Target);
			//Could not fire on the target
			if (projectile == null) {
				var previousTarget = Target;
				Target.RemoveShooter(this);
				Target = null;

				OnTargetLost?.Invoke(this, previousTarget);
				return;
			}

			OnShotFired?.Invoke(this, projectile);

			shotDelay = 60 / RateOfFire;

		}


		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);

			AddedToEntity(typeof(Shooter), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(Shooter), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}

	}
}
