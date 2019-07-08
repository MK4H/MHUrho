using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using MoreLinq;
using MHUrho.Control;
using MHUrho.EntityInfo;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using MHUrho.Helpers.Extensions;
using MHUrho.WorldMap;
using ShowcasePackage.Buildings;
using ShowcasePackage.Levels;
using ShowcasePackage.Misc;
using Urho;

namespace ShowcasePackage.Units
{
	public class DogType : UnitTypePlugin {

		public static string TypeName = "Dog";
		public static int TypeID = 4;

		public override string Name => TypeName;
		public override int ID => TypeID;

		const string PassableTileTypesElement = "canPass";

		public ViableTileTypes PassableTileTypes { get; private set; }

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit)
		{
			return new DogInstance(level, unit, this, false);
		}

		public override UnitInstancePlugin GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return new DogInstance(level, unit, this, true);
		}

		public override bool CanSpawnAt(ITile centerTile)
		{
			return PassableTileTypes.IsViable(centerTile) &&
					centerTile.Building == null;

		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			XElement canPass =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(PassableTileTypesElement));
			PassableTileTypes = ViableTileTypes.FromXml(canPass, package);
		}
	}

	public class DogInstance : UnitInstancePlugin, 
								WorldWalker.IUser, 
								MovingRangeTarget.IUser {



		/// <summary>
		/// State for the State design pattern
		/// </summary>
		abstract class State
		{

			public abstract States Current { get; }

			protected DogInstance Dog;

			protected State(DogInstance dog)
			{
				this.Dog = dog;
			}

			public static State Load(SequentialPluginDataReader reader, DogInstance dog)
			{
				reader.GetNext(out int stateInt);
				States savedState = (States)stateInt;
				switch (savedState)
				{
					case States.GoingToTree:
						return new GoingToTree(reader, dog);
					case States.Chomping:
						return new Chomping(reader, dog);
					case States.BringingWood:
						return new BringingWood(reader, dog);
					case States.SearchingForTree:
						return new SearchingForTree(reader, dog);
					default:
						throw new ArgumentException("Provided data reader contains invalid data");
				}
			}

			public abstract void Save(SequentialPluginDataWriter writer);

			public virtual void OnUpdate(float timeStep)
			{

			}

			/// <summary>
			/// Switch from this state to <paramref name="newState"/>.
			/// </summary>
			/// <param name="newState">New state we are switching to.</param>
			public virtual void SwitchingTo(State newState)
			{

			}

			public virtual void MovementStarted()
			{

			}

			public virtual void MovementFinished()
			{

			}

			public virtual void MovementFailed()
			{

			}

			public virtual void MovementCanceled()
			{

			}
		}

		/// <summary>
		/// State representing the state when the unit is going from building to the tree
		/// </summary>
		class GoingToTree : State
		{

			public override States Current => States.GoingToTree;

			public GoingToTree(State oldState, DogInstance dog)
				: base(dog)
			{

			}

			public GoingToTree(SequentialPluginDataReader reader, DogInstance dog)
				: base(dog)
			{ }

			public override void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext((int)Current);
			}

			public override void OnUpdate(float timeStep)
			{
				Tree tree = CheckTree();
				GoToTree(tree);
			}

			public override void MovementFailed()
			{
				Tree tree = CheckTree();
				GoToTree(tree);
			}

			public override void MovementCanceled()
			{
				Tree tree = CheckTree();
				GoToTree(tree);
			}

			public override void MovementFinished()
			{
				Dog.currentState = new Chomping(this, Dog);
			}

			Tree CheckTree()
			{
				if (Dog.targetTree == null || Dog.targetTree.Entity.IsRemovedFromLevel)
				{
					Dog.currentState = new SearchingForTree(this, Dog);
				}

				return Dog.targetTree;
			}

			void GoToTree(Tree tree)
			{
				if (Dog.walker.State != WorldWalkerState.Started)
				{
					foreach (var neighbour in tree.Building.Tiles[0].GetNeighbours())
					{
						if (Dog.walker.GoTo(Dog.Level.Map.PathFinding.GetTileNode(neighbour)))
						{
							return;
						}
					}

					//Cannot get to tree
					Dog.inaccesibleTrees.Add(tree.Building);
					Dog.currentState = new SearchingForTree(this, Dog);
				}
			}
		}

		/// <summary>
		/// Represents a state when the unit is standing next to the tree and waiting to chop it down.
		/// </summary>
		class Chomping : State
		{

			public override States Current => States.Chomping;


			const double duration = 10;
			readonly Timeout chomping;

			public Chomping(State oldState, DogInstance dog)
				: base(dog)
			{
				chomping = new Timeout(duration);
			}

			public Chomping(SequentialPluginDataReader reader, DogInstance dog)
				: base(dog)
			{
				reader.GetNext(out double remaining);
				chomping = new Timeout(duration, remaining);
			}


			public override void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext((int)Current);
				writer.StoreNext(chomping.Remaining);
			}

			public override void OnUpdate(float timeStep)
			{
				if (chomping.Update(timeStep))
				{
					if (Dog.targetTree == null || Dog.targetTree.Building.IsRemovedFromLevel)
					{
						Dog.currentState = new SearchingForTree(this, Dog);
					}
					else
					{
						Dog.targetTree.Chomp();
						Dog.currentState = new BringingWood(this, Dog);
					}

				}
			}
		}

		/// <summary>
		/// Represents the state when the unit is going back from the tree with the chopped wood.
		/// </summary>
		class BringingWood : State
		{

			public override States Current => States.BringingWood;

			/// <summary>
			/// Time to destruction when we cannot find path back to cutter
			/// </summary>
			const double DestructionTime = 5;
			Timeout destruction = null;

			public BringingWood(State oldState, DogInstance dog)
				: base(dog)
			{ }

			public BringingWood(SequentialPluginDataReader reader, DogInstance dog)
				: base(dog)
			{
				reader.GetNext(out bool isDestructing);
				if (isDestructing)
				{
					reader.GetNext(out double remaining);
					destruction = new Timeout(DestructionTime, remaining);
				}
			}


			public override void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext((int)Current);
				writer.StoreNext(destruction != null);
			}

			public override void OnUpdate(float timeStep)
			{
				if (destruction == null && Dog.walker.State != WorldWalkerState.Started)
				{
					GoToCutter();
					return;
				}

				if (destruction != null && destruction.Update(timeStep))
				{
					if (!GoToCutter())
					{
						Dog.Unit.RemoveFromLevel();
					}
				}
			}

			public override void MovementFinished()
			{
				Dog.Unit.Player.ChangeResourceAmount(Dog.Cutter.ProducedResource, 1);
				Dog.currentState = new GoingToTree(this, Dog);
			}

			public override void MovementFailed()
			{
				GoToCutter();
			}

			public override void MovementCanceled()
			{
				GoToCutter();
			}

			bool GoToCutter()
			{
				foreach (var neighbour in Dog.Cutter.Building.Tiles[0].GetNeighbours())
				{
					if (Dog.walker.GoTo(Dog.Level.Map.PathFinding.GetTileNode(neighbour)))
					{
						destruction = null;
						return true;
					}
				}


				//Could not find path back to cutter
				if (destruction == null)
				{
					destruction = new Timeout(DestructionTime);
				}
				return false;
			}
		}

		/// <summary>
		/// Represents the state when unit is searching for trees to chop down.
		/// </summary>
		class SearchingForTree : State
		{

			public override States Current => States.SearchingForTree;

			const double searchTimeout = 5;

			readonly Timeout timeout;

			public SearchingForTree(State oldState, DogInstance dog)
				: base(dog)
			{
				this.timeout = new Timeout(searchTimeout);
			}

			public SearchingForTree(SequentialPluginDataReader reader, DogInstance dog)
				: base(dog)
			{
				reader.GetNext<double>(out double remaining);
				timeout = new Timeout(searchTimeout, remaining);
			}

			public override void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext((int)Current);
				writer.StoreNext(timeout.Remaining);
			}

			public override void OnUpdate(float timeStep)
			{
				if (timeout.Update(timeStep))
				{
					timeout.Reset();


					var trees = Dog.Level
									.NeutralPlayer
									.GetBuildingsOfType(Dog.Level.Package.GetBuildingType(TreeType.TypeID));
					if (trees.Count == 0)
					{
						return;
					}

					var closest = trees.Where(tree => !Dog.inaccesibleTrees.Contains(tree)).MinBy((tree) => Vector3.Distance(Dog.Entity.Position, tree.Position)).First();
					Dog.targetTree = (Tree)closest.Plugin;

					Dog.currentState = new GoingToTree(this, Dog);
				}
			}
		}


		/// <summary>
		/// Represents the view of the pathfinding graph by dog unit.
		/// Rejects edges that would not be present and returns the weights of the rest of the edges.
		/// </summary>
		class DogDistCalc : ClimbingDistCalc {
			static readonly Dictionary<Tuple<NodeType, NodeType>, float> TeleportTimes =
				new Dictionary<Tuple<NodeType, NodeType>, float>
				{
					{Tuple.Create(NodeType.Tile, NodeType.Tile), 1},
					{Tuple.Create(NodeType.Tile, NodeType.Building), 10},
					{Tuple.Create(NodeType.Tile, NodeType.Temp), 1},
					{Tuple.Create(NodeType.Building, NodeType.Tile), 1},
					{Tuple.Create(NodeType.Building, NodeType.Building), 10},
					{Tuple.Create(NodeType.Building, NodeType.Temp), 1},
					{Tuple.Create(NodeType.Temp, NodeType.Tile), 1},
					{Tuple.Create(NodeType.Temp, NodeType.Building), 10},
					{Tuple.Create(NodeType.Temp, NodeType.Temp), 1},
				};


			/// <summary>
			/// Coefficient of teleport times compared to base teleport times.
			/// </summary>
			public float TeleportCoef { get; set; }

			ILevelManager Level => instance.Level;
			IMap Map => Level.Map;

			readonly DogInstance instance;



			/// <summary>
			/// Creates new instance of Dog distance calculator.
			/// </summary>
			/// <param name="baseCoef">Coefficient of linear motion speed.</param>
			/// <param name="angleCoef">Coefficient how much the linear motion speed is affected by angle.</param>
			/// <param name="teleportCoef">Coefficient of teleport times.</param>
			public DogDistCalc(DogInstance dog, float baseCoef, float angleCoef, float teleportCoef)
				:base(baseCoef, angleCoef)
			{
				this.instance = dog;
				this.TeleportCoef = teleportCoef;
			}

			protected override float GetTeleportTime(INode source, INode target)
			{
				return TeleportTimes[new Tuple<NodeType, NodeType>(source.NodeType, target.NodeType)] * TeleportCoef;
			}


			protected override bool CanPass(ITileNode source, ITileNode target)
			{
				if (!CanPassToTileNode(target)) {
					return false;
				}

				//If the edge is diagonal and there are buildings on both sides of the edge, dont go there
				return source.Tile.MapLocation.X == target.Tile.MapLocation.X || 
						source.Tile.MapLocation.Y == target.Tile.MapLocation.Y || 
						Map.GetTileByMapLocation(new IntVector2(source.Tile.MapLocation.X, target.Tile.MapLocation.Y))
							.Building == null || 
						Map.GetTileByMapLocation(new IntVector2(target.Tile.MapLocation.X,source.Tile.MapLocation.Y))
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
				return (target.Tag != Gate.GateDoorTag || ((Gate)target.Building.Plugin).IsOpen) &&
						!((LevelInstancePluginBase)Level.Plugin).IsRoofNode(target);
			}

			bool CanPassToTileNode(ITileNode target)
			{
				//Is passable and is not covered by a building
				return instance.myType.PassableTileTypes.Contains(target.Tile.Type) &&
						target.Tile.Building == null;
			}
		}

		public TreeCutter Cutter { get; set; }

		readonly Vector3 targetOffset = new Vector3(0, 0.5f, 0);

		AnimationController animationController;
		WorldWalker walker;

		HealthBarControl healthBar;

		enum States { GoingToTree, Chomping, BringingWood, SearchingForTree}

		State currentState;

		Tree targetTree;

		readonly DogDistCalc distCalc;
		readonly HashSet<IBuilding> inaccesibleTrees = new HashSet<IBuilding>();
		readonly Timeout clean = new Timeout(120);
		readonly DogType myType;

		public DogInstance(ILevelManager level, IUnit unit, DogType myType, bool loading)
			: base(level, unit)
		{
			this.myType = myType;
			this.distCalc = new DogDistCalc(this, 0.5f, 0.2f, 1);
			unit.AlwaysVertical = false;

			if (loading) {
				return;
			}

			animationController = unit.CreateComponent<AnimationController>();
			walker = WorldWalker.CreateNew(this, level);
			RegisterEvents(walker);
			MovingRangeTarget.CreateNew(this, level, targetOffset);
			healthBar = new HealthBarControl(level, unit, 100, new Vector3(0, 3, 0), new Vector2(0.5f, 0.1f), true);
			currentState = new SearchingForTree((State)null, this);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			healthBar.Save(writer);
			currentState.Save(writer);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			animationController = Unit.CreateComponent<AnimationController>();
			walker = Unit.GetDefaultComponent<WorldWalker>();

			RegisterEvents(walker);

			var reader = pluginData.GetReaderForWrappedSequentialData();
			healthBar = HealthBarControl.Load(Level, Unit, reader);
			currentState = State.Load(reader, this);
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

			if (healthBar.ChangeHitPoints(-30))
			{
				walker.Enabled = false;
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

			if (!healthBar.ChangeHitPoints(-(damage*2))) {
				walker.Enabled = false;
				Unit.RemoveFromLevel();
			}
		}

		public override void OnUpdate(float timeStep)
		{
			if (clean.Update(timeStep)) {
				inaccesibleTrees.Clear();
			}

			currentState.OnUpdate(timeStep);
		}

		INodeDistCalculator WorldWalker.IUser.GetNodeDistCalculator()
		{
			return distCalc;
		}

		IEnumerable<Waypoint> MovingRangeTarget.IUser.GetFutureWaypoints(MovingRangeTarget target)
		{
			return walker.GetRestOfThePath(targetOffset);
		}

		void OnMovementStarted(WorldWalker walker)
		{
			animationController.PlayExclusive("Assets/Units/Dog/Walk.ani", 0, true);
			animationController.SetSpeed("Assets/Units/Dog/Walk.ani", 2);

			currentState.MovementStarted();
		}

		void OnMovementFinished(WorldWalker walker)
		{
			animationController.Stop("Assets/Units/Dog/Walk.ani");

			currentState.MovementFinished();
		}

		void OnMovementFailed(WorldWalker walker)
		{
			animationController.Stop("Assets/Units/Dog/Walk.ani");

			currentState.MovementFailed();
		}

		void OnMovementCanceled(WorldWalker walker)
		{
			animationController.Stop("Assets/Units/Dog/Walk.ani");

			currentState.MovementCanceled();
		}

		void RegisterEvents(WorldWalker walker)
		{
			walker.MovementStarted += OnMovementStarted;
			walker.MovementFinished += OnMovementFinished;
			walker.MovementFailed += OnMovementFailed;
			walker.MovementCanceled += OnMovementCanceled;

		}

	}
}
