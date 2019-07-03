using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.DefaultComponents;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UserInterface.MandK;
using MHUrho.Helpers.Extensions;
using Urho;
using MHUrho.Control;
using MHUrho.EntityInfo;
using ShowcasePackage.Levels;
using ShowcasePackage.Misc;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	public class WallType : BaseBuildingTypePlugin
	{
		public static string TypeName = "Wall";
		public static int TypeID = 4;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public Cost Cost { get; private set; }
		public ViableTileTypes ViableTileTypes { get; private set; }

		const string CostElement = "cost";
		const string CanBuildOnElement = "canBuildOn";

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			XElement costElem = extensionElement.Element(package.PackageManager.GetQualifiedXName(CostElement));
			Cost = Cost.FromXml(costElem, package);

			XElement canBuildOnElem =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(CanBuildOnElement));
			ViableTileTypes = ViableTileTypes.FromXml(canBuildOnElem, package);
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return new Wall(level, building);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return new Wall(level, building);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, IPlayer owner, ILevelManager level)
		{
			return topLeftTileIndex == bottomRightTileIndex && 
					level.Map
						.GetTilesInRectangle(topLeftTileIndex, bottomRightTileIndex)
						.All((tile) => tile.Building == null && tile.Units.Count == 0 && ViableTileTypes.CanBuildOn(tile));
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new WallBuilder(input, ui, camera, input.Level.Package.GetBuildingType(ID), this);
		}
	}

	public class Wall : WalkableBuildingPlugin {

		public static readonly object WallTag = "Wall";

		const float height = 4;

		IBuildingNode pathNode;

		HealthBar healthBar;
		double hp;

		public Wall(ILevelManager level, IBuilding building)
			: base(level, building)
		{
			StaticRangeTarget.CreateNew(this, level, building.Center);

			Vector3 topPosition = building.Center;
			topPosition = topPosition.WithY(GetHeightAt(topPosition.X, topPosition.Z).Value);
			pathNode = Level.Map.PathFinding.CreateBuildingNode(building, topPosition, WallTag);
			ConnectNeighbours();
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			writer.StoreNext(hp);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var reader = pluginData.GetReaderForWrappedSequentialData();
			reader.GetNext(out hp);
		}

		public override void Dispose()
		{
			pathNode.Remove();
		}

		public override float? GetHeightAt(float x, float y)
		{
			return Level.Map.GetTerrainHeightAt(x, y) + height;
		}

		public override IBuildingNode TryGetNodeAt(ITile tile)
		{
			return tile.Building == Building ? pathNode : null;
		}

		public override IFormationController GetFormationController(Vector3 centerPosition)
		{
			IBuildingNode startNode = pathNode;
			return new BFSRoofFormationController((LevelPluginBase)Level.Plugin, startNode);
		}

		void ConnectNeighbours()
		{
			ITile myTile = Level.Map.GetContainingTile(Building.Center);
			foreach (var neighbor in myTile.GetNeighbours()) {
				if (neighbor?.Building == null) {
					continue;
				}

				if (neighbor.Building.BuildingPlugin is WalkableBuildingPlugin plugin) {
					IBuildingNode node = plugin.TryGetNodeAt(neighbor);
					MovementType movementType;
					if (node.Tag == WallTag) {
						movementType = MovementType.Linear;
					}
					else if (node.Tag == GateInstance.GateRoofTag) { 
						movementType = MovementType.Teleport;
					}
					else {
						continue;
					}
					pathNode.CreateEdge(node, movementType);
					node.CreateEdge(pathNode, movementType);
				}
			}
		}
	}

	class WallBuilder : LineBuilder {
		readonly BaseCustomWindowUI cwUI;

		public WallBuilder(GameController input, GameUI ui, CameraMover camera, BuildingType type, WallType myType)
			: base(input, ui, camera, type)
		{
			cwUI = new BaseCustomWindowUI(ui, myType.Name, $"Cost: {myType.Cost}");
		}

		public override void Enable()
		{
			base.Enable();

			cwUI.Show();
		}

		public override void Disable()
		{
			cwUI.Hide();

			base.Disable();
		}

		public override void Dispose()
		{
			cwUI.Dispose();

			base.Dispose();
		}
	}
}
