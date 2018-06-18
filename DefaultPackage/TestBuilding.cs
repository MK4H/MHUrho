using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
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
		UnitType workerType;
		TileType tileType;

		public TestBuildingType() {

		}

		public override bool IsMyType(string typeName) {
			return typeName == "TestBuilding";
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building) {
			IUnit[] workers = new Unit[2];
			workers[0] = level.SpawnUnit(workerType,
										 level.Map.GetTileByTopLeftCorner(building.Rectangle.TopLeft() + new IntVector2(0, -1)),
										 building.Player);
			workers[1] = level.SpawnUnit(workerType,
										 level.Map.GetTileByTopLeftCorner(building.Rectangle.TopLeft() + new IntVector2(-1, 0)),
										 building.Player);

			return new TestBuildingInstance(level, building, workers);
		}

		public override BuildingInstancePlugin GetInstanceForLoading() {
			return new TestBuildingInstance();
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

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			workerType = PackageManager.Instance
									   .ActiveGame
									   .GetUnitType(XmlHelpers.GetString(XmlHelpers.GetChild(extensionElement,"workerType")),
													true);
			tileType = PackageManager.Instance.ActiveGame.GetTileType(XmlHelpers.GetString(XmlHelpers.GetChild(extensionElement, "tileType")), true);
		}
	}

	public class TestBuildingInstance : BuildingInstancePlugin {

		ILevelManager level;
		TestWorkerInstance[] workers;
		Dictionary<ITile, IBuildingNode> pathfindingNodes;

		int resources;

		const float timeBetweenResourceSpawns = 5;

		float timeToNextResource = timeBetweenResourceSpawns;

		public TestBuildingInstance() {

		}

		public TestBuildingInstance(ILevelManager level, IBuilding building, IUnit[] workers) {
			this.level = level;

			this.Building = building;
			this.workers = new TestWorkerInstance[workers.Length];

			for (int i = 0; i < workers.Length; i++) {
				this.workers[i] = (TestWorkerInstance)workers[i].UnitPlugin;
				this.workers[i].WorkedBuilding = this;
			}
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

		public override void LoadState(ILevelManager level, IBuilding building, PluginDataWrapper pluginData) {
			this.level = level;
			this.Building = building;

			var reader = pluginData.GetReaderForWrappedSequentialData();
			reader.MoveNext();
			workers = new TestWorkerInstance[reader.GetCurrent<int>()];
			reader.MoveNext();
			for (int i = 0; i < workers.Length; i++) {
				workers[i] = (TestWorkerInstance)level.GetUnit(reader.GetCurrent<int>()).UnitPlugin;
				reader.MoveNext();
			}

			resources = reader.GetCurrent<int>();
			reader.MoveNext();
			timeToNextResource = reader.GetCurrent<float>();
			reader.MoveNext();

		}

		public override void OnProjectileHit(IProjectile projectile)
		{
			throw new NotImplementedException();
		}

		public override void OnMeeleHit(IEntity byEntity)
		{
			throw new NotImplementedException();
		}

		public override IFormationController GetFormationController(Vector3 centerPosition)
		{
			return new TestBuildingFormationController(pathfindingNodes, Map.GetContainingTile(centerPosition), Map);
		}

		public ITile GetInterfaceTile(TestWorkerInstance testWorker) {
			for (int i = 0; i < workers.Length; i++) {
				if (testWorker == workers[i]) {
					return level.Map.GetTileByBottomRightCorner(Building.Rectangle.TopLeft() +
																new IntVector2(0, i % 4));
				}
			}
			return null;
		}


		void AddPathfindingNodes()
		{
			pathfindingNodes = new Dictionary<ITile, IBuildingNode>();
			IntVector2 tileLocation = Building.Rectangle.TopLeft();

			for (int y = Building.Rectangle.Top; y <= Building.Rectangle.Bottom; y++) {
				for (int x = Building.Rectangle.Left; x <= Building.Rectangle.Right; x++) {
					ITile tile = Map.GetTileByTopLeftCorner(x, y);
					pathfindingNodes.Add(tile,
										Map.PathFinding.CreateBuildingNode(Building, tile.Center3 + new Vector3(0, 3, 0), null));
				}
			}

			foreach (var node in pathfindingNodes) {
				foreach (var neighbour in node.Key.GetNeighbours()) {
					if (neighbour != null && pathfindingNodes.TryGetValue(neighbour, out IBuildingNode neighbourNode)) {
						node.Value.CreateEdge(neighbourNode, MovementType.Linear);
					} 
				}
			}

			ITile sourceTile = Map.GetContainingTile(Building.Center + Building.Forward * 2);
			var tileNode = Map.PathFinding.GetTileNode(sourceTile);
			var buildingEntryNode = pathfindingNodes[Map.GetContainingTile(Building.Center + Building.Forward)];
			buildingEntryNode.CreateEdge(tileNode, MovementType.Teleport);
			tileNode.CreateEdge(buildingEntryNode, MovementType.Teleport);
		}

	}

	class TestBuildingFormationController : IFormationController {

		Dictionary<ITile, IBuildingNode> nodes;
		Spiral.SpiralEnumerator spiral;
		IMap map;

		public TestBuildingFormationController(Dictionary<ITile, IBuildingNode> nodes, ITile center, IMap map)
		{
			nodes = new Dictionary<ITile, IBuildingNode>();
			spiral = new Spiral(center.MapLocation).GetSpiralEnumerator();
		}

		public bool MoveToFormation(UnitSelector unit)
		{
			while (spiral.MoveNext() && 
					spiral.ContainingSquareSize < 6 &&
					nodes.TryGetValue(map.GetTileByMapLocation(spiral.Current), out IBuildingNode buildingNode)) {
				unit.Order(buildingNode,)
			}
		}

		public void MoveToFormation(IEnumerator<UnitSelector> units)
		{
			
		}

	}

}
