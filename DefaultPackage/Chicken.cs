using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;

namespace DefaultPackage
{
	public class ChickenType : UnitTypePluginBase {

		public UnitTypeInitializationData TypeData => new UnitTypeInitializationData();

		public ProjectileType ProjectileType { get; private set; }


		public override bool IsMyType(string unitTypeName) {
			return unitTypeName == "Chicken";
		}

		public ChickenType() {

		}



		public override UnitInstancePluginBase CreateNewInstance(ILevelManager level, Unit unit) {
			return new ChickenInstance(level, unit, this);
		}

		public override UnitInstancePluginBase GetInstanceForLoading() {
			return new ChickenInstance();
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return true;
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			ProjectileType = packageManager.ActiveGame.GetProjectileType("EggProjectile", true);
		}
	}

	public class ChickenInstance : UnitInstancePluginBase, 
									WorldWalker.INotificationReceiver, 
									UnitSelector.INotificationReceiver, 
									Shooter.INotificationReceiver,
									MovingRangeTarget.INotificationReceiver{

		AnimationController animationController;
		WorldWalker walker;
		Shooter shooter;

		bool dying;

		public ChickenInstance() {

		}

		public ChickenInstance(ILevelManager level, Unit unit, ChickenType type) 
			:base(level,unit) {
			animationController = unit.Node.CreateComponent<AnimationController>();
			walker = WorldWalker.GetInstanceFor(this,level);
			shooter = Shooter.CreateNew(this, level,type.ProjectileType, 20);
			shooter.SearchForTarget = true;
			shooter.TargetSearchDelay = 2;
			unit.AddComponent(walker);
			unit.AddComponent(shooter);
			unit.AddComponent(UnitSelector.CreateNew(this, level));
			unit.AddComponent(MovingRangeTarget.CreateNew(this, level));
			
			unit.AlwaysVertical = true;
		}

		public override void SaveState(PluginDataWrapper pluginDataStorage) {

		}

		public override void LoadState(ILevelManager level, Unit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			this.Unit = unit;
			unit.AlwaysVertical = true;
			animationController = unit.Node.CreateComponent<AnimationController>();
			walker = unit.GetDefaultComponent<WorldWalker>();
			shooter = unit.GetDefaultComponent<Shooter>();

		}

		public override bool CanGoFromTo(ITile fromTile, ITile toTile) {
			return toTile.Building == null;
		}

		public override void OnUpdate(float timeStep) {
			if (dying) {
				if (animationController.IsAtEnd("Chicken/Models/Dying.ani")) {
					Unit.Kill();
				}
				return;
			}

			if (shooter.Target != null) {
				var targetPos = shooter.Target.CurrentPosition;

				var diff = Unit.Position - targetPos;

				Unit.FaceTowards(Unit.Position + diff);
			}
		}

		public float GetMovementSpeed(ITile tile) {
			return 1;
		}

		public void OnMovementStarted(WorldWalker walker) {
			animationController.PlayExclusive("Chicken/Models/Walk.ani", 0, true);
			animationController.SetSpeed("Chicken/Models/Walk.ani", 2);

			shooter.StopShooting();
			shooter.SearchForTarget = false;
		}

		public void OnMovementFinished(WorldWalker walker) {
			animationController.Stop("Chicken/Models/Walk.ani");
			shooter.SearchForTarget = true;
		}

		public void OnMovementFailed(WorldWalker walker) {
			animationController.Stop("Chicken/Models/Walk.ani");
			shooter.SearchForTarget = true;
		}

		public void OnUnitSelected(UnitSelector selector) {
			if (!walker.MovementStarted) {
				animationController.Play("Chicken/Models/Idle.ani", 0, true);
			}
		}

		public void OnUnitDeselected(UnitSelector selector) {

		}

		public void OnUnitOrderedToTile(UnitSelector selector, ITile targetTile, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = walker.GoTo(targetTile);
		}

		public void OnUnitOrderedToUnit(UnitSelector selector, Unit targetUnit, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs)
		{
			IRangeTarget rangeTarget;
			if (Unit.Player.IsEnemy(targetUnit.Player) && ((rangeTarget = targetUnit.GetDefaultComponent<RangeTargetComponent>()) != null)) {
				orderArgs.Executed = shooter.ShootAt(rangeTarget);
				return;
			}

			orderArgs.Executed = false;
		}

		public void OnUnitOrderedToBuilding(UnitSelector selector, Building targetBuilding, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = false;
		}

		public void OnTargetAcquired(Shooter shooter) {
			var targetPos = shooter.Target.CurrentPosition;

			var diff = Unit.Position - targetPos;

			Unit.FaceTowards(Unit.Position + diff);
		}

		public void BeforeShotFired(Shooter shooter) {
			var targetPos = shooter.Target.CurrentPosition;

			var diff = Unit.Position - targetPos;

			Unit.FaceTowards(Unit.Position + diff);
		}

		public void AfterShotFired(Shooter shooter, Projectile projectile) {

		}

		public void OnShotReloaded(Shooter shooter) {

		}

		public Vector3 GetSourceOffset(Shooter forShooter) 
		{
			return Unit.Backward * 0.7f + new Vector3(0,0.7f,0);
		}

		public Vector3 GetCurrentPosition(MovingRangeTarget movingRangeTarget) {
			return Unit.Position + new Vector3(0, 0.5f, 0);
		}

		public void OnHit(MovingRangeTarget target, Projectile projectile)
		{
			animationController.PlayExclusive("Chicken/Models/Dying.ani", 0, false);
			dying = true;
		}

		IEnumerator<Waypoint> MovingRangeTarget.INotificationReceiver.GetWaypoints(MovingRangeTarget movingRangeTarget)
		{
			return walker.GetRestOfThePath(new Vector3(0, 0.5f, 0));
		}
	}
}
