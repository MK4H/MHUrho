using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
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
	public class TestWorkerType : UnitTypePlugin {

		public override int ID => 9;

		public override string Name => "TestWorker";

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit)
		{
			return TestWorkerInstance.CreateNew(level, unit);
		}

		public override UnitInstancePlugin GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return TestWorkerInstance.GetInstanceForLoading(level, unit);
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return centerTile.Type != PackageManager.Instance.ActivePackage.DefaultTileType &&
				   centerTile.Building == null;
		}

		public override void Initialize(XElement extensionElement, GamePack package) {
			
		}
	}

	public class TestWorkerInstance : UnitInstancePlugin, WorldWalker.IUser {



		class DistanceCalc : NodeDistCalculator {

			IMap map;

			public DistanceCalc(IMap map)
			{
				this.map = map;
			}

			public override float GetMinimalAproxTime(Vector3 source, Vector3 target)
			{
				return (target - source).Length;
			}

			protected override bool GetTime(ITileNode source, ITileNode target, out float time)
			{
				Vector3 edgePosition = source.GetEdgePosition(target);
				time = (edgePosition - source.Position).Length + (target.Position - edgePosition).Length;

				//If the edge is diagonal and there are buildings on both sides of the edge, dont go there
				if (source.Tile.MapLocation.X != target.Tile.MapLocation.X &&
					source.Tile.MapLocation.Y != target.Tile.MapLocation.Y &&
					map.GetTileByMapLocation(new IntVector2(source.Tile.MapLocation.X,
															target.Tile.MapLocation.Y))
						.Building != null &&
					map.GetTileByMapLocation(new IntVector2(target.Tile.MapLocation.X,
															source.Tile.MapLocation.Y)) != null
				)
				{
					time = -1;
					return false;
				}

				return true;
			}

			protected override bool GetTime(ITempNode source, ITileNode target, out float time)
			{
				time = (source.Position - target.Position).Length;
				return true;
			}

			protected override bool GetTime(ITileNode source, ITempNode target, out float time)
			{
				time = (source.Position - target.Position).Length;
				return true;
			}
		}

		public TestBuildingInstance WorkedBuilding { get; set; }


		WorldWalker walker;
		bool homeGoing = false;
		bool started = false;

		readonly DistanceCalc distCalc;

		public static TestWorkerInstance CreateNew(ILevelManager level, IUnit unit)
		{
			var instance = new TestWorkerInstance(level, unit);
			instance.walker = WorldWalker.CreateNew(instance, level);

			instance.walker.MovementFinished += instance.OnMovementFinished;
			return instance;
		}

		public static TestWorkerInstance GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return new TestWorkerInstance(level, unit);
		}

		protected TestWorkerInstance(ILevelManager level, IUnit unit) 
			: base(level, unit)
		{

			this.distCalc = new DistanceCalc(Level.Map);
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

		INodeDistCalculator WorldWalker.IUser.GetNodeDistCalculator()
		{
			return distCalc;
		}

		public void OnMovementFinished(WorldWalker walker) {
			if (homeGoing) {
				homeGoing = !homeGoing;
				walker.GoTo(Level.Map.PathFinding.GetTileNode(Level.Map.GetTileByMapLocation(new IntVector2(20, 20))));
				
			}
			else if (!homeGoing) {
				homeGoing = !homeGoing;
				walker.GoTo(Level.Map.PathFinding.GetTileNode(WorkedBuilding.GetInterfaceTile(this)));
				
			}
		}


		public override void Dispose()
		{

		}

	}
}
