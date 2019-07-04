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
using ShowcasePackage.Misc;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	public class TowerType : BaseBuildingTypePlugin
	{
		public static string TypeName = "Tower";
		public static int TypeID = 5;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public Cost Cost { get; private set; }
		public ViableTileTypes ViableTileTypes { get; private set; }

		const string CostElement = "cost";
		const string CanBuildOnElement = "canBuildOn";

		BuildingType myType;

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			XElement costElem = extensionElement.Element(package.PackageManager.GetQualifiedXName(CostElement));
			Cost = Cost.FromXml(costElem, package);

			XElement canBuildOnElem =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(CanBuildOnElement));
			ViableTileTypes = ViableTileTypes.FromXml(canBuildOnElem, package);

			myType = package.GetBuildingType(TypeID);
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return new Tower(level, building);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return new Tower(level, building);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, IPlayer owner, ILevelManager level)
		{
			return level.Map
						.GetTilesInRectangle(topLeftTileIndex, bottomRightTileIndex)
						.All((tile) => tile.Building == null && tile.Units.Count == 0 && ViableTileTypes.IsViable(tile));
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new TowerBuilder(input, ui, camera, input.Level.Package.GetBuildingType(ID), this);
		}
	}

	public class Tower : WalkableBuildingPlugin
	{

		public static readonly object TowerTag = "Tower";
		const int BuildingHeight = 8;

		readonly Dictionary<ITile, IBuildingNode> nodes;

		public Tower(ILevelManager level, IBuilding building)
			: base(level, building)
		{
			StaticRangeTarget.CreateNew(this, level, building.Center);

			nodes = new Dictionary<ITile, IBuildingNode>();
			CreatePathfindingNodes();
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{

		}

		public override void LoadState(PluginDataWrapper pluginData)
		{

		}

		public override void Dispose()
		{
			foreach (var node in nodes) {
				node.Value.Remove();
			}
		}

		public override float? GetHeightAt(float x, float y)
		{
			return Level.Map.GetTerrainHeightAt(x, y) + BuildingHeight;
		}

		public override IBuildingNode TryGetNodeAt(ITile tile)
		{
			return nodes.TryGetValue(tile, out IBuildingNode node) ? node : null;
		}

		public override IFormationController GetFormationController(Vector3 centerPosition)
		{
			ITile tile = Level.Map.GetContainingTile(centerPosition);
			if (!nodes.TryGetValue(tile, out IBuildingNode startNode))
			{
				return null;
			}
			return new BFSRoofFormationController((LevelInstancePluginBase)Level.Plugin, startNode);
		}

		void CreatePathfindingNodes()
		{

			for (int y = Building.Rectangle.Top; y < Building.Rectangle.Bottom; y++)
			{
				for (int x = Building.Rectangle.Left; x < Building.Rectangle.Right; x++)
				{
					ITile tile = Level.Map.GetTileByTopLeftCorner(x, y);
					Vector3 position = new Vector3(tile.Center.X, GetHeightAt(tile.Center.X, tile.Center.Y).Value, tile.Center.Y);
					IBuildingNode node = Level.Map.PathFinding.CreateBuildingNode(Building, position, TowerTag);
					nodes.Add(tile, node);
				}
			}

			//Connect roof edges
			foreach (var tileAndNode in nodes)
			{
				ITile tile = tileAndNode.Key;
				IBuildingNode node = tileAndNode.Value;
				foreach (var neighbour in tile.GetNeighbours())
				{
					if (neighbour == null)
					{
						continue;
					}
					//Connect to neighbor roof nodes
					if (nodes.TryGetValue(neighbour, out IBuildingNode neighbourNode))
					{
						node.CreateEdge(neighbourNode, MovementType.Linear);
					}
					else if (neighbour.Building != null &&
							neighbour.Building.BuildingPlugin is WalkableBuildingPlugin plugin)
					{
						INode foreighNode = plugin.TryGetNodeAt(neighbour);
						foreighNode.CreateEdge(node, MovementType.Teleport);
						node.CreateEdge(foreighNode, MovementType.Teleport);
					}
				}
			}
		}
	}

	class TowerBuilder : DirectionlessBuilder {

		readonly BaseCustomWindowUI cwUI;

		public TowerBuilder(GameController input, GameUI ui, CameraMover camera, BuildingType type, TowerType myType)
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


