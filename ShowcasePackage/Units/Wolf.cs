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
			return centerTile.Building == null && centerTile.Units.Count == 0;
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

		class WolfDistCalc : ClimbingDistCalc
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

			ILevelManager Level => instance.Level;
			IMap Map => Level.Map;

			readonly Wolf instance;



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
				this.instance = wolf;
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
				return (target.Tag != GateInstance.GateDoorTag || ((GateInstance) target.Building.Plugin).IsOpen);
			}

			bool CanPassToTileNode(ITileNode target)
			{
				//Is passable and is not covered by a building
				return instance.myType.PassableTileTypes.Contains(target.Tile.Type) &&
						target.Tile.Building == null;
			}
		}


		static readonly Vector3 targetOffset = new Vector3(0, 0.5f, 0);

		AnimationController animationController;
		WorldWalker walker;
		MovingMeeleAttacker attacker;

		readonly WolfDistCalc distCalc;

		float hp;
		HealthBar healthbar;

		readonly WolfType myType;

		Wolf(ILevelManager level, IUnit unit, WolfType myType)
			:base(level, unit)
		{
			this.myType = myType;
			this.distCalc = new WolfDistCalc(this, 0.5f, 0.2f, 1);
		}

		public static Wolf CreateNew(ILevelManager level, IUnit unit, WolfType myType)
		{
			Wolf wolf = new Wolf(level, unit, myType);
			wolf.animationController = unit.CreateComponent<AnimationController>();
			wolf.walker = WorldWalker.CreateNew(wolf, level);
			wolf.attacker = MovingMeeleAttacker.CreateNew(wolf,
													level,
													true,
													new IntVector2(20, 20),
													1,
													5,
													0.5f);
			UnitSelector.CreateNew(wolf, level);
			MovingRangeTarget.CreateNew(wolf, level, targetOffset);
			unit.AlwaysVertical = false;
			wolf.hp = 100;
			wolf.healthbar = new HealthBar(level, unit, new Vector3(0, 15, 0), new Vector2(0.5f, 0.1f), wolf.hp);

			return wolf;
		}

		public static Wolf CreateForLoading(ILevelManager level, IUnit unit, WolfType myType)
		{
			return new Wolf(level, unit, myType);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var sequentialData = pluginData.GetWriterForWrappedSequentialData();
			sequentialData.StoreNext(hp);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			animationController = Unit.CreateComponent<AnimationController>();
			walker = Unit.GetDefaultComponent<WorldWalker>();
			attacker = Unit.GetDefaultComponent<MovingMeeleAttacker>();

			RegisterEvents(walker);
			var sequentialData = pluginData.GetReaderForWrappedSequentialData();
			sequentialData.GetNext(out hp);
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 15, 0), new Vector2(0.5f, 0.1f), hp);
		}

		public override void Dispose()
		{
			healthbar.Dispose();
		}

		public override void TileHeightChanged(ITile tile)
		{
			Unit.MoveTo(Unit.Position.WithY(Level.Map.GetHeightAt(Unit.XZPosition)));
		}

		public override void OnHit(IEntity other, object userData)
		{
			if (other.Player == Unit.Player)
			{
				return;
			}

			hp -= 5;
			if (hp < 0)
			{
				healthbar.SetHealth(0);
				walker.Enabled = false;
				attacker.Enabled = false;
				Unit.RemoveFromLevel();
			}
			else
			{
				healthbar.SetHealth((int)hp);
			}
		}

		INodeDistCalculator WorldWalker.IUser.GetNodeDistCalculator()
		{
			return distCalc;
		}

		IEnumerable<Waypoint> MovingRangeTarget.IUser.GetFutureWaypoints(MovingRangeTarget target)
		{
			return walker.GetRestOfThePath(targetOffset);
		}

		bool UnitSelector.IUser.ExecuteOrder(Order order)
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

		bool MeeleAttacker.IBaseUser.IsInRange(MeeleAttacker attacker, IEntity target)
		{
			return Vector3.Distance(target.Position, Unit.Position) < 1;
		}

		IUnit MeeleAttacker.IBaseUser.PickTarget(ICollection<IUnit> possibleTargets)
		{
			return possibleTargets.MinBy((target) => Vector3.Distance(target.Position, Unit.Position)).FirstOrDefault();
		}

		void MovingMeeleAttacker.IUser.MoveTo(Vector3 position)
		{
			walker.GoTo(Level.Map.PathFinding.GetClosestNode(position));
		}

		void OnMovementStarted(WorldWalker walker)
		{
			animationController.PlayExclusive("Units/Wolf/Wolf_Walk_cycle_.ani", 0, true);
			animationController.SetSpeed("Units/Wolf/Wolf_Walk_cycle_.ani", 2);
		}

		void OnMovementFinished(WorldWalker walker)
		{
			animationController.Stop("Units/Wolf/Wolf_Walk_cycle_.ani");
		}

		void OnMovementFailed(WorldWalker walker)
		{
			animationController.Stop("Units/Wolf/Wolf_Walk_cycle_.ani");
		}

		void OnMovementCanceled(WorldWalker walker)
		{
			animationController.Stop("Units/Wolf/Wolf_Walk_cycle_.ani");
		}

		void RegisterEvents(WorldWalker walker)
		{
			walker.MovementStarted += OnMovementStarted;
			walker.MovementFinished += OnMovementFinished;
			walker.MovementFailed += OnMovementFailed;
			walker.MovementCanceled += OnMovementCanceled;

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
