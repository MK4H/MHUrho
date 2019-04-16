using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.EntityInfo;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.PathFinding.AStar;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;

namespace DefaultPackage
{
	public class ChickenType : UnitTypePlugin {

		public override int ID => 3;

		public override string Name => "Chicken";

		public ProjectileType ProjectileType { get; private set; }


		public ChickenType() {

		}



		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit) {
			return new ChickenInstance(level, unit, this);
		}

		public override UnitInstancePlugin GetInstanceForLoading(ILevelManager level, IUnit unit) {
			return new ChickenInstance(level, unit);
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return true;
		}

		public override void Initialize(XElement extensionElement, GamePack package) {
			ProjectileType = package.GetProjectileType("EggProjectile");
		}
	}

	public class ChickenInstance : UnitInstancePlugin, 
									WorldWalker.IUser, 
									MovingRangeTarget.IUser{

		class DistanceCalc : NodeDistCalculator
		{
			protected override bool GetTime(ITileNode source, ITileNode target, out float time)
			{
				Vector3 edgePosition = source.GetEdgePosition(target);
				time = GetTime(source.Position, edgePosition) + GetTime(edgePosition, target.Position);
				return true;
			}

			protected override bool GetTime(ITempNode source, IBuildingNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			protected override bool GetTime(ITempNode source, ITempNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			protected override bool GetTime(IBuildingNode source, ITempNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			protected override bool GetTime(ITempNode source, ITileNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			protected override bool GetTime(IBuildingNode source, IBuildingNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			protected override bool GetTime(ITileNode source, IBuildingNode target, out float time)
			{
				time = 1;
				return true;
			}

			protected override bool GetTime(ITileNode source, ITempNode target, out float time)
			{
				time = GetTime(source.Position, target.Position);
				return true;
			}

			protected override bool GetTime(IBuildingNode source, ITileNode target, out float time)
			{
				time = 1;
				return true;
			}

			float GetTime(Vector3 source, Vector3 to)
			{
				//Check for complete equality, which breaks the code below
				if (source == to)
				{
					return 0;
				}

				Vector3 diff = to - source;

				//In radians
				float angle = (float)Math.Max(Math.Asin(Math.Abs(diff.Y) / diff.Length), 0);

				//TODO: Maybe cache the Length in the Edge
				return (diff.Length / 2) + angle;
			}

			public override float GetMinimalAproxTime(Vector3 source, Vector3 target)
			{
				return (source.XZ2() - target.XZ2()).Length / 2;
			}

		}

		AnimationController animationController;
		public WorldWalker Walker { get; private set; }
		public Shooter Shooter{ get; private set; }

		bool dying;

		readonly DistanceCalc distCalc;

		float hp;
		HealthBar healthbar;

		IRangeTarget explicitTarget;
		bool targetMoved = false;

		const float timeBetweenTests = 1.0f;
		float shootTestTimer = timeBetweenTests;


		public ChickenInstance(ILevelManager level, IUnit unit)
			:base(level, unit)
		{
			distCalc = new DistanceCalc();
		}

		public ChickenInstance(ILevelManager level, IUnit unit, ChickenType type) 
			:base(level,unit) {
			animationController = unit.CreateComponent<AnimationController>();
			Walker = WorldWalker.CreateNew(this, level);
			Shooter = Shooter.CreateNew(this, level, type.ProjectileType, new Vector3(0,0.7f,-0.7f), 20);
			Shooter.SearchForTarget = true;
			Shooter.TargetSearchDelay = 2;

			var selector = UnitSelector.CreateNew(this, level);


			MovingRangeTarget.CreateNew(this, level, new Vector3(0, 0.5f, 0));
			
			unit.AlwaysVertical = true;
			distCalc = new DistanceCalc();

			RegisterEvents(Walker, Shooter, selector);

			hp = 100;
			Init(hp);
		}

		public override void SaveState(PluginDataWrapper pluginDataStorage)
		{
			var sequentialData = pluginDataStorage.GetWriterForWrappedSequentialData();
			sequentialData.StoreNext(hp);
		}

		public override void LoadState(PluginDataWrapper pluginData) {
			Unit.AlwaysVertical = true;
			animationController = Unit.CreateComponent<AnimationController>();
			Walker = Unit.GetDefaultComponent<WorldWalker>();
			Shooter = Unit.GetDefaultComponent<Shooter>();

			RegisterEvents(Walker, Shooter, Unit.GetDefaultComponent<UnitSelector>());

			var sequentialData = pluginData.GetReaderForWrappedSequentialData();
			hp = sequentialData.GetNext<float>();
			Init(hp);
		}



		public override void OnHit(IEntity other, object userData)
		{
			if (other.Player == Unit.Player) {
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


		public override void OnUpdate(float timeStep) {
			if (dying) {
				if (animationController.IsAtEnd("Chicken/Models/Dying.ani")) {
					Unit.RemoveFromLevel();
				}
				return;
			}

			if (explicitTarget != null) {
				shootTestTimer -= timeStep;
				if (shootTestTimer < 0) {
					shootTestTimer = timeBetweenTests;

					if (Shooter.CanShootAt(explicitTarget)) {
						Walker.Stop();
						Shooter.ShootAt(explicitTarget);
					}
					else if (explicitTarget.Moving && targetMoved) {
						targetMoved = false;
						Walker.GoTo(Map.PathFinding.GetClosestNode(explicitTarget.CurrentPosition));
					}
				}
			}

			if (Shooter.Target != null && Walker.State != WorldWalkerState.Started) {
				var targetPos = Shooter.Target.CurrentPosition;

				var diff = Unit.Position - targetPos;

				Unit.FaceTowards(Unit.Position + diff);
			}
		}

		public override void Dispose()
		{
			healthbar.Dispose();
		}

		INodeDistCalculator WorldWalker.IUser.GetNodeDistCalculator()
		{
			return distCalc;
		}

		void OnMovementStarted(WorldWalker walker) {
			animationController.PlayExclusive("Chicken/Models/Walk.ani", 0, true);
			animationController.SetSpeed("Chicken/Models/Walk.ani", 2);

			Shooter.StopShooting();
			Shooter.SearchForTarget = false;
		}

		void OnMovementFinished(WorldWalker walker) {
			animationController.Stop("Chicken/Models/Walk.ani");
			Shooter.SearchForTarget = true;
			Shooter.ResetShotDelay();
		}

		void OnMovementFailed(WorldWalker walker) {
			animationController.Stop("Chicken/Models/Walk.ani");
			Shooter.SearchForTarget = true;
			Shooter.ResetShotDelay();
		}

		void OnMovementCanceled(WorldWalker walker)
		{
			animationController.Stop("Chicken/Models/Walk.ani");
			Shooter.SearchForTarget = true;
			Shooter.ResetShotDelay();
		}

		void OnUnitSelected(UnitSelector selector) {
			if (Walker.State != WorldWalkerState.Started) {
				animationController.Play("Chicken/Models/Idle.ani", 0, true);
			}
		}

		void OnUnitOrdered(UnitSelector selector, Order order) {
			order.Executed = false;
			if (order.PlatformOrder) {
				switch (order) {
					case MoveOrder moveOrder:
						Shooter.StopShooting();
						Shooter.SearchForTarget = false;
						explicitTarget = null;
						order.Executed = Walker.GoTo(moveOrder.Target);
						break;
					case AttackOrder attackOrder:
						Shooter.StopShooting();

						if (Unit.Player.IsEnemy(attackOrder.Target.Player) && (SetExplicitTarget(attackOrder.Target) != null)) {
							order.Executed = Shooter.ShootAt(explicitTarget) || Walker.GoTo(Map.PathFinding.GetClosestNode(explicitTarget.CurrentPosition));
						}

						if (order.Executed) {
							Shooter.SearchForTarget = false;
						}
						break;
				}
			}
		}

		void OnTargetAcquired(Shooter shooter) {
			var targetPos = shooter.Target.CurrentPosition;

			var diff = Unit.Position - targetPos;

			Unit.FaceTowards(Unit.Position + diff);
		}

		void BeforeShotFired(Shooter shooter) {
			var targetPos = shooter.Target.CurrentPosition;

			var diff = Unit.Position - targetPos;

			Unit.FaceTowards(Unit.Position + diff);
		}

		void OnTargetDestroyed(Shooter shooter, IRangeTarget target)
		{
			if (explicitTarget != null) {
				Debug.Assert(target == explicitTarget);

				explicitTarget = null;
				targetMoved = false;
			}
		}
		IEnumerable<Waypoint> MovingRangeTarget.IUser.GetFutureWaypoints(MovingRangeTarget movingRangeTarget)
		{
			return Walker.GetRestOfThePath(new Vector3(0, 0.5f, 0));
		}

		void Init(float health)
		{
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 15, 0), new Vector2(0.5f, 0.1f), health);
		}

	

		IRangeTarget SetExplicitTarget(IEntity targetEntity)
		{
			IRangeTarget target = targetEntity.GetDefaultComponent<RangeTargetComponent>();

			if (target == null) return null;

			explicitTarget = target;
			target.OnTargetMoved += ExplicitTargetMoved;

			return target;
		}

		void ExplicitTargetMoved(IRangeTarget target)
		{
			targetMoved = true;
		}

		void RegisterEvents(WorldWalker walker, Shooter shooter, UnitSelector selector)
		{
			walker.OnMovementStarted += OnMovementStarted;
			walker.OnMovementEnded += OnMovementFinished;
			walker.OnMovementFailed += OnMovementFailed;
			walker.OnMovementCanceled += OnMovementCanceled;

			

			shooter.OnBeforeShotFired += BeforeShotFired;
			shooter.OnTargetAcquired += OnTargetAcquired;
			shooter.OnTargetDestroyed += OnTargetDestroyed;

			selector.Ordered += OnUnitOrdered;
			selector.UnitSelected += OnUnitSelected;
		}
	}
}
