using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using MHUrho.CameraMovement;
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
using MHUrho.DefaultComponents;
using MHUrho.Input.MandK;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;
using ShowcasePackage.Buildings;
using ShowcasePackage.Misc;
using Urho;

namespace ShowcasePackage.Units
{
	public class ChickenType : SpawnableUnitTypePlugin {

		public static string TypeName = "Chicken";
		public static int TypeID = 3;


		public override string Name => TypeName;
		public override int ID => TypeID;

		public ProjectileType ProjectileType { get; private set; }
		public override Cost Cost => cost;
		public override UnitType UnitType => myType;

		public ViableTileTypes PassableTileTypes { get; private set; }

		const string CostElement = "cost";
		const string PassableTileTypesElement = "canPass";

		Cost cost;
		UnitType myType;

		public ChickenType() {

		}



		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit) {
			return ChickenInstance.CreateNew(level, unit, this);
		}

		public override UnitInstancePlugin GetInstanceForLoading(ILevelManager level, IUnit unit) {
			return ChickenInstance.CreateForLoading(level, unit, this);
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return true;
		}

		public override Spawner GetSpawner(GameController input, GameUI ui, CameraMover camera)
		{
			return new ChickenSpawner(input, ui, camera, myType, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package) {
			XElement costElem = extensionElement.Element(package.PackageManager.GetQualifiedXName(CostElement));
			cost = Cost.FromXml(costElem, package);

			XElement canPass =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(PassableTileTypesElement));
			PassableTileTypes = ViableTileTypes.FromXml(canPass, package);

			myType = package.GetUnitType(ID);
			ProjectileType = package.GetProjectileType("EggProjectile");
		}
	}

	public class ChickenInstance : UnitInstancePlugin, 
									WorldWalker.IUser, 
									MovingRangeTarget.IUser,
									UnitSelector.IUser
	{

		class ChickenDistCalc : ClimbingDistCalc {

			static readonly Dictionary<Tuple<NodeType, NodeType>, float> TeleportTimes =
				new Dictionary<Tuple<NodeType, NodeType>, float>
				{
					{Tuple.Create(NodeType.Tile, NodeType.Tile), 1},
					{Tuple.Create(NodeType.Tile, NodeType.Building), 2},
					{Tuple.Create(NodeType.Tile, NodeType.Temp), 1},
					{Tuple.Create(NodeType.Building, NodeType.Tile), 1},
					{Tuple.Create(NodeType.Building, NodeType.Building), 1},
					{Tuple.Create(NodeType.Building, NodeType.Temp), 1},
					{Tuple.Create(NodeType.Temp, NodeType.Tile), 1},
					{Tuple.Create(NodeType.Temp, NodeType.Building), 2},
					{Tuple.Create(NodeType.Temp, NodeType.Temp), 1},
				};


			/// <summary>
			/// Coefficient of teleport times compared to base teleport times.
			/// </summary>
			public float TeleportCoef { get; set; }

			ILevelManager Level => instance.Level;
			IMap Map => Level.Map;

			readonly ChickenInstance instance;



			/// <summary>
			/// Creates new instance of Chicken distance calculator.
			/// </summary>
			/// <param name="chicken">Chicken for which this is calculating.</param>
			/// <param name="baseCoef">Coefficient of linear motion speed.</param>
			/// <param name="angleCoef">Coefficient how much the linear motion speed is affected by angle.</param>
			/// <param name="teleportCoef">Coefficient of teleport times.</param>
			public ChickenDistCalc(ChickenInstance chicken, float baseCoef, float angleCoef, float teleportCoef)
				: base(baseCoef, angleCoef)
			{
				this.instance = chicken;
				this.TeleportCoef = teleportCoef;
			}

			protected override float GetTeleportTime(INode source, INode target)
			{
				return TeleportTimes[new Tuple<NodeType, NodeType>(source.NodeType, target.NodeType)] * TeleportCoef;
			}


			protected override bool CanPass(ITileNode source, ITileNode target)
			{
				if (!CanPassToTileNode(target))
				{
					return false;
				}

				//If the edge is diagonal and there are buildings on both sides of the edge, dont go there
				return source.Tile.MapLocation.X == target.Tile.MapLocation.X ||
						source.Tile.MapLocation.Y == target.Tile.MapLocation.Y ||
						Map.GetTileByMapLocation(new IntVector2(source.Tile.MapLocation.X, target.Tile.MapLocation.Y))
							.Building == null ||
						Map.GetTileByMapLocation(new IntVector2(target.Tile.MapLocation.X, source.Tile.MapLocation.Y))
							.Building == null;
			}

			protected override bool CanPass(ITileNode source, IBuildingNode target)
			{
				return CanPassToBuildingNode(target);
			}

			protected override bool CanPass(IBuildingNode source, ITileNode target)
			{
				return CanPassToTileNode(target);
			}

			protected override bool CanPass(IBuildingNode source, IBuildingNode target)
			{
				return CanPassToBuildingNode(target);
			}

			protected override bool CanTeleport(ITileNode source, ITileNode target)
			{
				return CanPassToTileNode(target);
			}

			protected override bool CanTeleport(ITileNode source, IBuildingNode target)
			{
				return CanPassToBuildingNode(target);
			}

			protected override bool CanTeleport(IBuildingNode source, ITileNode target)
			{
				return CanPassToTileNode(target);
			}

			protected override bool CanTeleport(IBuildingNode source, IBuildingNode target)
			{
				return CanPassToBuildingNode(target);
			}

			bool CanPassToBuildingNode(IBuildingNode target)
			{
				//Is not closed gate door
				return target.Tag != GateInstance.GateDoorTag || ((GateInstance)target.Building.Plugin).IsOpen;
			}

			bool CanPassToTileNode(ITileNode target)
			{
				//Is passable and is not covered by a building
				return instance.myType.PassableTileTypes.Contains(target.Tile.Type) &&
						target.Tile.Building == null;
			}
		}


		AnimationController animationController;
		public WorldWalker Walker { get; private set; }
		public Shooter Shooter{ get; private set; }

		bool dying;

		readonly ChickenDistCalc distCalc;

		float hp;
		HealthBar healthbar;

		IRangeTarget explicitTarget;
		bool targetMoved = false;

		const float timeBetweenTests = 1.0f;
		float shootTestTimer = timeBetweenTests;


		readonly ChickenType myType;

		public ChickenInstance(ILevelManager level, IUnit unit, ChickenType type) 
			:base(level,unit) {

			this.myType = type;

			
			unit.AlwaysVertical = true;
			distCalc = new ChickenDistCalc(this, 1, 1, 1);
		}

		public static ChickenInstance CreateNew(ILevelManager level, IUnit unit, ChickenType type)
		{
			ChickenInstance newChicken = new ChickenInstance(level, unit, type);

			newChicken.animationController = unit.CreateComponent<AnimationController>();
			newChicken.Walker = WorldWalker.CreateNew(newChicken, level);
			newChicken.Shooter = Shooter.CreateNew(newChicken, level, type.ProjectileType, new Vector3(0, 0.7f, -0.7f), 20);
			newChicken.Shooter.SearchForTarget = true;
			newChicken.Shooter.TargetSearchDelay = 2;

			var selector = UnitSelector.CreateNew(newChicken, level);

			MovingRangeTarget.CreateNew(newChicken, level, new Vector3(0, 0.5f, 0));

			newChicken.RegisterEvents(newChicken.Walker, newChicken.Shooter, selector);

			newChicken.hp = 100;
			newChicken.Init(newChicken.hp);

			return newChicken;
		}

		public static ChickenInstance CreateForLoading(ILevelManager level, IUnit unit, ChickenType type)
		{
			return new ChickenInstance(level, unit, type);
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
			sequentialData.GetNext(out hp);
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

		public override void TileHeightChanged(ITile tile)
		{
			Unit.MoveTo(Unit.Position.WithY(Level.Map.GetHeightAt(Unit.XZPosition)));
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
						Walker.GoTo(Level.Map.PathFinding.GetClosestNode(explicitTarget.CurrentPosition));
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

		bool UnitSelector.IUser.ExecuteOrder(Order order)
		{
			order.Executed = false;
			if (order.PlatformOrder)
			{
				switch (order)
				{
					case MoveOrder moveOrder:
						Shooter.StopShooting();
						Shooter.SearchForTarget = false;
						explicitTarget = null;
						order.Executed = Walker.GoTo(moveOrder.Target);
						break;
					case AttackOrder attackOrder:
						Shooter.StopShooting();

						if (Unit.Player.IsEnemy(attackOrder.Target.Player) && (SetExplicitTarget(attackOrder.Target) != null))
						{
							order.Executed = Shooter.ShootAt(explicitTarget) || Walker.GoTo(Level.Map.PathFinding.GetClosestNode(explicitTarget.CurrentPosition));
						}

						if (order.Executed)
						{
							Shooter.SearchForTarget = false;
						}
						break;
				}
			}

			return order.Executed;
		}

		void OnMovementStarted(WorldWalker walker) {
			animationController.PlayExclusive("Assets/Units/Chicken/Models/Walk.ani", 0, true);
			animationController.SetSpeed("Assets/Units/Chicken/Models/Walk.ani", 2);

			Shooter.StopShooting();
			Shooter.SearchForTarget = false;
		}

		void OnMovementFinished(WorldWalker walker) {
			animationController.Stop("Assets/Units/Chicken/Models/Walk.ani");
			Shooter.SearchForTarget = true;
			Shooter.ResetShotDelay();
		}

		void OnMovementFailed(WorldWalker walker) {
			animationController.Stop("Assets/Units/Chicken/Models/Walk.ani");
			Shooter.SearchForTarget = true;
			Shooter.ResetShotDelay();
		}

		void OnMovementCanceled(WorldWalker walker)
		{
			animationController.Stop("Assets/Units/Chicken/Models/Walk.ani");
			Shooter.SearchForTarget = true;
			Shooter.ResetShotDelay();
		}

		void OnUnitSelected(UnitSelector selector) {
			if (Walker.State != WorldWalkerState.Started) {
				animationController.Play("Assets/Units/Chicken/Models/Idle.ani", 0, true);
			}
		}

		void OnTargetAutoAcquired(Shooter shooter) {
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
			target.TargetMoved += ExplicitTargetMoved;

			return target;
		}

		void ExplicitTargetMoved(IRangeTarget target)
		{
			targetMoved = true;
		}

		void RegisterEvents(WorldWalker walker, Shooter shooter, UnitSelector selector)
		{
			walker.MovementStarted += OnMovementStarted;
			walker.MovementFinished += OnMovementFinished;
			walker.MovementFailed += OnMovementFailed;
			walker.MovementCanceled += OnMovementCanceled;

			

			shooter.BeforeShotFired += BeforeShotFired;
			shooter.TargetAutoAcquired += OnTargetAutoAcquired;
			shooter.TargetDestroyed += OnTargetDestroyed;

			selector.UnitSelected += OnUnitSelected;
		}


	}

	class ChickenSpawner : PointSpawner {

		public override Cost Cost => myType.Cost;

		readonly ChickenType myType;

		public ChickenSpawner(GameController input, GameUI ui, CameraMover camera, UnitType type, ChickenType myType)
			: base(input, ui, camera, type)
		{
			this.myType = myType;
		}

		public override IUnit SpawnAt(ITile tile, IPlayer player)
		{
			if (myType.CanSpawnAt(tile) && (Level.EditorMode || myType.Cost.HasResources(player))) {
				return Level.SpawnUnit(UnitType, tile, Quaternion.Identity, player);
			}

			return null;
		}

	}
}
