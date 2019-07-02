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

namespace ShowcasePackage.Buildings
{
	public class KeepType : BaseBuildingTypePlugin
	{
		public static string TypeName = "Keep";
		public static int TypeID = 7;

		public override string Name => TypeName;
		public override int ID => TypeID;

		BuildingType myType;

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			myType = package.GetBuildingType(ID);
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return new Keep(level, building);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return new Keep(level, building);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, IPlayer owner, ILevelManager level)
		{
			return owner.GetBuildingsOfType(myType).Count == 0 &&
					level.Map
						.GetTilesInRectangle(topLeftTileIndex, bottomRightTileIndex)
						.All((tile) => tile.Building == null && tile.Units.Count == 0);
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new DirectionalBuilder(input, ui, camera, input.Level.Package.GetBuildingType(ID));
		}
	}

	public class Keep : WalkableBuildingPlugin
	{

		public const string KeepTag = "Keep";
		const int BuildingHeight = 7;

		readonly Dictionary<ITile, IBuildingNode> nodes;

		public Keep(ILevelManager level, IBuilding building)
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
			foreach (var node in nodes)
			{
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

		void CreatePathfindingNodes()
		{

			for (int y = Building.Rectangle.Top; y < Building.Rectangle.Bottom; y++)
			{
				for (int x = Building.Rectangle.Left; x < Building.Rectangle.Right; x++)
				{
					ITile tile = Level.Map.GetTileByTopLeftCorner(x, y);
					Vector3 position = new Vector3(tile.Center.X, GetHeightAt(tile.Center.X, tile.Center.Y).Value, tile.Center.Y);
					IBuildingNode node = Level.Map.PathFinding.CreateBuildingNode(Building, position, KeepTag);
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
}


