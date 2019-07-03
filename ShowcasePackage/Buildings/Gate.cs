using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.Control;
using MHUrho.Helpers;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using MHUrho.Helpers.Extensions;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;
using ShowcasePackage.Levels;
using ShowcasePackage.Misc;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	public class GateType : BaseBuildingTypePlugin {

		public static string TypeName = "Gate";

		public static int TypeID = 3;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public Cost Cost { get; private set; }
		public ViableTileTypes ViableTileTypes { get; private set; }

		const string CostElement = "cost";
		const string CanBuildOnElement = "canBuildOn";

		BuildingType myType;

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return GateInstance.CreateNew(level, building);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return GateInstance.CreateForLoading(level, building);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, IPlayer owner, ILevelManager level)
		{
			return level.Map
						.GetTilesInRectangle(topLeftTileIndex, bottomRightTileIndex)
						.All((tile) => tile.Building == null && tile.Units.Count == 0 && ViableTileTypes.CanBuildOn(tile));
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new GateBuilder(input, ui, camera, myType, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			XElement costElem = extensionElement.Element(package.PackageManager.GetQualifiedXName(CostElement));
			Cost = Cost.FromXml(costElem, package);

			XElement canBuildOnElem =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(CanBuildOnElement));
			ViableTileTypes = ViableTileTypes.FromXml(canBuildOnElem, package);

			myType = package.GetBuildingType(ID);
		}
	}

	public class GateInstance : WalkableBuildingPlugin {


		class Door {

			/// <summary>
			/// Time to open or close the doors in seconds
			/// </summary>
			public double OpeningTime { get; set; }
			public bool IsOpen { get; private set; }
			public bool IsMoving { get; private set; }


			readonly Node node;
			readonly double openAngle;
			readonly double closedAngle;

			/// <summary>
			/// Remaining amount of rotation to target.
			/// </summary>
			double remainingRotation;

			/// <summary>
			/// Angle of the door at the start of the movement.
			/// </summary>
			double start;

			/// <summary>
			/// Angle of the door at the end of the movement.
			/// </summary>
			double target;

			/// <summary>
			/// Target of the current movement is the Open position.
			/// Differs from <see cref="IsOpen"/>, because it is changed at the start
			/// of the movement, whereas <see cref="IsOpen"/> is changed at the end.
			/// </summary>
			bool isTargetOpen;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="doorNode"></param>
			/// <param name="openAngle"></param>
			/// <param name="closedAngle"></param>
			/// <param name="openingTime">Time to open or close the doors in seconds</param>
			/// <param name="isOpen"></param>
			public Door(Node doorNode, double openAngle, double closedAngle, double openingTime, bool isOpen)
			{
				this.node = doorNode;
				this.openAngle = openAngle;
				this.closedAngle = closedAngle;
				this.OpeningTime = openingTime;
				this.IsOpen = isOpen;
				this.isTargetOpen = isOpen;
				this.remainingRotation = 0;
				this.start = isOpen ? closedAngle : openAngle;
				this.target = isOpen ? openAngle : closedAngle;

				SetAngle((float)target);
			}

			public void Open()
			{
				if (isTargetOpen) {
					return;
				}

				remainingRotation = openAngle - closedAngle;
				start = closedAngle;
				target = openAngle;
				isTargetOpen = true;
				IsMoving = true;
			}

			public void Close()
			{
				if (!isTargetOpen)
				{
					return;
				}

				remainingRotation = closedAngle - openAngle;
				start = openAngle;
				target = closedAngle;
				isTargetOpen = false;
				IsMoving = true;
			}

			public void Show()
			{
				node.Enabled = true;
			}

			public void Hide()
			{
				node.Enabled = false;
			}

			public void SetOpen()
			{
				Open();
				SetAngle((float) target);
				IsOpen = true;
				isTargetOpen = true;
				IsMoving = false;
			}

			public void SetClosed()
			{
				Close();
				SetAngle((float)target);
				IsOpen = false;
				isTargetOpen = false;
				IsMoving = false;
			}

			public void OnUpdate(float timeStep)
			{
				if (!IsMoving) {
					return;
				}
				double rotation = target - start;
				bool positiveMovement = rotation > 0;
				bool positiveRemaining = remainingRotation > 0;
				if ((positiveMovement && positiveRemaining) || (!positiveMovement && !positiveRemaining)) {
					//Moving to target 
					double tickRotation = (rotation / OpeningTime) * timeStep;
					node.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, (float)tickRotation));
					remainingRotation -= tickRotation;
				}
				else {
					//On target angle
					SetAngle((float)target);
					IsOpen = isTargetOpen;
					IsMoving = false;
				}
			}

			void SetAngle(float angle)
			{
				node.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, angle);
			}
		}

		public static readonly object GateTunnelTag = "GateTunnel";
		public static readonly object GateDoorTag = "GateDoor";
		public static readonly object GateRoofTag = "GateRoof";

		public bool IsOpen => leftDoor.IsOpen && rightDoor.IsOpen;
		public bool IsDoorMoving => leftDoor.IsMoving || rightDoor.IsMoving;

		readonly Door leftDoor;
		readonly Door rightDoor;

		readonly Dictionary<ITile, IBuildingNode> roofNodes;
		readonly Dictionary<ITile, IBuildingNode> tunnelNodes;

		Clicker clicker;

		GateWindow window;

		GateInstance(ILevelManager level, IBuilding building)
			: base(level, building)
		{

			this.leftDoor = new Door(building.Node.GetChild("Door_l"), 90, 180, 5, true);
			this.rightDoor = new Door(building.Node.GetChild("Door_r"), 90, 0, 5, true);
			this.roofNodes = new Dictionary<ITile, IBuildingNode>();
			this.tunnelNodes = new Dictionary<ITile, IBuildingNode>();
			this.window = new GateWindow(this);
			
			CreatePathfindingNodes();
		}

		public static GateInstance CreateNew(ILevelManager level, IBuilding building)
		{
			GateInstance newGate = new GateInstance(level, building);
			StaticRangeTarget.CreateNew(newGate, level, building.Center);
			newGate.clicker = Clicker.CreateNew(newGate, level);
			newGate.clicker.Clicked += newGate.OnClicked;
			return newGate;
		}

		public static GateInstance CreateForLoading(ILevelManager level, IBuilding building)
		{
			return new GateInstance(level, building);
		}

		public void Open()
		{
			leftDoor.Open();
			rightDoor.Open();
		}

		public void Close()
		{
			leftDoor.Close();
			rightDoor.Close();
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			writer.StoreNext(IsOpen);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var reader = pluginData.GetReaderForWrappedSequentialData();
			reader.GetNext(out bool isOpen);

			if (isOpen) {
				leftDoor.SetOpen();
				rightDoor.SetOpen();
			}
			else {
				leftDoor.SetClosed();
				rightDoor.SetClosed();
			}

			clicker = Building.GetDefaultComponent<Clicker>();
			clicker.Clicked += OnClicked;
		}

		public override void Dispose()
		{
			clicker.Clicked -= OnClicked;
			foreach (var node in roofNodes.Values)
			{
				node.Remove();
			}

			foreach (var node in tunnelNodes.Values)
			{
				node.Remove();
			}
		}

		public override void OnUpdate(float timeStep)
		{
			leftDoor.OnUpdate(timeStep);
			rightDoor.OnUpdate(timeStep);
			window?.OnUpdate(timeStep);
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
			
		}

		public override IBuildingNode TryGetNodeAt(ITile tile)
		{
			return roofNodes.TryGetValue(tile, out IBuildingNode value) ? value : null;
		}

		public override float? GetHeightAt(float x, float y)
		{
			return Level.Map.GetTerrainHeightAt(x,y) + 5;
		}

		public override IFormationController GetFormationController(Vector3 centerPosition)
		{
			ITile tile = Level.Map.GetContainingTile(centerPosition);
			if (!roofNodes.TryGetValue(tile, out IBuildingNode startNode)) {
				return null;
			}
			return new BFSRoofFormationController((LevelPluginBase)Level.Plugin, startNode);
		}

		void OnClicked(int button, int buttons, int qualifiers)
		{
			window.Display();
		}

		void CreatePathfindingNodes()
		{
			//Roof nodes
			for (int y = Building.Rectangle.Top; y < Building.Rectangle.Bottom; y++)
			{
				for (int x = Building.Rectangle.Left; x < Building.Rectangle.Right; x++)
				{
					ITile tile = Level.Map.GetTileByTopLeftCorner(x, y);
					Vector3 position = new Vector3(tile.Center.X, GetHeightAt(tile.Center.X, tile.Center.Y).Value, tile.Center.Y);
					IBuildingNode node = Level.Map.PathFinding.CreateBuildingNode(Building, position, GateRoofTag);
					roofNodes.Add(tile, node);
				}
			}

			//Connect roof edges
			foreach (var tileAndNode in roofNodes) {
				ITile tile = tileAndNode.Key;
				IBuildingNode node = tileAndNode.Value;
				foreach (var neighbour in tile.GetNeighbours())
				{
					if (neighbour == null) {
						continue;
					}
					//Connect to neighbor roof nodes
					if (roofNodes.TryGetValue(neighbour, out IBuildingNode neighbourNode))
					{
						node.CreateEdge(neighbourNode, MovementType.Linear);
					}
					else if (neighbour.Building != null &&
							neighbour.Building.BuildingPlugin is WalkableBuildingPlugin plugin) {
						INode foreighNode = plugin.TryGetNodeAt(neighbour);
						foreighNode.CreateEdge(node, MovementType.Teleport);
						node.CreateEdge(foreighNode, MovementType.Teleport);
					}
				}
			}

			//Gate nodes
			Vector3 centerPosition = Building.Center;

			//Goes through the strip of tiles at the center of the Gate, withY 0 because nodes have to follow
			// the flat base of the building
			List<IBuildingNode> newTunnelNodes = new List<IBuildingNode>();
			for (int i = -2; i < 2; i++) {
				Vector3 position = centerPosition + i * Building.Forward.WithY(0);
				ITile tile = Level.Map.GetContainingTile(position);
				IBuildingNode node = Level.Map.PathFinding.CreateBuildingNode(Building, position, GateTunnelTag);
				tunnelNodes.Add(tile, node);
				newTunnelNodes.Add(node);
			}

			Vector3 doorPosition = centerPosition + 2 * Building.Forward.WithY(0);
			ITile doorInnerTile = Level.Map.GetContainingTile(doorPosition);
			IBuildingNode doorNode = Level.Map.PathFinding.CreateBuildingNode(Building, 
																		doorPosition, 
																		GateTunnelTag);
			tunnelNodes.Add(doorInnerTile, doorNode);
			newTunnelNodes.Add(doorNode);

			//Connect tunnel edges
			for (int i = 1; i < newTunnelNodes.Count; i++) {
				newTunnelNodes[i - 1].CreateEdge(newTunnelNodes[i], MovementType.Linear);
				newTunnelNodes[i].CreateEdge(newTunnelNodes[i - 1], MovementType.Linear);
			}

			//Connect front node and back node to outside tiles
			ITile backTile = Level.Map.GetContainingTile(centerPosition - 3 * Building.Forward);
			ITile frontTile = Level.Map.GetContainingTile(centerPosition + 3 * Building.Forward);
			INode backNode = Level.Map.PathFinding.GetTileNode(backTile);
			INode frontNode = Level.Map.PathFinding.GetTileNode(frontTile);

			backNode.CreateEdge(newTunnelNodes[0], MovementType.Linear);
			newTunnelNodes[0].CreateEdge(backNode, MovementType.Linear);
			frontNode.CreateEdge(newTunnelNodes[newTunnelNodes.Count - 1], MovementType.Linear);
			newTunnelNodes[newTunnelNodes.Count - 1].CreateEdge(frontNode, MovementType.Linear);

			//Connect roof with the tunnel
			ITile centerTile = Level.Map.GetContainingTile(Building.Center);
			tunnelNodes[centerTile].CreateEdge(roofNodes[centerTile], MovementType.Teleport);
			roofNodes[centerTile].CreateEdge(tunnelNodes[centerTile], MovementType.Teleport);

		}
	}

	class GateBuilder : DirectionalBuilder {

		readonly BaseCustomWindowUI cwUI;

		public GateBuilder(GameController input, GameUI ui, CameraMover camera, BuildingType buildingType, GateType myType)
			: base(input, ui, camera, buildingType)
		{
			AbleFront = Color.Red;
			AbleBack = Color.Yellow;

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

	class GateWindow : ExclusiveWindow
	{

		class GateWindowInstance : IDisposable
		{

			GateInstance Gate => gateWindow.gate;

			readonly GateWindow gateWindow;

			readonly Window window;
			readonly Button hideButton;
			readonly Button openButton;
			readonly Button closeButton;

			public GateWindowInstance(GateWindow gateWindow)
			{
				this.gateWindow = gateWindow;
				Gate.Level.UIManager.LoadLayoutToUI("Assets/UI/GateWindow.xml");
				this.window = (Window)Gate.Level.UIManager.UI.Root.GetChild("GateWindow");

				this.hideButton = (Button)window.GetChild("HideButton");

				this.openButton = (Button)window.GetChild("OpenButton", true);
				this.closeButton = (Button)window.GetChild("CloseButton", true);

				if (Gate.IsOpen)
				{
					openButton.Enabled = false;
					closeButton.Enabled = true;
				}

				RegisterHandlers();
			}

			public void OnUpdate(float timeStep)
			{
				if (!window.Visible)
				{
					return;
				}

				if (Gate.IsDoorMoving)
				{
					openButton.Enabled = false;
					closeButton.Enabled = false;
					return;
				}

				if (Gate.IsOpen)
				{
					openButton.Enabled = false;
					closeButton.Enabled = true;
				}
				else
				{
					openButton.Enabled = true;
					closeButton.Enabled = false;
				}
			}

			public void Dispose()
			{
				UnregisterHandlers();
				hideButton.Dispose();
				openButton.Dispose();
				closeButton.Dispose();
				window.Remove();
			}

			void RegisterHandlers()
			{
				hideButton.Pressed += HideButtonPressed;
				openButton.Pressed += OpenButtonPressed;
				closeButton.Pressed += CloseButtonPressed;
			}

			void UnregisterHandlers()
			{
				hideButton.Pressed -= HideButtonPressed;
				openButton.Pressed -= OpenButtonPressed;
				closeButton.Pressed -= CloseButtonPressed;
			}

			void CloseButtonPressed(PressedEventArgs obj)
			{
				Gate.Close();
			}

			void OpenButtonPressed(PressedEventArgs obj)
			{
				Gate.Open();
			}

			void HideButtonPressed(PressedEventArgs obj)
			{
				gateWindow.Hide();
			}
		}

		readonly GateInstance gate;

		GateWindowInstance instance;

		public GateWindow(GateInstance gate)
		{
			this.gate = gate;
		}

		protected override void OnDisplay()
		{
			instance = new GateWindowInstance(this);
		}

		protected override void OnHide()
		{
			instance?.Dispose();
			instance = null;
		}

		public void OnUpdate(float timeStep)
		{
			instance?.OnUpdate(timeStep);
		}

		public override void Dispose()
		{
			if (instance != null)
			{
				Hide();
			}
		}
	}


}
