using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace DefaultPackage
{
	public class TestWorkerType : UnitTypePlugin {

		public override bool IsMyType(string typeName) {
			return typeName == "TestWorker";
		}

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit)
		{
			return TestWorkerInstance.CreateNew(level, unit);
		}

		public override UnitInstancePlugin GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return TestWorkerInstance.GetInstanceForLoading(level, unit);
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return centerTile.Type != PackageManager.Instance.ActiveGame.DefaultTileType &&
				   centerTile.Building == null;
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			
		}
	}

	public class TestWorkerInstance : UnitInstancePlugin, WorldWalker.IUser {

		class PathVisitor : NodeVisitor {
			TestWorkerInstance worker;

			public PathVisitor(TestWorkerInstance worker)
			{
				this.worker = worker;
			}

			public override bool Visit(ITempNode source, ITileEdgeNode target, out float time)
			{
				time = (source.Position - target.Position).Length;
				return true;
			}

			public override bool Visit(ITempNode source, ITileNode target, out float time)
			{
				time = (source.Position - target.Position).Length;
				return true;
			}

			public override bool Visit(ITileEdgeNode source, ITileNode target, out float time)
			{
				if (target.Tile.Building == null) {
					time = (source.Position - target.Position).Length;
					return true;
				}

				time = -1;
				return false;
			}

			public override bool Visit(ITileNode source, ITileEdgeNode target, out float time)
			{
				time = (source.Position - target.Position).Length;
				ITileNode targetTile = target.GetOtherSide(source);

				//If the edge is diagonal and there are buildings on both sides of the edge, dont go there
				if (source.Tile.MapLocation.X != targetTile.Tile.MapLocation.X &&
					source.Tile.MapLocation.Y != targetTile.Tile.MapLocation.Y &&
					worker.Map
						.GetTileByMapLocation(new IntVector2(source.Tile.MapLocation.X,
															targetTile.Tile.MapLocation.Y))
						.Building != null &&
					worker.Map
						.GetTileByMapLocation(new IntVector2(targetTile.Tile.MapLocation.X,
															source.Tile.MapLocation.Y)) != null
				) {
					time = -1;
					return false;
				}

				return true;
			}

		}

		public TestBuildingInstance WorkedBuilding { get; set; }


		WorldWalker walker;
		bool homeGoing = false;
		bool started = false;

		readonly PathVisitor pathVisitor;

		public static TestWorkerInstance CreateNew(ILevelManager level, IUnit unit)
		{
			var instance = new TestWorkerInstance(level, unit);
			instance.walker = WorldWalker.CreateNew(instance, level);
			unit.AddComponent(instance.walker);

			instance.walker.OnMovementEnded += instance.OnMovementFinished;
			return instance;
		}

		public static TestWorkerInstance GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return new TestWorkerInstance(level, unit);
		}

		protected TestWorkerInstance(ILevelManager level, IUnit unit) 
			: base(level, unit) {
			
			this.pathVisitor = new PathVisitor(this);
		}

		public override void OnUpdate(float timeStep) {
			if (WorkedBuilding == null) {
				throw new InvalidOperationException("TestWorker has no building");
			}

			if (!started) {
				started = true;
				OnMovementFinished(walker);
			}

		}

		public override void SaveState(PluginDataWrapper pluginData) {
			var indexedData = pluginData.GetWriterForWrappedIndexedData();
			indexedData.Store(1, WorkedBuilding.Building.ID);
			indexedData.Store(2, homeGoing);
		}

		public override void LoadState( PluginDataWrapper pluginData) {
			this.started = true;

			var indexedData = pluginData.GetReaderForWrappedIndexedData();
			WorkedBuilding = (TestBuildingInstance)Level.GetBuilding(indexedData.Get<int>(1)).BuildingPlugin;
			homeGoing = indexedData.Get<bool>(2);
			walker = Unit.GetDefaultComponent<WorldWalker>();
		}



		public override void OnHit(IEntity other, object userData)
		{
			throw new NotImplementedException();
		}

		public bool GetTime(INode from, INode to, out float time)
		{
			return from.Accept(pathVisitor, to, out time);
		}

		public float GetMinimalAproximatedTime(Vector3 from, Vector3 to)
		{
			return (to - from).Length;
		}


		public void OnMovementFinished(WorldWalker walker) {
			if (homeGoing) {
				homeGoing = !homeGoing;
				walker.GoTo(Map.PathFinding.GetTileNode(Map.GetTileByMapLocation(new IntVector2(20, 20))));
				
			}
			else if (!homeGoing) {
				homeGoing = !homeGoing;
				walker.GoTo(Map.PathFinding.GetTileNode(WorkedBuilding.GetInterfaceTile(this)));
				
			}
		}


		public override void Dispose()
		{

		}

		public void GetMandatoryDelegates(out GetTime getTime, out GetMinimalAproxTime getMinimalAproximatedTime)
		{
			getTime = GetTime;
			getMinimalAproximatedTime = GetMinimalAproximatedTime;
		}
	}
}
