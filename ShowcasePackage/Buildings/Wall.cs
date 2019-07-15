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
	public class WallType : BaseBuildingTypePlugin
	{
		public static string TypeName = "Wall";
		public static int TypeID = 4;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public BuildingType MyTypeInstance { get; private set; }

		public Cost Cost { get; private set; }
		public ViableTileTypes ViableTileTypes { get; private set; }


		const string CostElement = "cost";
		const string CanBuildOnElement = "canBuildOn";



		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			MyTypeInstance = package.GetBuildingType(ID);

			XElement costElem = extensionElement.Element(package.PackageManager.GetQualifiedXName(CostElement));
			Cost = Cost.FromXml(costElem, package);

			XElement canBuildOnElem =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(CanBuildOnElement));
			ViableTileTypes = ViableTileTypes.FromXml(canBuildOnElem, package);
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return Wall.CreateNew(level, building);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return Wall.CreateForLoading(level, building);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IPlayer owner, ILevelManager level)
		{
			return level.Map
						.GetTilesInRectangle(MyTypeInstance.GetBuildingTilesRectangle(topLeftTileIndex))
						.All((tile) => tile.Building == null && tile.Units.Count == 0 && ViableTileTypes.IsViable(tile));
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new WallBuilder(input, ui, camera, input.Level.Package.GetBuildingType(ID), this);
		}
	}

	public class Wall : WalkableBuildingPlugin {

		public static readonly object WallTag = "Wall";

		const float Height = 4;

		HealthBarControl healthBar;

		readonly IBuildingNode pathNode;
		Wall(ILevelManager level, IBuilding building)
			: base(level, building)
		{
			Vector3 topPosition = building.Center;
			topPosition = topPosition.WithY(GetHeightAt(topPosition.X, topPosition.Z).Value);
			pathNode = level.Map.PathFinding.CreateBuildingNode(building, topPosition, WallTag);
		}

		public static Wall CreateNew(ILevelManager level, IBuilding building)
		{
			Wall wall = null;
			try {
				wall = new Wall(level, building);
				wall.ConnectNeighbours();
				wall.healthBar =
					new HealthBarControl(level, building, 100, new Vector3(0, 5, 0), new Vector2(0.5f, 0.2f), false);
				StaticRangeTarget.CreateNew(wall, level, building.Center);
				return wall;
			}
			catch (Exception e) {
				wall?.Dispose();
				throw;
			}
		}

		public static Wall CreateForLoading(ILevelManager level, IBuilding building)
		{
			return new Wall(level, building);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			healthBar.Save(writer);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var reader = pluginData.GetReaderForWrappedSequentialData();
			healthBar = HealthBarControl.Load(Level, Building, reader);

			ConnectNeighbours();
		}

		public override void Dispose()
		{
			pathNode?.Remove();
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
			return true;
		}

		public override void TileHeightChanged(ITile tile)
		{
			Building.ChangeHeight(Level.Map.GetTerrainHeightAt(Building.Position.XZ2()));
			pathNode.ChangePosition(pathNode.Position.WithY(GetHeightAt(Building.Center.X, Building.Center.Z).Value));
		}

		public override float? GetHeightAt(float x, float y)
		{
			return Level.Map.GetTerrainHeightAt(x, y) + Height;
		}

		public override IBuildingNode TryGetNodeAt(ITile tile)
		{
			return tile.Building == Building ? pathNode : null;
		}

		public override IFormationController GetFormationController(Vector3 centerPosition)
		{
			IBuildingNode startNode = pathNode;
			return new BFSRoofFormationController((LevelInstancePluginBase)Level.Plugin, startNode);
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


					//Either is not loaded yet, will connect from the other side
					// or does not contain a node (impossible in the current version)
					if (node == null) {
						continue;
					}


					MovementType movementType;
					if (node.Tag == WallTag) {
						movementType = MovementType.Linear;
					}
					else if (node.Tag == Gate.GateRoofTag) { 
						movementType = MovementType.Teleport;
					}
					else {
						continue;
					}

					if (!pathNode.HasEdgeTo(node)) {
						pathNode.CreateEdge(node, movementType);
					}

					if (!node.HasEdgeTo(pathNode)) {
						node.CreateEdge(pathNode, movementType);
					}		
				}
			}
		}
	}

	class WallBuilder : LineBuilder {
		readonly BaseCustomWindowUI cwUI;

		public WallBuilder(GameController input, GameUI ui, CameraMover camera, BuildingType type, WallType myType)
			: base(input, ui, camera, type, input.Level.EditorMode ? Cost.Free : myType.Cost)
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
