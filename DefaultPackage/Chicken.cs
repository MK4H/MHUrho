using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;

namespace DefaultPackage
{
	public class ChickenType : UnitTypePlugin {

		public UnitTypeInitializationData TypeData => new UnitTypeInitializationData();

		public ProjectileType ProjectileType { get; private set; }


		public override bool IsMyType(string unitTypeName) {
			return unitTypeName == "Chicken";
		}

		public ChickenType() {

		}



		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit) {
			return new ChickenInstance(level, unit, this);
		}

		public override UnitInstancePlugin GetInstanceForLoading() {
			return new ChickenInstance();
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return true;
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			ProjectileType = packageManager.ActiveGame.GetProjectileType("EggProjectile", true);
		}
	}

	public class ChickenInstance : UnitInstancePlugin, 
									WorldWalker.INotificationReceiver, 
									UnitSelector.INotificationReceiver, 
									Shooter.INotificationReceiver,
									MovingRangeTarget.INotificationReceiver{

		class PathVisitor : NodeVisitor {
			readonly ChickenInstance chicken;

			public PathVisitor(ChickenInstance chicken)
			{
				this.chicken = chicken;
			}

			public override bool Visit(ITempNode source, IBuildingNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			public override bool Visit(ITempNode source, ITileEdgeNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			public override bool Visit(ITempNode source, ITileNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			public override bool Visit(ITileEdgeNode source, ITileNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			public override bool Visit(IBuildingNode source, IBuildingNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			public override bool Visit(ITileNode source, IBuildingNode target, out float time)
			{
				time = 1;
				return true;
			}

			public override bool Visit(ITileNode source, ITileEdgeNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			public override bool Visit(ITileEdgeNode source, IBuildingNode target, out float time)
			{
				time = 1;
				return true;
			}

			public override bool Visit(IBuildingNode source, ITileEdgeNode target, out float time)
			{
				time = 1;
				return true;
			}

			public override bool Visit(IBuildingNode source, ITileNode target, out float time)
			{
				time = 1;
				return true;
			}

			float GetTime(Vector3 from, Vector3 to)
			{
				//Check for complete equality, which breaks the code below
				if (from == to) {
					return 0;
				}

				Vector3 diff = to - from;

				//In radians
				float angle = (float)Math.Max(Math.Asin(Math.Abs(diff.Y) / diff.Length), 0);

				//TODO: Maybe cache the Length in the Edge
				return (diff.Length / 2) + angle;
			}
		}

		AnimationController animationController;
		public WorldWalker Walker { get; private set; }
		public Shooter Shooter{ get; private set; }

		bool dying;

		readonly PathVisitor pathVisitor;

		public ChickenInstance()
		{
			pathVisitor = new PathVisitor(this);
		}

		public ChickenInstance(ILevelManager level, IUnit unit, ChickenType type) 
			:base(level,unit) {
			animationController = unit.CreateComponent<AnimationController>();
			Walker = WorldWalker.GetInstanceFor(this,level);
			Shooter = Shooter.CreateNew(this, level,type.ProjectileType, 20);
			Shooter.SearchForTarget = true;
			Shooter.TargetSearchDelay = 2;
			unit.AddComponent(Walker);
			unit.AddComponent(Shooter);
			unit.AddComponent(UnitSelector.CreateNew(this, level));
			unit.AddComponent(MovingRangeTarget.CreateNew(this, level));
			
			unit.AlwaysVertical = true;
			pathVisitor = new PathVisitor(this);
		}

		public override void SaveState(PluginDataWrapper pluginDataStorage) {

		}

		public override void LoadState(ILevelManager level, IUnit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			this.Unit = unit;
			unit.AlwaysVertical = true;
			animationController = unit.CreateComponent<AnimationController>();
			Walker = unit.GetDefaultComponent<WorldWalker>();
			Shooter = unit.GetDefaultComponent<Shooter>();

		}

		public bool GetTime(INode from, INode to, out float time)
		{
			return from.Accept(pathVisitor, to, out time);
		}

		public float GetMinimalAproximatedTime(Vector3 from, Vector3 to)
		{
			return (from.XZ2() - to.XZ2()).Length / 2;
		}

		public override void OnProjectileHit(IProjectile projectile)
		{
			throw new NotImplementedException();
		}

		public override void OnMeeleHit(IEntity byEntity)
		{
			throw new NotImplementedException();
		}

		public override void OnUpdate(float timeStep) {
			if (dying) {
				if (animationController.IsAtEnd("Chicken/Models/Dying.ani")) {
					Unit.RemoveFromLevel();
				}
				return;
			}

			if (Shooter.Target != null) {
				var targetPos = Shooter.Target.CurrentPosition;

				var diff = Unit.Position - targetPos;

				Unit.FaceTowards(Unit.Position + diff);
			}
		}


		public void OnMovementStarted(WorldWalker walker) {
			animationController.PlayExclusive("Chicken/Models/Walk.ani", 0, true);
			animationController.SetSpeed("Chicken/Models/Walk.ani", 2);

			Shooter.StopShooting();
			Shooter.SearchForTarget = false;
		}

		public void OnMovementFinished(WorldWalker walker) {
			animationController.Stop("Chicken/Models/Walk.ani");
			Shooter.SearchForTarget = true;
		}

		public void OnMovementFailed(WorldWalker walker) {
			animationController.Stop("Chicken/Models/Walk.ani");
			Shooter.SearchForTarget = true;
		}

		public void OnUnitSelected(UnitSelector selector) {
			if (!Walker.MovementStarted) {
				animationController.Play("Chicken/Models/Idle.ani", 0, true);
			}
		}

		public void OnUnitDeselected(UnitSelector selector) {

		}

		public void OnUnitOrderedToTile(UnitSelector selector, ITile targetTile, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = Walker.GoTo(targetTile);
		}

		public void OnUnitOrderedToUnit(UnitSelector selector, IUnit targetUnit, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs)
		{
			IRangeTarget rangeTarget;
			if (Unit.Player.IsEnemy(targetUnit.Player) && ((rangeTarget = targetUnit.GetDefaultComponent<RangeTargetComponent>()) != null)) {
				orderArgs.Executed = Shooter.ShootAt(rangeTarget);
				return;
			}

			orderArgs.Executed = false;
		}

		public void OnUnitOrderedToBuilding(UnitSelector selector, IBuilding targetBuilding, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
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

		public void AfterShotFired(Shooter shooter, IProjectile projectile) {

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

		public void OnHit(MovingRangeTarget target, IProjectile projectile)
		{
			if (projectile.Player != Unit.Player) {
				animationController.PlayExclusive("Chicken/Models/Dying.ani", 0, false);
				dying = true;
				Shooter.Enabled = false;
				Walker.Enabled = false;
			}
		}

		IEnumerator<Waypoint> MovingRangeTarget.INotificationReceiver.GetWaypoints(MovingRangeTarget movingRangeTarget)
		{
			return Walker.GetRestOfThePath(new Vector3(0, 0.5f, 0));
		}


	}
}
