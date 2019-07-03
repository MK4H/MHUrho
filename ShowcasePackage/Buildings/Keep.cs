using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.DefaultComponents;
using MHUrho.Helpers;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UserInterface.MandK;
using MHUrho.Helpers.Extensions;
using ShowcasePackage.Misc;
using ShowcasePackage.Units;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	public class KeepType : BaseBuildingTypePlugin
	{
		public static string TypeName = "Keep";
		public static int TypeID = 7;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public static IReadOnlyList<string> SpawnedUnits = new List<string>{ChickenType.TypeName, WolfType.TypeName};

		public ViableTileTypes ViableTileTypes { get; private set; }
		const string CanBuildOnElement = "canBuildOn";

		public ResourceType ProducedResource { get; private set; }
		public double ProductionRate { get; private set; }
		const string ProducedResourceElement = "producedResource";
		const string ProductionRateAttribute = "rate";

		BuildingType myType;

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			myType = package.GetBuildingType(ID);

			XElement canBuildOnElem =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(CanBuildOnElement));
			ViableTileTypes = ViableTileTypes.FromXml(canBuildOnElem, package);

			XElement producedResourceElem =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(ProducedResourceElement));
			ProducedResource = package.GetResourceType(producedResourceElem.Value);
			ProductionRate = double.Parse(producedResourceElem.Attribute(ProductionRateAttribute).Value);
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return Keep.CreateNew(level, building, this);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return Keep.CreateForLoading(level, building, this);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, IPlayer owner, ILevelManager level)
		{
			return owner.GetBuildingsOfType(myType).Count == 0 &&
					level.Map
						.GetTilesInRectangle(topLeftTileIndex, bottomRightTileIndex)
						.All((tile) => tile.Building == null && tile.Units.Count == 0 && ViableTileTypes.CanBuildOn(tile));
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new KeepBuilder(input, ui, camera, input.Level.Package.GetBuildingType(ID), this);
		}
	}

	public class Keep : WalkableBuildingPlugin
	{

		public const string KeepTag = "Keep";
		const int BuildingHeight = 5;

		public ITile TileInFront { get; private set; }

		readonly Dictionary<ITile, IBuildingNode> nodes;
		readonly KeepWindow window;

		readonly KeepType myType;

		Clicker clicker;

		Keep(ILevelManager level, IBuilding building, KeepType myType)
			: base(level, building)
		{
			this.myType = myType;
			window = building.Player == level.HumanPlayer ? new KeepWindow(this) : null;

			TileInFront = level.Map.GetContainingTile(building.Center + building.Forward * 3);


			nodes = new Dictionary<ITile, IBuildingNode>();
			CreatePathfindingNodes();
		}

		public static Keep CreateNew(ILevelManager level, IBuilding building, KeepType myType)
		{
			Keep newKeep = new Keep(level, building, myType);
			StaticRangeTarget.CreateNew(newKeep, level, building.Center);
			newKeep.clicker = Clicker.CreateNew(newKeep, level);
			newKeep.clicker.Clicked += newKeep.KeepClicked;

			return newKeep;
		}

		public static Keep CreateForLoading(ILevelManager level, IBuilding building, KeepType myType)
		{
			return new Keep(level, building, myType);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{

		}

		public override void LoadState(PluginDataWrapper pluginData)
		{

		}

		public override void OnUpdate(float timeStep)
		{
			window?.OnUpdate(timeStep);
			if (!Level.EditorMode) {
				Building.Player.ChangeResourceAmount(myType.ProducedResource, myType.ProductionRate * timeStep);
			}
		}

		public override void Dispose()
		{
			foreach (var node in nodes.Values)
			{
				node.Remove();
			}

			clicker.Clicked -= KeepClicked;
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

			//Connect to tile in front
			var buildingFrontTile = Level.Map.GetContainingTile(Building.Center + Building.Forward * 2);
			ITileNode tileNode = Level.Map.PathFinding.GetTileNode(TileInFront);
			IBuildingNode buildingNode = nodes[buildingFrontTile];
			tileNode.CreateEdge(buildingNode, MovementType.Teleport);
			buildingNode.CreateEdge(tileNode, MovementType.Teleport);
		}

		void KeepClicked(int button, int buttons, int qualifiers)
		{
			//Is not null only for human player
			window?.Display();
		}
	}

	class KeepBuilder : DirectionalBuilder {

		readonly BaseCustomWindowUI cwUI;

		public KeepBuilder(GameController input, GameUI ui, CameraMover camera, BuildingType type, KeepType myType)
			: base(input, ui, camera, type)
		{
			cwUI = new BaseCustomWindowUI(ui, myType.Name, "");
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

	class KeepWindow : ExclusiveWindow {

		class KeepWindowInstance : IDisposable {

			Keep Keep => keepWindow.keep;

			readonly KeepWindow keepWindow;

			readonly Window window;
			readonly Button hideButton;
			readonly UIElement container;

			Dictionary<UIElement, SpawnableUnitTypePlugin> spawningButtons;

			public KeepWindowInstance(KeepWindow keepWindow)
			{
				this.keepWindow = keepWindow;

				Keep.Level.UIManager.LoadLayoutToUI("Assets/UI/KeepWindow.xml");
				this.window = (Window)Keep.Level.UIManager.UI.Root.GetChild("KeepWindow");

				this.hideButton = (Button)window.GetChild("HideButton");

				if (!Keep.Level.EditorMode) {

				}
				foreach (var unitTypeName in KeepType.SpawnedUnits) {
					var unitType = this.Keep.Level.Package.GetUnitType(unitTypeName);

					var button = container.CreateButton();
					button.SetStyle("SpawningCheckBox");
					button.Pressed += ButtonPressed;
					button.Texture = Keep.Level.Package.UnitIconTexture;
					button.ImageRect = unitType.IconRectangle;
					button.HoverOffset = new IntVector2(unitType.IconRectangle.Width(), 0);

					var unitTypePlugin = ((SpawnableUnitTypePlugin) unitType.Plugin);
					spawningButtons.Add(button, unitTypePlugin);
				}
				RegisterHandlers();

				window.Visible = false;
			}

			public void OnUpdate(float timeStep)
			{
				if (window.Visible) {
					foreach (var spawningButton in spawningButtons) {
						spawningButton.Key.Enabled = spawningButton.Value.Cost.HasResources(Keep.Building.Player);
					}
				}
				
			}

			public void Show()
			{
				window.Visible = true;
			}

			public void Hide()
			{
				window.Visible = false;
			}

			void ButtonPressed(PressedEventArgs args)
			{
				var unitTypePlugin = spawningButtons[args.Element];
				if (unitTypePlugin.Cost.HasResources(Keep.Building.Player)) {
					if (Keep.Level.SpawnUnit(unitTypePlugin.UnitType,
											Keep.TileInFront,
											Quaternion.Identity,
											Keep.Building.Player) != null) {
						unitTypePlugin.Cost.TakeFrom(Keep.Building.Player);
					}
				}
				else {
					args.Element.Enabled = false;
				}
			}

			public void Dispose()
			{
				hideButton.Dispose();
				container.Dispose();
				window.Remove();
			}

			void RegisterHandlers()
			{
				hideButton.Pressed += HideButtonPressed;
			}

			void HideButtonPressed(PressedEventArgs args)
			{
				Hide();
			}
		}

		readonly Keep keep;

		KeepWindowInstance instance;

		public KeepWindow(Keep keep)
		{
			this.keep = keep;
			instance = new KeepWindowInstance(this);
		}

		public void OnUpdate(float timeStep)
		{
			instance.OnUpdate(timeStep);
		}

		protected override void OnDisplay()
		{
			instance.Show();
		}

		protected override void OnHide()
		{
			instance.Hide();
		}

		public override void Dispose()
		{
			instance?.Dispose();
			instance = null;
		}
	}
}


