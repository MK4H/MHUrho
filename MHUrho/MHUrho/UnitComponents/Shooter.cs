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

	public class Shooter : DefaultComponent, RangeTargetComponent.IShooter
	{
		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => Shooter;

			public Shooter Shooter { get; private set; }

			int targetID;

			public Loader()
			{

			}

			public static StDefaultComponent SaveState(Shooter shooter)
			{
				var storedShooter = new StShooter
									{
										Enabled = shooter.Enabled,
										ProjectileTypeID = shooter.projectileType.ID,
										SourceOffset = shooter.SourceOffset.ToStVector3(),
										RateOfFire = shooter.RateOfFire,
										SearchDelay = shooter.searchDelay,
										SearchForTarget = shooter.SearchForTarget,
										ShotDelay = shooter.shotDelay,
										TargetID = shooter.Target?.InstanceID ?? 0,
										TargetSearchDelay = shooter.TargetSearchDelay
									};

				

				return new StDefaultComponent{Shooter = storedShooter};
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData) {
				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.Shooter) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedShooter = storedData.Shooter;

				Shooter = new Shooter(level,
									level.PackageManager.ActiveGame.GetProjectileType(storedShooter.ProjectileTypeID),
									storedShooter.SourceOffset.ToVector3(),
									storedShooter.RateOfFire)
						{
							SearchForTarget = storedShooter.SearchForTarget,
							TargetSearchDelay = storedShooter.TargetSearchDelay,
							shotDelay = storedShooter.ShotDelay,
							searchDelay = storedShooter.SearchDelay,
							Enabled = storedShooter.Enabled

				};

				targetID = storedShooter.TargetID;
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

		/// <summary>
		/// Shots per minute
		/// </summary>
		public float RateOfFire { get; set; }

		public bool SearchForTarget { get; set; }

		public float TargetSearchDelay { get; set; }

		/// <summary>
		/// Offset of the spawn point of projectiles from the <see cref="Entity.Position"/> of the owning entity
		/// Offset is in the Entities local space, +z is forward, +x is right, +y is up in the Entities current orientation
		/// </summary>
		public Vector3 SourceOffset { get; set; }

		public IRangeTarget Target { get; private set; }

		public event TargetAquiredDelegate OnTargetAcquired;
		public event ShotReloadedDelegate OnShotReloaded;
		public event BeforeShotFiredDelegate OnBeforeShotFired;
		public event TargetLostDelegate OnTargetLost;
		public event TargetDestroyedDelegate OnTargetDestroyed;
		public event ShotFiredDelegate OnShotFired;

		float shotDelay;
		float searchDelay;

		readonly ProjectileType projectileType;


		protected Shooter(ILevelManager level,
						ProjectileType projectileType,
						Vector3 sourceOffset,
						float rateOfFire) 
			:base(level)
		{

			this.projectileType = projectileType;
			this.SourceOffset = sourceOffset;
			this.RateOfFire = rateOfFire;
			this.shotDelay = 60 / RateOfFire;
			this.searchDelay = 0;
			ReceiveSceneUpdates = true;
		}

		public static Shooter CreateNew(ILevelManager level,
										ProjectileType projectileType,
										Vector3 sourceOffset,
										float rateOfFire)
		{

			return new Shooter(level,
								projectileType,
								sourceOffset,
								rateOfFire);
		}

		
		public override StDefaultComponent SaveState()
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

			//Rotate the SourceOffset according to Entity world rotation
			Vector3 worldOffset = Quaternion.FromRotationTo(Vector3.UnitZ, Entity.Forward) * SourceOffset;
			var projectile = Level.SpawnProjectile(projectileType, Entity.Position + worldOffset, Player, Target);
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
