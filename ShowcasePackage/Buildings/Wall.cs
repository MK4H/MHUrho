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
using ShowcasePackage.Levels;

namespace ShowcasePackage.Buildings
{
	public class WallType : BaseBuildingTypePlugin
	{
		public static string TypeName = "Wall";
		public static int TypeID = 4;

		public override string Name => TypeName;
		public override int ID => TypeID;

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

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
						.All((tile) => tile.Building == null && tile.Units.Count == 0);
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new LineBuilder(input, ui, camera, input.Level.Package.GetBuildingType(ID));
		}
	}

	public class Wall : WalkableBuildingPlugin {

		public static readonly object WallTag = "Wall";

		IBuildingNode pathNode;

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

		}

		public override void LoadState(PluginDataWrapper pluginData)
		{

		}

		public override void Dispose()
		{
			pathNode.Remove();
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
}
