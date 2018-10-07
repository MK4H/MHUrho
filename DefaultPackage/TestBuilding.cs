using System;
using System.Collections.Generic;
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
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;

namespace DefaultPackage
{
	public class TestBuildingType : BuildingTypePlugin
	{
		public override int ID => 1;

		public override string Name => "TestBuilding";

		UnitType workerType;
		TileType tileType;

		public TestBuildingType() {

		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building) {
			IUnit[] workers = new IUnit[2];
			workers[0] = level.SpawnUnit(workerType,
										 level.Map.GetTileByTopLeftCorner(building.Rectangle.TopLeft() + new IntVector2(0, -1)),
										 building.Player);
			workers[1] = level.SpawnUnit(workerType,
										 level.Map.GetTileByTopLeftCorner(building.Rectangle.TopLeft() + new IntVector2(-1, 0)),
										 building.Player);

			return new TestBuildingInstance(level, building, workers);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building) {
			return new TestBuildingInstance(level, building);
		}

		public override bool CanBuildIn(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, ILevelManager level) {
			bool empty = true;
			int rightTileTypeCount = 0;
			level.Map.ForEachInRectangle(topLeftTileIndex, 
										 bottomRightTileIndex, 
										 (tile) => {

														if (tile.Units.Count != 0 ||
															tile.Building != null) {

															empty = false;

														}

														if (tile.Type == tileType) {
															rightTileTypeCount++;
														}
													});
			return empty && rightTileTypeCount > 5;
		}


		public override void PopulateUI(MandKGameUI mouseAndKeyboardUI) {
			throw new NotImplementedException();
		}

		public override void ClearUI(MandKGameUI mouseAndKeyboardUI) {
			throw new NotImplementedException();
		}

		public override void PopulateUI(TouchUI touchUI) {
			throw new NotImplementedException();
		}

		public override void ClearUI(TouchUI touchUI) {
			throw new NotImplementedException();
		}

		public override void AddSelected(BuildingInstancePlugin buildingInstance) {
			throw new NotImplementedException();
		}

		public override void RemoveSelected(BuildingInstancePlugin buildingInstance) {
			throw new NotImplementedException();
		}

		public override void Initialize(XElement extensionElement, GamePack package) {
			workerType = package.GetUnitType(XmlHelpers.GetString(XmlHelpers.GetChild(extensionElement,"workerType")));
			tileType = package.GetTileType(XmlHelpers.GetString(XmlHelpers.GetChild(extensionElement, "tileType")));
		}
	}

	public class TestBuildingInstance : BuildingInstancePlugin {

		TestWorkerInstance[] workers;
		Dictionary<ITile, IBuildingNode> pathfindingNodes;
		ITile entryTile;

		int resources;

		const float timeBetweenResourceSpawns = 5;

		float timeToNextResource = timeBetweenResourceSpawns;


		public TestBuildingInstance(ILevelManager level, IBuilding building, IUnit[] workers)
			:base(level, building)
		{

			this.workers = new TestWorkerInstance[workers.Length];

			for (int i = 0; i < workers.Length; i++) {
				this.workers[i] = (TestWorkerInstance)workers[i].UnitPlugin;
				this.workers[i].WorkedBuilding = this;
			}

			AddPathfindingNodes();
		}

		public TestBuildingInstance(ILevelManager level, IBuilding building)
			:base(level, building)
		{

		}

		public override void OnUpdate(float timeStep) {
			timeToNextResource -= timeStep;
			if (timeToNextResource < 0) {
				resources++;
			}

			if (resources > 0) {
				
			}
		}

		public override void SaveState(PluginDataWrapper pluginData) {
			var sequentialData = pluginData.GetWriterForWrappedSequentialData();
			sequentialData.StoreNext(workers.Length);
			foreach (var worker in workers) {
				sequentialData.StoreNext(worker.Unit.ID);
			}

			sequentialData.StoreNext(resources);
			sequentialData.StoreNext(timeToNextResource);
		}

		public override void LoadState(PluginDataWrapper pluginData) {

			var reader = pluginData.GetReaderForWrappedSequentialData();
			reader.MoveNext();
			workers = new TestWorkerInstance[reader.GetCurrent<int>()];
			reader.MoveNext();
			for (int i = 0; i < workers.Length; i++) {
				workers[i] = (TestWorkerInstance)Level.GetUnit(reader.GetCurrent<int>()).UnitPlugin;
				reader.MoveNext();
			}

			resources = reader.GetCurrent<int>();
			reader.MoveNext();
			timeToNextResource = reader.GetCurrent<float>();
			reader.MoveNext();


			AddPathfindingNodes();
		}

		public override void OnHit(IEntity other, object userData)
		{
			
		}


		public override IFormationController GetFormationController(Vector3 centerPosition)
		{
			return new TestBuildingFormationController(pathfindingNodes, Map.GetContainingTile(centerPosition), entryTile, Map);
		}

		public override float? GetHeightAt(float x, float y)
		{
			return 3;
		}

		public ITile GetInterfaceTile(TestWorkerInstance testWorker) {
			for (int i = 0; i < workers.Length; i++) {
				if (testWorker == workers[i]) {
					return Map.GetTileByBottomRightCorner(Building.Rectangle.TopLeft() +
														new IntVector2(0, i % 4));
				}
			}
			return null;
		}

		public override void Dispose()
		{

		}

		void AddPathfindingNodes()
		{
			pathfindingNodes = new Dictionary<ITile, IBuildingNode>();

			for (int y = Building.Rectangle.Top; y < Building.Rectangle.Bottom; y++) {
				for (int x = Building.Rectangle.Left; x < Building.Rectangle.Right; x++) {
					ITile tile = Map.GetTileByTopLeftCorner(x, y);
					Vector3 position = new Vector3(tile.Center.X, Map.GetTerrainHeightAt(tile.Center) + GetHeightAt(tile.Center.X, tile.Center.Y).Value, tile.Center.Y);
					pathfindingNodes.Add(tile,
										Map.PathFinding.CreateBuildingNode(Building, position, null));
				}
			}

			foreach (var node in pathfindingNodes) {
				foreach (var neighbour in node.Key.GetNeighbours()) {
					if (neighbour != null && pathfindingNodes.TryGetValue(neighbour, out IBuildingNode neighbourNode)) {
						node.Value.CreateEdge(neighbourNode, MovementType.Linear);
					} 
				}
			}

			entryTile = Map.GetContainingTile(Building.Center + Building.Forward * 2);
			var tileNode = Map.PathFinding.GetTileNode(entryTile);
			var buildingEntryNode = pathfindingNodes[Map.GetContainingTile(Building.Center + Building.Forward)];
			buildingEntryNode.CreateEdge(tileNode, MovementType.Teleport);
			tileNode.CreateEdge(buildingEntryNode, MovementType.Teleport);
		}

	}

	class TestBuildingFormationController : IFormationController {

		Dictionary<ITile, IBuildingNode> nodes;
		Spiral.SpiralEnumerator spiral;
		IMap map;
		ITile entry;

		public TestBuildingFormationController(Dictionary<ITile, IBuildingNode> nodes, ITile center, ITile entry, IMap map)
		{
			this.nodes = nodes;
			this.map = map;
			this.entry = entry;
			spiral = new Spiral(center.MapLocation).GetSpiralEnumerator();
		}

		public bool MoveToFormation(UnitSelector unit)
		{
			bool executed = false;
			while (!executed &&
					spiral.MoveNext() && 
					spiral.ContainingSquareSize < 6 &&
					nodes.TryGetValue(map.GetTileByMapLocation(spiral.Current), out IBuildingNode buildingNode)) {
				executed = unit.Order(new MoveOrder(buildingNode));
			}

			return executed;
		}

		public bool MoveToFormation(UnitGroup units)
		{
			bool executed = false;

			while (units.IsValid()) {
				if (MoveToFormation(units.Current)) {
					executed = true;
					units.TryMoveNext();
				}
				else if (spiral.ContainingSquareSize >= 6 ) {
					map.GetFormationController(entry).MoveToFormation(units);
				}
			}
			return executed;
		}

	}

}
