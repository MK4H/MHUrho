using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.EntityInfo;
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

		float hp;
		HealthBar healthbar;

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

			hp = 100;
			Init(hp);
		}

		public override void SaveState(PluginDataWrapper pluginDataStorage)
		{
			var sequentialData = pluginDataStorage.GetWriterForWrappedSequentialData();
			sequentialData.StoreNext(hp);
		}

		public override void LoadState(ILevelManager level, IUnit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			this.Unit = unit;
			unit.AlwaysVertical = true;
			animationController = unit.CreateComponent<AnimationController>();
			Walker = unit.GetDefaultComponent<WorldWalker>();
			Shooter = unit.GetDefaultComponent<Shooter>();

			var sequentialData = pluginData.GetReaderForWrappedSequentialData();
			hp = sequentialData.GetNext<float>();

			Init(hp);
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
			if (projectile.Player == Unit.Player) {
				return;
			}

			hp -= 10;
			

			if (hp < 0) {
				healthbar.SetHealth(0);
				animationController.PlayExclusive("Chicken/Models/Dying.ani", 0, false);
				dying = true;
				Shooter.Enabled = false;
				Walker.Enabled = false;
			}
			else {
				healthbar.SetHealth((int)hp);
			}
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

		public void OnUnitOrdered(UnitSelector selector, Order order) {
			order.Executed = false;
			if (order.PlatformOrder) {
				switch (order) {
					case MoveOrder moveOrder:
						order.Executed = Walker.GoTo(moveOrder.Target);
						break;
					case AttackOrder attackOrder:
						IRangeTarget rangeTarget;
						if (Unit.Player.IsEnemy(attackOrder.Target.Player) && ((rangeTarget = attackOrder.Target.GetDefaultComponent<RangeTargetComponent>()) != null)) {
							order.Executed = Shooter.ShootAt(rangeTarget) || Walker.GoTo(Map.PathFinding.GetClosestNode(rangeTarget.CurrentPosition));
						}
						break;
				}
			}
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

		public override void Dispose()
		{
			healthbar.Dispose();
		}

		IEnumerator<Waypoint> MovingRangeTarget.INotificationReceiver.GetWaypoints(MovingRangeTarget movingRangeTarget)
		{
			return Walker.GetRestOfThePath(new Vector3(0, 0.5f, 0));
		}

		void Init(float health)
		{
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 15, 0), new Vector2(0.5f, 0.1f), health);
		}

	}
}
