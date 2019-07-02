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
using ShowcasePackage.Buildings;
using ShowcasePackage.Misc;
using Urho;

namespace ShowcasePackage.Units
{
	public class DogType : UnitTypePlugin {
		public override string Name => "Dog";
		public override int ID => 4;
		

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit)
		{
			return new DogInstance(level, unit, false);
		}

		public override UnitInstancePlugin GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return new DogInstance(level, unit, true);
		}

		public override bool CanSpawnAt(ITile centerTile)
		{
			return centerTile.Building == null && centerTile.Units.Count == 0;

		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}
	}

	public class DogInstance : UnitInstancePlugin, 
								WorldWalker.IUser, 
								MovingRangeTarget.IUser {

		public TreeCutter Cutter { get; set; }

		readonly Vector3 targetOffset = new Vector3(0, 0.5f, 0);

		AnimationController animationController;
		WorldWalker walker;

		readonly ClimbingDistCalc distCalc = new ClimbingDistCalc(0.5f, 0.2f);

		float hp;
		HealthBar healthbar;

		enum States { GoingToTree, Chomping, BringingWood, SearchingForTree}

		State currentState;

		Tree targetTree;

		readonly HashSet<IBuilding> inaccesibleTrees = new HashSet<IBuilding>();
		readonly Timeout clean = new Timeout(120);

		abstract class State {

			public abstract States Current { get; }

			protected DogInstance Dog;

			protected State(DogInstance dog)
			{
				this.Dog = dog;
			}

			public static State Load(SequentialPluginDataReader reader, DogInstance dog)
			{
				States savedState = (States)reader.GetNext<int>();
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

		class GoingToTree : State {

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
				writer.StoreNext((int) Current);
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

		class Chomping : State {

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
				double remaining = reader.GetNext<double>();
				chomping = new Timeout(duration, remaining);
			}


			public override void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext((int)Current);
				writer.StoreNext(chomping.Remaining);
			}

			public override void OnUpdate(float timeStep)
			{
				if (chomping.Update(timeStep)) {
					if (Dog.targetTree == null || Dog.targetTree.Building.IsRemovedFromLevel) {
						Dog.currentState = new SearchingForTree(this, Dog);
					}
					else {
						Dog.targetTree.Chomp();
						Dog.currentState = new BringingWood(this, Dog);
					}
					
				}
			}
		}

		class BringingWood : State {

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
				bool isDestructing = reader.GetNext<bool>();
				if (isDestructing) {
					double remaining = reader.GetNext<double>();
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
				if (destruction == null && Dog.walker.State != WorldWalkerState.Started) {
					GoToCutter();
					return;
				}

				if (destruction != null && destruction.Update(timeStep)) {
					if (!GoToCutter()) {
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
				foreach (var neighbour in Dog.Cutter.Building.Tiles[0].GetNeighbours()) {
					if (Dog.walker.GoTo(Dog.Level.Map.PathFinding.GetTileNode(neighbour))) {
						destruction = null;
						return true;
					}
				}


				//Could not find path back to cutter
				if (destruction == null) {
					destruction = new Timeout(DestructionTime);
				}	
				return false;
			}
		}

		class SearchingForTree : State {

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
				double remaining = reader.GetNext<double>();
				timeout = new Timeout(searchTimeout, remaining);
			}

			public override void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext((int)Current);
				writer.StoreNext(timeout.Remaining);
			}

			public override void OnUpdate(float timeStep)
			{
				if (timeout.Update(timeStep)) {
					timeout.Reset();


					var trees = Dog.Level
									.NeutralPlayer
									.GetBuildingsOfType(Dog.Level.Package.GetBuildingType(TreeType.TypeID));
					if (trees.Count == 0) {
						return;
					}

					var closest = trees.Where(tree => !Dog.inaccesibleTrees.Contains(tree)).MinBy((tree) => Vector3.Distance(Dog.Entity.Position, tree.Position)).First();
					Dog.targetTree = (Tree)closest.Plugin;

					Dog.currentState = new GoingToTree(this, Dog);
				}
			}
		}

		public DogInstance(ILevelManager level, IUnit unit, bool loading)
			: base(level, unit)
		{
			if (loading) {
				return;
			}

			animationController = unit.CreateComponent<AnimationController>();
			walker = WorldWalker.CreateNew(this, level);
			MovingRangeTarget.CreateNew(this, level, targetOffset);

			unit.AlwaysVertical = false;
			hp = 100;
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 15, 0), new Vector2(0.5f, 0.1f), hp);
			currentState = new SearchingForTree((State)null, this);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			writer.StoreNext(hp);
			currentState.Save(writer);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			animationController = Unit.CreateComponent<AnimationController>();
			walker = Unit.GetDefaultComponent<WorldWalker>();

			RegisterEvents(walker);

			var reader = pluginData.GetReaderForWrappedSequentialData();
			hp = reader.GetNext<float>();
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 15, 0), new Vector2(0.5f, 0.1f), hp);
			currentState = State.Load(reader, this);
		}

		public override void Dispose()
		{
			healthbar.Dispose();
		}

		public override void OnHit(IEntity other, object userData)
		{
			if (other.Player == Unit.Player)
			{
				return;
			}

			hp -= 20;
			if (hp < 0)
			{
				healthbar.SetHealth(0);
				walker.Enabled = false;
				Unit.RemoveFromLevel();
			}
			else
			{
				healthbar.SetHealth((int)hp);
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
			animationController.PlayExclusive("Units/Dog/Walk.ani", 0, true);
			animationController.SetSpeed("Units/Dog/Walk.ani", 2);

			currentState.MovementStarted();
		}

		void OnMovementFinished(WorldWalker walker)
		{
			animationController.Stop("Units/Dog/Walk.ani");

			currentState.MovementFinished();
		}

		void OnMovementFailed(WorldWalker walker)
		{
			animationController.Stop("Units/Dog/Walk.ani");

			currentState.MovementFailed();
		}

		void OnMovementCanceled(WorldWalker walker)
		{
			animationController.Stop("Units/Dog/Walk.ani");

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
