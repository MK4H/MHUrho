using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.Control;
using MHUrho.DefaultComponents;
using MHUrho.EntityInfo;
using MHUrho.Helpers.Extensions;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;
using MoreLinq;
using ShowcasePackage.Buildings;
using ShowcasePackage.Levels;
using ShowcasePackage.Misc;
using Urho;

namespace ShowcasePackage.Units
{
	public class WolfType : SpawnableUnitTypePlugin {

		public static string TypeName = "Wolf";
		public static int TypeID = 5;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public override Cost Cost => cost;
		public override UnitType UnitType => myType;

		public ViableTileTypes PassableTileTypes { get; private set; }

		const string CostElement = "cost";
		const string PassableTileTypesElement = "canPass";

		Cost cost;
		UnitType myType;

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			XElement costElem = extensionElement.Element(package.PackageManager.GetQualifiedXName(CostElement));
			cost = Cost.FromXml(costElem, package);

			XElement canPass =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(PassableTileTypesElement));
			PassableTileTypes = ViableTileTypes.FromXml(canPass, package);

			myType = package.GetUnitType(ID);
		}

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit)
		{
			return Wolf.CreateNew(level, unit, this);
		}

		public override UnitInstancePlugin GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return Wolf.CreateForLoading(level, unit, this);
		}

		public override bool CanSpawnAt(ITile centerTile)
		{
			//Can only spawn on a tile with no buildings and no other units
			return PassableTileTypes.IsViable(centerTile) &&
					(centerTile.Building == null ||
					centerTile.Building.Plugin is WalkableBuildingPlugin);
		}

		public override Spawner GetSpawner(GameController input, GameUI ui, CameraMover camera)
		{
			return new WolfSpawner(input, ui, camera, myType, this);
		}
	}

	public class Wolf : UnitInstancePlugin,
						WorldWalker.IUser,
						MovingRangeTarget.IUser,
						UnitSelector.IUser,
						MovingMeeleAttacker.IUser
	{

		/// <summary>
		/// Records the tiles at which the unit would be blocked during normal pathfinding by a building
		/// </summary>
		public class WolfDistCalcThroughWalls : WolfDistCalc
		{

			/// <summary>
			/// Normaly blocked by a building
			/// </summary>
			public HashSet<ITile> CanBreakThrough { get; private set; }

			public WolfDistCalcThroughWalls(Wolf wolf)
				: base(wolf, 1, 1, 1)
			{
				CanBreakThrough = new HashSet<ITile>();
			}

			protected override bool CanPass(ITileNode source, ITileNode target)
			{
				if (base.CanPass(source, target))
				{
					return true;
				}

				if (!CanPassToTile(target)) {
					return false;
				}

				//Diagonal
				if (source.Tile.MapLocation.X != target.Tile.MapLocation.X &&
					source.Tile.MapLocation.Y != target.Tile.MapLocation.Y) {
					//Blocked by own or neutral buildings we can't destroy

					var building1 = Map
									.GetTileByMapLocation(new IntVector2(source.Tile.MapLocation.X,
																		target.Tile.MapLocation.Y))
									.Building;
					var building2 = Map
									.GetTileByMapLocation(new IntVector2(target.Tile.MapLocation.X,
																		source.Tile.MapLocation.Y))
									.Building;
					if ((building1.Player == Level.NeutralPlayer || Instance.Unit.Player.IsFriend(building1.Player)) &&
						(building2.Player == Level.NeutralPlayer || Instance.Unit.Player.IsFriend(building2.Player))) {
						return false;
					}
				}

				CanBreakThrough.Add(target.Tile);
				return true;
			}

			protected override bool CanPass(ITileNode source, IBuildingNode target)
			{
				if (base.CanPass(source, target))
				{
					return true;
				}

				if (!CanPassToBuilding(target)) {
					return false;
				}

				CanBreakThrough.Add(Map.GetContainingTile(target.Position));
				return true;
			}

			protected override bool CanPass(IBuildingNode source, ITileNode target)
			{
				if (base.CanPass(source, target))
				{
					return true;
				}


				if (!CanPassToTile(target))
				{
					return false;
				}

				CanBreakThrough.Add(target.Tile);
				return true;
			}

			protected override bool CanPass(IBuildingNode source, IBuildingNode target)
			{
				if (base.CanPass(source, target))
				{
					return true;
				}


				if (!CanPassToBuilding(target))
				{
					return false;
				}

				CanBreakThrough.Add(Map.GetContainingTile(target.Position));
				return true;
			}

			protected override bool CanTeleport(ITileNode source, ITileNode target)
			{
				if (base.CanTeleport(source, target))
				{
					return true;
				}


				if (!CanPassToTile(target))
				{
					return false;
				}

				CanBreakThrough.Add(target.Tile);
				return true;
			}

			protected override bool CanTeleport(ITileNode source, IBuildingNode target)
			{
				if (base.CanPass(source, target))
				{
					return true;
				}

				if (!CanPassToBuilding(target))
				{
					return false;
				}

				CanBreakThrough.Add(Map.GetContainingTile(target.Position));

				return true;
			}

			protected override bool CanTeleport(IBuildingNode source, ITileNode target)
			{
				if (base.CanTeleport(source, target))
				{
					return true;
				}

				if (!CanPassToTile(target)) {
					return false;
				}


				CanBreakThrough.Add(target.Tile);
				return true;
			}

			protected override bool CanTeleport(IBuildingNode source, IBuildingNode target)
			{
				if (base.CanPass(source, target))
				{
					return true;
				}

				if (!CanPassToBuilding(target))
				{
					return false;
				}

				CanBreakThrough.Add(Map.GetContainingTile(target.Position));
				return true;
			}

			bool CanPassToTile(ITileNode target)
			{
				if (!Instance.myType.PassableTileTypes.Contains(target.Tile.Type))
				{
					return false;
				}

				if (target.Tile.Building != null &&
					(target.Tile.Building.Player == Level.NeutralPlayer ||
					Instance.Unit.Player.IsFriend(target.Tile.Building.Player)))
				{
					return false;
				}
				return true;
			}

			bool CanPassToBuilding(IBuildingNode target)
			{
				if (Instance.Unit.Player.IsFriend(target.Building.Player))
				{
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Represents the view of the pathfinding graph by wolf unit.
		/// Rejects edges that would not be present and returns the weights of the rest of the edges.
		/// </summary>
		public class WolfDistCalc : ClimbingDistCalc
		{
			static readonly Dictionary<Tuple<NodeType, NodeType>, float> TeleportTimes =
				new Dictionary<Tuple<NodeType, NodeType>, float>
				{
					{Tuple.Create(NodeType.Tile, NodeType.Tile), 1},
					{Tuple.Create(NodeType.Tile, NodeType.Building), 1},
					{Tuple.Create(NodeType.Tile, NodeType.Temp), 1},
					{Tuple.Create(NodeType.Building, NodeType.Tile), 1},
					{Tuple.Create(NodeType.Building, NodeType.Building), 1},
					{Tuple.Create(NodeType.Building, NodeType.Temp), 1},
					{Tuple.Create(NodeType.Temp, NodeType.Tile), 1},
					{Tuple.Create(NodeType.Temp, NodeType.Building), 1},
					{Tuple.Create(NodeType.Temp, NodeType.Temp), 1},
				};

			/// <summary>
			/// Coefficient of teleport times compared to base teleport times.
			/// </summary>
			public float TeleportCoef { get; set; }

			protected ILevelManager Level => Instance.Level;
			protected IMap Map => Level.Map;

			protected readonly Wolf Instance;



			/// <summary>
			/// Creates new instance of Wolf distance calculator.
			/// </summary>
			/// <param name="wolf">The instance this calculator is calculating for.</param>
			/// <param name="baseCoef">Coefficient of linear motion speed.</param>
			/// <param name="angleCoef">Coefficient how much the linear motion speed is affected by angle.</param>
			/// <param name="teleportCoef">Coefficient of teleport times.</param>
			public WolfDistCalc(Wolf wolf, float baseCoef, float angleCoef, float teleportCoef)
				:base(baseCoef, angleCoef)
			{
				this.Instance = wolf;
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
				//Is not closed gate door and is not roof
				return (target.Tag != Gate.GateDoorTag || ((Gate) target.Building.Plugin).IsOpen);
			}

			bool CanPassToTileNode(ITileNode target)
			{
				//Is passable and is not covered by a building
				return Instance.myType.PassableTileTypes.Contains(target.Tile.Type) &&
						target.Tile.Building == null;
			}
		}

		static readonly Vector3 targetOffset = new Vector3(0, 0.5f, 0);

		public bool AttackMove { get; set; }

		AnimationController animationController;
		WorldWalker walker;
		MovingMeeleAttacker attacker;

		HealthBarControl healthBar;

		readonly WolfDistCalc distCalc;

		readonly WolfType myType;

		Wolf(ILevelManager level, IUnit unit, WolfType myType)
			:base(level, unit)
		{
			this.myType = myType;
			this.distCalc = new WolfDistCalc(this, 0.5f, 0.2f, 1);
			this.AttackMove = true;
		}

		public static Wolf CreateNew(ILevelManager level, IUnit unit, WolfType myType)
		{
			Wolf wolf = new Wolf(level, unit, myType);

			wolf.animationController = CreateAnimationController(unit);
			wolf.walker = WorldWalker.CreateNew(wolf, level);
			wolf.attacker = MovingMeeleAttacker.CreateNew(wolf,
													level,
													true,
													new IntVector2(20, 20),
													1,
													5,
													0.5f);
			wolf.healthBar = new HealthBarControl(level, unit, 100, new Vector3(0, 0.7f, 0), new Vector2(0.5f, 0.1f), true);
			UnitSelector.CreateNew(wolf, level);
			MovingRangeTarget.CreateNew(wolf, level, targetOffset);
			unit.AlwaysVertical = false;
			wolf.RegisterEvents();
			return wolf;
		}

		public static Wolf CreateForLoading(ILevelManager level, IUnit unit, WolfType myType)
		{
			return new Wolf(level, unit, myType);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			healthBar.Save(writer);
			writer.StoreNext(AttackMove);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			animationController = CreateAnimationController(Unit);
			walker = Unit.GetDefaultComponent<WorldWalker>();
			attacker = Unit.GetDefaultComponent<MovingMeeleAttacker>();

			RegisterEvents();
			var reader = pluginData.GetReaderForWrappedSequentialData();
			healthBar = HealthBarControl.Load(Level, Unit, reader);
			reader.GetNext(out bool attackMove);
			AttackMove = attackMove;
		}

		public override void Dispose()
		{
			healthBar.Dispose();
		}

		public override void TileHeightChanged(ITile tile)
		{
			Unit.MoveTo(Unit.Position.WithY(Level.Map.GetHeightAt(Unit.XZPosition)));
		}

		public override void BuildingDestroyed(IBuilding building, ITile tile)
		{
			walker.Stop();
			Unit.MoveTo(Unit.Position.WithY(Level.Map.GetTerrainHeightAt(Unit.Position.XZ2())));

			if (healthBar.ChangeHitPoints(-30)) {
				walker.Enabled = false;
				attacker.Enabled = false;
				Unit.RemoveFromLevel();
			}
		}


		public override void BuildingBuilt(IBuilding building, ITile tile)
		{
			throw new InvalidOperationException("Building building on top of units is not supported.");
		}

		public override void OnHit(IEntity other, object userData)
		{
			if (Unit.Player.IsFriend(other.Player))
			{
				return;
			}

			int damage = (int)userData;

			if (!healthBar.ChangeHitPoints(-(damage * 0.5)))
			{
				walker.Enabled = false;
				attacker.Enabled = false;
				Unit.RemoveFromLevel();
			}
		}

		public bool ExecuteOrder(Order order)
		{
			order.Executed = false;
			if (order.PlatformOrder)
			{
				switch (order)
				{
					case MoveOrder moveOrder:
						order.Executed = walker.GoTo(moveOrder.Target);
						break;
					case AttackOrder attackOrder:
						if (Unit.Player.IsEnemy(attackOrder.Target.Player))
						{
							attacker.Attack(attackOrder.Target);
							order.Executed = true;
						}

						if (order.Executed)
						{
							attacker.SearchForTarget = false;
						}
						break;
				}
			}

			return order.Executed;
		}

		INodeDistCalculator WorldWalker.IUser.GetNodeDistCalculator()
		{
			return distCalc;
		}

		IEnumerable<Waypoint> MovingRangeTarget.IUser.GetFutureWaypoints(MovingRangeTarget target)
		{
			return walker.GetRestOfThePath(targetOffset);
		}

		

		bool MeeleAttacker.IBaseUser.IsInRange(MeeleAttacker attacker, IEntity target)
		{
			if (target is IBuilding building) {
				IntRect rect = building.Rectangle;
				rect.Top += 1;
				rect.Bottom -= 2;
				rect.Left += 1;
				rect.Right -= 2;
				return rect.Contains(Unit.XZPosition);
			}
			return Vector3.Distance(target.Position, Unit.Position) < 1.5;
		}

		IUnit MeeleAttacker.IBaseUser.PickTarget(ICollection<IUnit> possibleTargets)
		{
			return possibleTargets.MinBy((target) => Vector3.Distance(target.Position, Unit.Position)).FirstOrDefault();
		}

		bool MovingMeeleAttacker.IUser.MoveTo(IEntity target)
		{
			if (target is IBuilding building) {
				ITile[] tiles =
				{
					GetTileNextToBuilding(building, building.Forward),
					GetTileNextToBuilding(building, building.Backward),
					GetTileNextToBuilding(building, building.Left),
					GetTileNextToBuilding(building, building.Right),
				};

				foreach (var tile in tiles.OrderBy(tile => Vector3.Distance(tile.Center3, Unit.Position))) {
					if (walker.GoTo(Level.Map.PathFinding.GetTileNode(tile))) {
						return true;
					}
				}

				return false;
			}
			else {
				return walker.GoTo(Level.Map.PathFinding.GetClosestNode(target.Position));
			}
		}

		void OnMovementStarted(WorldWalker walker)
		{
			animationController.PlayExclusive("Assets/Units/Wolf/Models/Wolf_Run_cycle_.ani", 0, true);
			animationController.SetSpeed("Assets/Units/Wolf/Models/Wolf_Run_cycle_.ani", 1);
		}

		void OnMovementFinished(WorldWalker walker)
		{
			animationController.Stop("Assets/Units/Wolf/Models/Wolf_Run_cycle_.ani");
		}

		void OnMovementFailed(WorldWalker walker)
		{
			animationController.Stop("Assets/Units/Wolf/Models/Wolf_Run_cycle_.ani");
		}

		void OnMovementCanceled(WorldWalker walker)
		{
			animationController.Stop("Assets/Units/Wolf/Models/Wolf_Run_cycle_.ani");
		}

		void RegisterEvents()
		{
			walker.MovementStarted += OnMovementStarted;
			walker.MovementFinished += OnMovementFinished;
			walker.MovementFailed += OnMovementFailed;
			walker.MovementCanceled += OnMovementCanceled;
			attacker.Attacked += Attack;
			attacker.TargetInRange += TargetInRange;
			attacker.TargetLost += TargetLost;
		}

		void TargetLost(MeeleAttacker attacker)
		{
			attacker.SearchForTarget = true;
		}

		void TargetInRange(MeeleAttacker attacker, IEntity target)
		{
			walker.Stop();
		}

		void Attack(MeeleAttacker attacker, IEntity target)
		{
			target.HitBy(Unit, 10);
		}

		static AnimationController CreateAnimationController(IUnit unit)
		{
			//Animation controller has to be on the same node as animatedModel
			var modelNode = unit.Node.GetComponent<AnimatedModel>(true).Node;
			return modelNode.CreateComponent<AnimationController>();
		}

		ITile GetTileNextToBuilding(IBuilding building, Vector3 direction)
		{
			return Level.Map.GetContainingTile(building.Center +
												new Vector3(direction.X * (building.Size.X / 2 + 1), 0, direction.Z * (building.Size.Y / 2 + 1)));
		}
	}

	class WolfSpawner : PointSpawner {

		public override Cost Cost => myType.Cost;

		readonly WolfType myType;

		public WolfSpawner(GameController input, GameUI ui, CameraMover camera, UnitType type, WolfType myType)
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
