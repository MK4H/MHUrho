using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Urho;
using Urho.IO;

using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Control;
using MHUrho.EditorTools;
using MHUrho.UserInterface;
using Urho.Gui;
using Urho.Urho2D;
using Urho.Resources;

namespace MHUrho.Input
{
	

	public class GameMandKController : MandKController, IGameController
	{
		public delegate void OnMouseMove(MouseMovedEventArgs e);

		public delegate void OnMouseDown(MouseButtonDownEventArgs e);

		public delegate void OnMouseUp(MouseButtonUpEventArgs e);


		enum CameraMovementType { Fixed, FreeFloat }

		enum Mode { LockedToPoint, MouseAreaSelection, WorldAreaSelection}

		enum Actions {  CameraMoveForward = 0,
								CameraMoveBackward,
								CameraMoveLeft,
								CameraMoveRight,
								CameraRotationRight,
								CameraRotationLeft,
								CameraRotationUp,
								CameraRotationDown,
								CameraSwitchMode
		}

		struct KeyAction {
			public Action<int> KeyDown;
			public Action<int> Repeat;
			public Action<int> KeyUp;

			public KeyAction(Action<int> keyDown, Action<int> repeat, Action<int> keyUp) {
				this.KeyDown = keyDown;
				this.Repeat = repeat;
				this.KeyUp = keyUp;
			}
		}

		public IPlayer Player { get; set; }

		GameUIManager IGameController.UIManager => UIManager;

		public MandKGameUI UIManager { get; private set; }


		public InputType InputType => InputType.MouseAndKeyboard;

		public bool DoOnlySingleRaycasts { get; set; }

		public float CameraScrollSensitivity { get; set; }

		public float CameraRotationSensitivity { get; set; }

		public bool MouseBorderCameraMovement { get; set; }

		public bool UIHovering { get; set; }

		public ILevelManager Level { get; private set; }

		public event OnMouseMove MouseMove;
		public event OnMouseDown MouseDown;
		public event OnMouseUp MouseUp;

		
		readonly CameraController cameraController;
		readonly Octree octree;

		

		List<KeyAction> actions;
		Dictionary<Key, Actions> keyActions;

		CameraMovementType cameraType;

		const float CloseToBorder = 1/100f;

		bool mouseInLeftRight;
		bool mouseInTopBottom;

		/// <summary>
		/// Is set to null at the end of MouseDown, MouseMove, MouseUp and ViewMoved handlers
		/// </summary>
		ITile cachedTileUnderCursor;

		public GameMandKController(MyGame game, ILevelManager level, IPlayer player, CameraController cameraController) : base(game) {
			this.CameraScrollSensitivity = 20f;
			this.CameraRotationSensitivity = 15f;
			this.cameraType = CameraMovementType.Fixed;
			this.cameraController = cameraController;
			this.octree = level.Scene.GetComponent<Octree>();
			this.Level = level;
			this.DoOnlySingleRaycasts = true;
			this.Player = player;
			this.UIManager = new MandKGameUI(game, this);

			cameraController.OnFixedMove += OnViewMoved;

			FillActionList();

			//TODO: Load from config
			SetKeyBindings();

			Enable();

			UIManager.AddTool(new VertexHeightToolMandK(this));
			UIManager.AddTool(new TileTypeToolMandK(this));
			UIManager.AddTool(new TileHeightToolMandK(this));
			UIManager.AddTool(new UnitSelectorToolMandK(this));
			UIManager.AddTool(new UnitSpawningToolMandK(this));
			UIManager.AddTool(new BuildingBuilderToolMandK(this));

		}

		public void Dispose()
		{
			cameraController.Dispose();
			UIManager.Dispose();
			Disable();
		}

		public List<RayQueryResult> CursorRaycast() {
			var cursorRay = GetCursorRay();
			return octree.Raycast(cursorRay);
		}

		public RayQueryResult? CursorRaycastFirstOnly() {
			var cursorRay = GetCursorRay();
			return octree.RaycastSingle(cursorRay);
		}

		public float RaycastHeightAbovePoint(Vector3 point) {
			Vector3 resultPoint = cameraController.GetPointUnderInput(point, new Vector2(UI.Cursor.Position.X / (float) UI.Root.Width,
																						 UI.Cursor.Position.Y / (float) UI.Root.Height));
			return resultPoint.Y;
		}

		/// <summary>
		/// Gets the map matrix coordinates of the tile corner closest to the cursor
		/// 
		/// <seealso cref="GetClosestTileCornerPosition"/>
		/// </summary>
		/// <returns></returns>
		public IntVector2? GetClosestTileCorner() {
			return Level.Map.RaycastToVertex(CursorRaycast());
		}

		/// <summary>
		/// Gets the world position of the tile corner closest to the cursor
		/// </summary>
		/// <returns></returns>
		public Vector3? GetClosestTileCornerPosition() {
			return Level.Map.RaycastToVertexPosition(CursorRaycast());
		}

		/// <summary>
		/// Gets tile currently under the cursor, is cached for the calls in the same
		/// handler, so all tools calling this from MouseMove will get the same value,
		/// calculated on the first call
		/// </summary>
		/// <returns>Tile under the cursor</returns>
		public ITile GetTileUnderCursor() {
			if (cachedTileUnderCursor != null) {
				return cachedTileUnderCursor;
			}
			var raycast = CursorRaycast();
			return (cachedTileUnderCursor = Level.Map.RaycastToTile(raycast));
		}

		public void HideCursor() {
			UI.Cursor.Visible = false;
			UI.Cursor.Position = new IntVector2(UI.Root.Width / 2, UI.Root.Height / 2);
		}

		/// <summary>
		/// Makes cursor visible
		/// </summary>
		/// <param name="abovePoint">world point above which the cursor should show up, or null if does not matter</param>
		public void ShowCursor(Vector3? abovePoint = null) {
			if (abovePoint != null) {
				var screenPoint = cameraController.Camera.WorldToScreenPoint(abovePoint.Value);
				UI.Cursor.Position = new IntVector2((int)(UI.Root.Width * screenPoint.X), (int)(UI.Root.Height * screenPoint.Y));
			}
			UI.Cursor.Visible = true;
		}

		public void AddTool(Tool tool)
		{
			UIManager.AddTool(tool);
		}

		public void RemoveTool(Tool tool)
		{
			UIManager.RemoveTool(tool);
		}

		public void RegisterToolAction(int number, Action<int> keyDown, Action<int> repeat = null, Action<int> keyUp = null) {
			//TODO: REDO THE ACTION MAPPING
			actions[number] = new KeyAction(keyDown, repeat, keyUp);
		}

		public void UnregisterToolAction(int number) {
			actions[number] = new KeyAction();
		}

		void FillActionList() {
			//TODO: REDO ACTION LIST
			actions = new List<KeyAction> {
				new KeyAction(StartCameraMoveForward, null, StopCameraMoveForward),
				new KeyAction(StartCameraMoveBackward, null, StopCameraMoveBackward),
				new KeyAction(StartCameraMoveLeft, null, StopCameraMoveLeft),
				new KeyAction(StartCameraMoveRight, null, StopCameraMoveRight),
				new KeyAction(StartCameraRotationRight, null, StopCameraRotationRight),
				new KeyAction(StartCameraRotationLeft, null, StopCameraRotationLeft),
				new KeyAction(StartCameraRotationUp, null, StopCameraRotationUp),
				new KeyAction(StartCameraRotationDown, null, StopCameraRotationDown),
				new KeyAction(CameraSwitchMode, null, null),
				new KeyAction()
			};
		}
		
		//TODO: Load from config
		void SetKeyBindings() {
			keyActions = new Dictionary<Key, Actions> {
				{Key.W, Actions.CameraMoveForward},
				{Key.S, Actions.CameraMoveBackward},
				{Key.A, Actions.CameraMoveLeft},
				{Key.D, Actions.CameraMoveRight},
				{Key.E, Actions.CameraRotationRight},
				{Key.Q, Actions.CameraRotationLeft},
				{Key.R, Actions.CameraRotationUp },
				{Key.F, Actions.CameraRotationDown },
				{Key.LeftShift, Actions.CameraSwitchMode },
				{Key.C, (Actions)9 }
			};
		}

		KeyAction GetAction(Actions action) {
			return actions[(int)action];
		}

		protected override void KeyDown(KeyDownEventArgs e) {
			if (keyActions.TryGetValue(e.Key, out Actions action)) {
				if (!e.Repeat) {
					GetAction(action).KeyDown?.Invoke(e.Qualifiers);
				}
				else {
					GetAction(action).Repeat?.Invoke(e.Qualifiers);
				}
			}
		}

		protected override void KeyUp(KeyUpEventArgs e) {
			if (keyActions.TryGetValue(e.Key, out Actions action)) {
				GetAction(action).KeyUp?.Invoke(e.Qualifiers);
			}
		}

		protected override void MouseButtonDown(MouseButtonDownEventArgs e) {
			if (!UIHovering) {
				Log.Write(LogLevel.Debug, $"Mouse button down at: X={UI.Cursor.Position.X}, Y={UI.Cursor.Position.Y}");

				MouseDown?.Invoke(e);
				cachedTileUnderCursor = null;
			}   
		}

		protected override void MouseButtonUp(MouseButtonUpEventArgs e) {
			if (!UIHovering) {
				Log.Write(LogLevel.Debug, $"Mouse button up at: X={UI.Cursor.Position.X}, Y={UI.Cursor.Position.Y}");

				MouseUp?.Invoke(e);
				cachedTileUnderCursor = null;
			}
			
		}

		protected override void MouseMoved(MouseMovedEventArgs e) {
			if (cameraType == CameraMovementType.FreeFloat) {
				cameraController.AddRotation(new Vector2(e.DY, -e.DX) * MouseSensitivity);
			}
			else if (cameraType == CameraMovementType.Fixed) {

				MouseBorderMovement(UI.Cursor.Position);

				MouseMove?.Invoke(e);
				cachedTileUnderCursor = null;
				//DrawHighlight();
			}

		}

		protected override void MouseWheel(MouseWheelEventArgs e) {

		}

		void OnViewMoved(Vector3 movement, Vector2 rotation, float timeStep) {
			cachedTileUnderCursor = null;
		}

		Ray GetCursorRay() {
			return cameraController.Camera.GetScreenRay(UI.Cursor.Position.X / (float)UI.Root.Width,
														UI.Cursor.Position.Y / (float)UI.Root.Height);
		}

		void DrawHighlight() {
			var clickedRay = cameraController.Camera.GetScreenRay(UI.Cursor.Position.X / (float)UI.Root.Width,
																  UI.Cursor.Position.Y / (float)UI.Root.Height);
			var raycastResult = octree.RaycastSingle(clickedRay);
			if (raycastResult.HasValue) {
 
				//ITile centerTile = levelManager.Map.RaycastToTile(raycastResult.Value);
				//if (centerTile != null && (cursorTile == null || centerTile != cursorTile)) {
				//    levelManager.Map.HighlightArea(centerTile, new IntVector2(3, 3));
				//    cursorTile = centerTile;
				//}
			}
		}

		void MouseBorderMovement(IntVector2 mousePos) {

			Vector2 cameraMovement = new Vector2(cameraController.StaticMovement.X, cameraController.StaticMovement.Z);

			if (!mouseInLeftRight) {
				//Mouse was not in the border area before, check if it is now 
				// and if it is, set the movement
				if (mousePos.X < UI.Root.Width * CloseToBorder) {
					cameraMovement.X = -CameraScrollSensitivity;
					mouseInLeftRight = true;
				}
				else if (mousePos.X > UI.Root.Width * (1 - CloseToBorder)) {
					cameraMovement.X = CameraScrollSensitivity;
					mouseInLeftRight = true;
				}

			}
			else if (UI.Root.Width * CloseToBorder <= mousePos.X && mousePos.X <= UI.Root.Width * (1 - CloseToBorder)) {
				//Mouse was in the area, and now it is not, reset the movement
				cameraMovement.X = 0;
				mouseInLeftRight = false;
			}

			if (!mouseInTopBottom) {
				if (mousePos.Y < UI.Root.Height * CloseToBorder) {
					cameraMovement.Y = CameraScrollSensitivity;
					mouseInTopBottom = true;
				}
				else if (mousePos.Y > UI.Root.Height * (1 - CloseToBorder)) {
					cameraMovement.Y = -CameraScrollSensitivity;
					mouseInTopBottom = true;
				}
			}
			else if (UI.Root.Height * CloseToBorder <= mousePos.Y && mousePos.Y <= UI.Root.Height * (1 - CloseToBorder)) {
				cameraMovement.Y = 0;
				mouseInTopBottom = false;
			}

			cameraController.SetHorizontalMovement(cameraMovement);
		}

		void StartCameraMoveLeft(int qualifiers) {
			var movement = cameraController.StaticMovement;
			movement.X = -CameraScrollSensitivity;
			cameraController.SetMovement(movement);
		}

		void StopCameraMoveLeft(int qualifiers) {
			var movement = cameraController.StaticMovement;
			if (movement.X == -CameraScrollSensitivity) {
				movement.X = 0;
			}
			cameraController.SetMovement(movement);
		}

		void StartCameraMoveRight(int qualifiers) {
			var movement = cameraController.StaticMovement;
			movement.X = CameraScrollSensitivity;
			cameraController.SetMovement(movement);
		}

		void StopCameraMoveRight(int qualifiers) {
			var movement = cameraController.StaticMovement;
			if (movement.X == CameraScrollSensitivity) {
				movement.X = 0;
			}
			cameraController.SetMovement(movement);
		}

		void StartCameraMoveForward(int qualifiers) {
			var movement = cameraController.StaticMovement;
			movement.Z = CameraScrollSensitivity;
			cameraController.SetMovement(movement);
		}

		void StopCameraMoveForward(int qualifiers) {
			var movement = cameraController.StaticMovement;
			if (movement.Z == CameraScrollSensitivity) {
				movement.Z = 0;
			}
			cameraController.SetMovement(movement);
		}

		void StartCameraMoveBackward(int qualifiers) {
			var movement = cameraController.StaticMovement;
			movement.Z = -CameraScrollSensitivity;
			cameraController.SetMovement(movement);
		}

		void StopCameraMoveBackward(int qualifiers) {
			var movement = cameraController.StaticMovement;
			if (movement.Z == -CameraScrollSensitivity) {
				movement.Z = 0;
			}
			cameraController.SetMovement(movement);
		}

		void StartCameraRotationRight(int qualifiers) {
			cameraController.SetYaw(-CameraRotationSensitivity);
		}

		void StopCameraRotationRight(int qualifiers) {
			if (cameraController.StaticYaw == -CameraRotationSensitivity) {
				cameraController.SetYaw(0);
			}
		}

		void StartCameraRotationLeft(int qualifiers) {
			cameraController.SetYaw(CameraRotationSensitivity);
		}

		void StopCameraRotationLeft(int qualifiers) {
			if (cameraController.StaticYaw == CameraRotationSensitivity) {
				cameraController.SetYaw(0);
			}
		}

		void StartCameraRotationUp(int qualifiers) {
			cameraController.SetPitch(-CameraRotationSensitivity);
		}

		void StopCameraRotationUp(int qualifiers) {
			if (cameraController.StaticPitch == -CameraRotationSensitivity) {
				cameraController.SetPitch(0);
			}
		}

		void StartCameraRotationDown(int qualifiers) {
			cameraController.SetPitch(CameraRotationSensitivity);
		}

		void StopCameraRotationDown(int qualifiers) {
			if (cameraController.StaticPitch == CameraRotationSensitivity) {
				cameraController.SetPitch(0);
			}
		}

		void CameraSwitchMode(int qualifiers) {
			if (cameraType == CameraMovementType.FreeFloat) {
				cameraController.SwitchToFixed();
				cameraType = CameraMovementType.Fixed;
				UI.Cursor.Visible = true;
			}
			else {
				cameraController.SwitchToFree();
				cameraType = CameraMovementType.FreeFloat;
				UI.Cursor.Visible = false;
				Level.Map.DisableHighlight();
			}
		}

	}
}
