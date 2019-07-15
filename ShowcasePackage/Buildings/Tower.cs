using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.DefaultComponents;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UserInterface.MouseKeyboard;
using MHUrho.Helpers.Extensions;
using Urho;
using MHUrho.Control;
using MHUrho.EntityInfo;
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

		public BuildingType MyTypeInstance { get; private set; }

		public Cost Cost { get; private set; }
		public ViableTileTypes ViableTileTypes { get; private set; }

		const string CostElement = "cost";
		const string CanBuildOnElement = "canBuildOn";

		const float MaxHeightDiff = 0.5f;


		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			XElement costElem = extensionElement.Element(package.PackageManager.GetQualifiedXName(CostElement));
			Cost = Cost.FromXml(costElem, package);

			XElement canBuildOnElem =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(CanBuildOnElement));
			ViableTileTypes = ViableTileTypes.FromXml(canBuildOnElem, package);

			MyTypeInstance = package.GetBuildingType(TypeID);
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return Tower.CreateNew(level, building);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return Tower.CreateForLoading(level, building);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IPlayer owner, ILevelManager level)
		{
			return level.Map
						.GetTilesInRectangle(MyTypeInstance.GetBuildingTilesRectangle(topLeftTileIndex))
						.All((tile) => tile.Building == null && tile.Units.Count == 0 && ViableTileTypes.IsViable(tile)) &&
					HeightDiffLow(topLeftTileIndex, MyTypeInstance.GetBottomRightTileIndex(topLeftTileIndex), level, MaxHeightDiff);
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new TowerBuilder(input, ui, camera, this);
		}
	}

	public class Tower : WalkableBuildingPlugin
	{

		public static readonly object TowerTag = "Tower";
		const int BuildingHeight = 8;

		readonly Dictionary<ITile, IBuildingNode> nodes;

		HealthBarControl healthBar;

		Tower(ILevelManager level, IBuilding building)
			: base(level, building)
		{
			nodes = new Dictionary<ITile, IBuildingNode>();
		}

		public static Tower CreateNew(ILevelManager level, IBuilding building)
		{
			Tower newTower = null;
			try {
				newTower = new Tower(level, building);
				newTower.CreatePathfindingNodes();
				StaticRangeTarget.CreateNew(newTower, level, building.Center);
				newTower.healthBar =
					new HealthBarControl(level, building, 100, new Vector3(0, 5, 0), new Vector2(1f, 0.2f), false);

				return newTower;
			}
			catch (Exception e) {
				newTower?.Dispose();
				throw;
			}
		}

		public static Tower CreateForLoading(ILevelManager level, IBuilding building)
		{
			return new Tower(level, building);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			healthBar.Save(writer);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var reader = pluginData.GetReaderForWrappedSequentialData();
			healthBar = HealthBarControl.Load(Level, Building,reader);

			CreatePathfindingNodes();
		}

		public override void Dispose()
		{
			foreach (var node in nodes) {
				node.Value.Remove();
			}

			healthBar?.Dispose();
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
			if (Building.Player.IsFriend(byEntity.Player) || !(byEntity is IProjectile))
			{
				return;
			}

			int damage = (int)userData;

			if (!healthBar.ChangeHitPoints(-damage))
			{
				Building.RemoveFromLevel();
			}
		}

		public override bool CanChangeTileHeight(int x, int y)
		{
			return false;
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
						IBuildingNode foreighNode = plugin.TryGetNodeAt(neighbour);

						//Either is not loaded yet, will connect from the other side
						// or does not contain a node (impossible in the current version)
						if (foreighNode == null) {
							continue;
						}

						//Do not connect to keep
						if (foreighNode.Tag == Keep.KeepTag) {
							continue;
						}

						if (!foreighNode.HasEdgeTo(node)) {
							foreighNode.CreateEdge(node, MovementType.Teleport);
						}

						if (!node.HasEdgeTo(foreighNode)) {
							node.CreateEdge(foreighNode, MovementType.Teleport);
						}
					}
				}
			}
		}
	}

	class TowerBuilder : DirectionlessBuilder {

		readonly BaseCustomWindowUI cwUI;

		public TowerBuilder(GameController input, GameUI ui, CameraMover camera, TowerType type)
			: base(input, ui, camera, type.MyTypeInstance, input.Level.EditorMode ? Cost.Free : type.Cost)
		{
			cwUI = new BaseCustomWindowUI(ui, type.Name, $"Cost: {type.Cost}");
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


