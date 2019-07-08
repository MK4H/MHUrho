using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.DefaultComponents;
using MHUrho.EntityInfo;
using MHUrho.Helpers;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UserInterface.MandK;
using MHUrho.Helpers.Extensions;
using ShowcasePackage.Levels;
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

		public BuildingType MyTypeInstance { get; private set; }

		public static IReadOnlyList<string> SpawnedUnits = new List<string>{ChickenType.TypeName, WolfType.TypeName};

		public ViableTileTypes ViableTileTypes { get; private set; }
		const string CanBuildOnElement = "canBuildOn";

		public ResourceType ProducedResource { get; private set; }
		public double ProductionRate { get; private set; }
		const string ProducedResourceElement = "producedResource";
		const string ProductionRateAttribute = "rate";

		const float MaxHeightDiff = 0.5f;

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			MyTypeInstance = package.GetBuildingType(ID);

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

		public override bool CanBuild(IntVector2 topLeftTileIndex, IPlayer owner, ILevelManager level)
		{
			return owner.GetBuildingsOfType(MyTypeInstance).Count == 0 &&
					level.Map
						.GetTilesInRectangle(MyTypeInstance.GetBuildingTilesRectangle(topLeftTileIndex))
						.All((tile) => tile.Building == null && tile.Units.Count == 0 && ViableTileTypes.IsViable(tile)) &&
					HeightDiffLow(topLeftTileIndex, MyTypeInstance.GetBottomRightTileIndex(topLeftTileIndex), level, MaxHeightDiff);
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new KeepBuilder(input, ui, camera, this);
		}
	}

	public class Keep : WalkableBuildingPlugin
	{

		public const string KeepTag = "Keep";
		const int BuildingHeight = 5;

		public ITile TileInFront { get; private set; }

		readonly Dictionary<ITile, IBuildingNode> nodes;
		
		readonly KeepType myType;

		KeepWindow window;
		HealthBarControl healthBar;
		Clicker clicker;
		

		Keep(ILevelManager level, IBuilding building, KeepType myType)
			: base(level, building)
		{
			this.myType = myType;
	
			TileInFront = level.Map.GetContainingTile(building.Center + building.Forward * 3);


			nodes = new Dictionary<ITile, IBuildingNode>();
			CreatePathfindingNodes();
		}

		public static Keep CreateNew(ILevelManager level, IBuilding building, KeepType myType)
		{
			Keep newKeep = null;
			try {
				newKeep = new Keep(level, building, myType);
				StaticRangeTarget.CreateNew(newKeep, level, building.Center);
				newKeep.clicker = Clicker.CreateNew(newKeep, level);
				newKeep.clicker.Clicked += newKeep.KeepClicked;
				newKeep.healthBar =
					new HealthBarControl(level, building, 100, new Vector3(0, 3, 0), new Vector2(2f, 0.2f), false);
				newKeep.window = building.Player == level.HumanPlayer ? new KeepWindow(newKeep) : null;
				return newKeep;
			}
			catch (Exception e) {
				newKeep?.Dispose();
				throw;
			}
		}

		public static Keep CreateForLoading(ILevelManager level, IBuilding building, KeepType myType)
		{
			return new Keep(level, building, myType);
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


			clicker = Building.GetDefaultComponent<Clicker>();
			clicker.Clicked += KeepClicked;
			window = Building.Player == Level.HumanPlayer ? new KeepWindow(this) : null;
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
			window?.Dispose();
			healthBar?.Dispose();
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
			if (Building.Player.IsFriend(byEntity.Player) || byEntity is IProjectile)
			{
				return;
			}

			int damage = (int)userData;

			if (!healthBar.ChangeHitPoints(-damage)) {
				//Player will see this and end himself
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

		public KeepBuilder(GameController input, GameUI ui, CameraMover camera, KeepType type)
			: base(input, ui, camera, type.MyTypeInstance)
		{
			cwUI = new BaseCustomWindowUI(ui, type.Name, "");
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

			readonly Dictionary<UIElement, SpawnableUnitTypePlugin> spawningButtons;

			public KeepWindowInstance(KeepWindow keepWindow)
			{
				this.keepWindow = keepWindow;
				this.spawningButtons = new Dictionary<UIElement, SpawnableUnitTypePlugin>();
				var packageUI = ((LevelInstancePluginBase)Keep.Level.Plugin).PackageUI;
				packageUI.LoadLayoutToUI("Assets/UI/KeepWindow.xml");

				this.window = (Window)packageUI.PackageRoot.GetChild("KeepWindow");
				this.hideButton = (Button)window.GetChild("HideButton");
				this.container = window.GetChild("Container");

				try {
					if (!Keep.Level.EditorMode) {
						foreach (var unitTypeName in KeepType.SpawnedUnits) {
							var unitType = this.Keep.Level.Package.GetUnitType(unitTypeName);

							var button = container.CreateButton();
							button.SetStyle("SpawningCheckBox");
							button.Texture = Keep.Level.Package.UnitIconTexture;
							button.ImageRect = unitType.IconRectangle;
							button.HoverOffset = new IntVector2(unitType.IconRectangle.Width(), 0);

							var unitTypePlugin = ((SpawnableUnitTypePlugin) unitType.Plugin);
							spawningButtons.Add(button, unitTypePlugin);
						}
					}

				}
				catch (Exception) {
					Dispose();
					throw;
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
				UnregisterHandlers();

				foreach (var button in spawningButtons.Keys) {
					((Button) button).Pressed -= ButtonPressed;
				}

				hideButton.Dispose();
				container.Dispose();
				window.Remove();
			}

			void RegisterHandlers()
			{
				hideButton.Pressed += HideButtonPressed;

				foreach (var pair in spawningButtons) {
					Button button = (Button) pair.Key;
					button.Pressed += ButtonPressed;
					Keep.Level.UIManager.RegisterForHover(button);
				}
			}

			void UnregisterHandlers()
			{
				hideButton.Pressed -= HideButtonPressed;

				foreach (var pair in spawningButtons)
				{
					Button button = (Button)pair.Key;
					button.Pressed -= ButtonPressed;
					Keep.Level.UIManager.UnregisterForHover(button);
				}
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


