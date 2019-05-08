using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
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

			readonly LevelManager level;
			readonly InstancePlugin plugin;
			readonly StDefaultComponent storedData;

			int targetID;

			public Loader()
			{

			}

			protected Loader(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				this.level = level;
				this.plugin = plugin;
				this.storedData = storedData;
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

			public override void StartLoading() {
				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.Shooter) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedShooter = storedData.Shooter;

				Shooter = new Shooter(level,
									level.PackageManager.ActivePackage.GetProjectileType(storedShooter.ProjectileTypeID),
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

			public override  void ConnectReferences()
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

			public override DefaultComponentLoader Clone(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				return new Loader(level, plugin, storedData);
			}
		}

		/// <summary>
		/// Shots per minute
		/// </summary>
		public float RateOfFire { get; set; }

		/// <summary>
		/// If shooter should search for a target automatically.
		/// </summary>
		public bool SearchForTarget { get; set; }

		/// <summary>
		/// Delay between search sweeps.
		/// </summary>
		public float TargetSearchDelay { get; set; }

		/// <summary>
		/// Offset of the spawn point of projectiles from the <see cref="Entity.Position"/> of the owning entity
		/// Offset is in the Entities local space, +z is forward, +x is right, +y is up in the Entities current orientation
		/// </summary>
		public Vector3 SourceOffset { get; set; }

		/// <summary>
		/// Current target the shooter is shooting at.
		/// </summary>
		public IRangeTarget Target { get; private set; }

		/// <summary>
		/// Invoked when shooter acquires a target automatically.
		/// </summary>
		public event TargetAquiredDelegate TargetAutoAcquired;

		/// <summary>
		/// Invoked when timeout between shots expires.
		/// </summary>
		public event ShotReloadedDelegate ShotReloaded;

		/// <summary>
		/// Invoked just before projectile is fired.
		/// </summary>
		public event BeforeShotFiredDelegate BeforeShotFired;

		/// <summary>
		/// Invoked when shooter looses current target. (Target gets out of range, etc.)
		/// </summary>
		public event TargetLostDelegate TargetLost;

		/// <summary>
		/// Invoked when target dies.
		/// </summary>
		public event TargetDestroyedDelegate TargetDestroyed;

		/// <summary>
		/// Invoked just after the projectile is fired.
		/// </summary>
		public event ShotFiredDelegate ShotFired;

		float shotDelay;
		float searchDelay;

		/// <summary>
		/// Type of the projectile to shoot.
		/// </summary>
		readonly ProjectileType projectileType;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="level">Current level.</param>
		/// <param name="projectileType">Type of the projectile to shoot.</param>
		/// <param name="sourceOffset">Offset of the source of projectiles from the Entity Node.</param>
		/// <param name="rateOfFire">Number of projectiles to shoot per minute.</param>
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

		/// <summary>
		/// Creates new instance of a shooter and attaches it to the Entity and it's node.
		/// </summary>
		/// <param name="plugin">Plugin of the entity</param>
		/// <param name="level">Current level.</param>
		/// <param name="projectileType">Type of the projectile to shoot.</param>
		/// <param name="sourceOffset">Offset of the source of projectiles from the Entity Node.</param>
		/// <param name="rateOfFire">Rate of fire in number of projectiles per minute.</param>
		/// <returns>The newly created instance of Shooter.</returns>
		public static Shooter CreateNew(EntityInstancePlugin plugin,
										ILevelManager level,
										ProjectileType projectileType,
										Vector3 sourceOffset,
										float rateOfFire)
		{

			var newInstance =  new Shooter(level,
											projectileType,
											sourceOffset,
											rateOfFire);
			plugin.Entity.AddComponent(newInstance);
			return newInstance;
		}

		
		public override StDefaultComponent SaveState()
		{
			return Loader.SaveState(this);
		}

		/// <summary>
		/// Stops shooting at any current target and sets <paramref name="newTarget"/> as the target of this Shooter if the <paramref name="newTarget"/> is in range of this shooter.
		/// </summary>
		/// <param name="newTarget">New target to try shooting at.</param>
		/// <returns>True if <paramref name="newTarget"/> is in range, false otherwise.</returns>
		public bool ShootAt(IRangeTarget newTarget) {
			StopShooting();

			if (!CanShootAt(newTarget)) return false;

			Target = newTarget;
			newTarget.AddShooter(this);
			return true;
		}

		/// <summary>
		/// Checks if <paramref name="target"/> can be shot at, mainly if the target is in range.
		/// </summary>
		/// <param name="target">The target to check.</param>
		/// <returns>True of target can be shot at, false otherwise.</returns>
		public bool CanShootAt(IRangeTarget target)
		{
			return projectileType.IsInRange(Entity.Position, target);
		}

		/// <summary>
		/// Stops shooting at any current target.
		/// </summary>
		public void StopShooting() {
			Target?.RemoveShooter(this);
			Target = null;
		}

		/// <summary>
		/// Manually resets shot delay, meaning if shooter has a target, it will shoot at it immediately.
		/// </summary>
		public void ResetShotDelay()
		{
			shotDelay = 60 / RateOfFire;
		}

		/// <summary>
		/// Informs the shooter that the target was destroyed.
		/// </summary>
		/// <param name="target">The destroyed target.</param>
		void RangeTargetComponent.IShooter.OnTargetDestroy(IRangeTarget target)
		{
			Debug.Assert(this.Target == target);
			this.Target = null;
			InvokeOnTargetDestroyed(target);
		}

		/// <summary>
		/// Invoked when this component is deleted, basically a destructor.
		/// </summary>
		protected override void OnDeleted() {
			Target?.RemoveShooter(this);
			base.OnDeleted();
		}

		/// <summary>
		/// Game tick update, invoked only when both Level and Entity are enabled.
		/// </summary>
		/// <param name="timeStep">Elapsed time since the previous update.</param>
		protected override void OnUpdateChecked(float timeStep)
		{
			SearchTarget(timeStep);
			Shoot(timeStep);
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


		void Shoot(float timeStep)
		{
			if (shotDelay > 0) {
				shotDelay -= timeStep;
				return;
			}

			InvokeOnShotReloaded();

			if (Target == null) {
				return;
			}

			InvokeOnBeforeShotFired();

			//Check if shotDelay was not reset in the OnBeforeShotFired or OnShotReloaded handlers
			if (shotDelay > 0) {
				return;
			}

			//Rotate the SourceOffset according to Entity world rotation
			Vector3 worldOffset = Quaternion.FromRotationTo(Vector3.UnitZ, Entity.Forward) * SourceOffset;
			var projectile = Level.SpawnProjectile(projectileType, Entity.Position + worldOffset, Quaternion.Identity, Player, Target);
			//Could not fire on the target
			if (projectile == null) {
				var previousTarget = Target;
				Target.RemoveShooter(this);
				Target = null;

				InvokeOnTargetLost(previousTarget);
			}
			else {
				InvokeOnShotFired(projectile);
			}

			ResetShotDelay();
		}

		void SearchTarget(float timeStep)
		{
			if (!SearchForTarget) {
				return;
			}

			if (searchDelay >= 0) {
				searchDelay -= timeStep;
				return;
			}

			if (Target == null && searchDelay < 0) {
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
					InvokeOnTargetAcquired();
					break;
				}

			}
		}

		void InvokeOnTargetAcquired()
		{
			try
			{
				TargetAutoAcquired?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(TargetAutoAcquired)}: {e.Message}");
			}
		}

		void InvokeOnShotReloaded(){
			try
			{
				ShotReloaded?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(ShotReloaded)}: {e.Message}");
			}
		}


		void InvokeOnBeforeShotFired(){
			try
			{
				BeforeShotFired?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(BeforeShotFired)}: {e.Message}");
			}
		}


		void InvokeOnTargetLost(IRangeTarget target){
			try
			{
				TargetLost?.Invoke(this, target);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(TargetLost)}: {e.Message}");
			}

		}


		void InvokeOnTargetDestroyed(IRangeTarget target){
			try
			{
				TargetDestroyed?.Invoke(this, target);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(TargetDestroyed)}: {e.Message}");
			}
		}


		void InvokeOnShotFired(IProjectile projectile){
			try
			{
				ShotFired?.Invoke(this, projectile);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(ShotFired)}: {e.Message}");
			}
		}

	}
}
