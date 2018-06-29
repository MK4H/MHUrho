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

	
		Dictionary<Key, Action<KeyUpEventArgs>> keyUpActions;
		Dictionary<Key, Action<KeyDownEventArgs>> keyDownActions;
		Dictionary<Key, Action<KeyDownEventArgs>> keyRepeatActions;

		CameraMovementType cameraType;

		const float CloseToBorder = 1/100f;

		bool mouseInLeftRight;
		bool mouseInTopBottom;

		/// <summary>
		/// Is set to null at the end of MouseDown, MouseMove, MouseUp and ViewMoved handlers
		/// </summary>
		ITile cachedTileUnderCursor;

		public GameMandKController(MyGame game, ILevelManager level, Octree octree, IPlayer player, CameraController cameraController) : base(game) {
			this.CameraScrollSensitivity = 20f;
			this.CameraRotationSensitivity = 15f;
			this.cameraType = CameraMovementType.Fixed;
			this.cameraController = cameraController;
			this.octree = octree;
			this.Level = level;
			this.DoOnlySingleRaycasts = true;
			this.Player = player;
			this.UIManager = new MandKGameUI(game, this);
			this.keyDownActions = new Dictionary<Key, Action<KeyDownEventArgs>>();
			this.keyUpActions = new Dictionary<Key, Action<KeyUpEventArgs>>();
			this.keyRepeatActions = new Dictionary<Key, Action<KeyDownEventArgs>>();

			cameraController.OnFixedMove += OnViewMoved;

			RegisterCameraControlKeys();

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

		public void RegisterKeyUpAction(Key key, Action<KeyUpEventArgs> action)
		{
			if (keyUpActions.TryGetValue(key, out var actions)) {
				keyUpActions[key] = actions + action;
			}
			else {
				keyUpActions.Add(key, action);
			}
		}

		public void RemoveKeyUpAction(Key key, Action<KeyUpEventArgs> action)
		{
			if (keyUpActions.TryGetValue(key, out var actions)) {
				actions -= action;
				if (actions == null) {
					keyUpActions.Remove(key);
				}
				else {
					keyUpActions[key] = actions;
				}
			}
		}

		public void RegisterKeyDownAction(Key key, Action<KeyDownEventArgs> action)
		{
			if (keyDownActions.TryGetValue(key, out var actions)) {
				keyDownActions[key] = actions + action;
			}
			else {
				keyDownActions.Add(key, action);
			}
		}

		public void RemoveKeyDownAction(Key key, Action<KeyDownEventArgs> action)
		{
			if (keyDownActions.TryGetValue(key, out var actions)) {
				actions -= action;
				if (actions == null) {
					keyDownActions.Remove(key);
				}
				else {
					keyDownActions[key] = actions;
				}
			}
		}

		public void RegisterKeyRepeatAction(Key key, Action<KeyDownEventArgs> action)
		{
			if (keyRepeatActions.TryGetValue(key, out var actions)) {
				keyRepeatActions[key] = actions + action;
			}
			else {
				keyRepeatActions.Add(key, action);
			}
		}

		public void RemoveKeyRepeatAction(Key key, Action<KeyDownEventArgs> action)
		{
			if (keyRepeatActions.TryGetValue(key, out var actions)) {
				actions -= action;
				if (actions == null) {
					keyRepeatActions.Remove(key);
				}
				else {
					keyRepeatActions[key] = actions;
				}
			}
		}

	
		
		protected override void KeyDown(KeyDownEventArgs e) {
			if (e.Repeat && keyRepeatActions.TryGetValue(e.Key, out var repeatAction)) {
				repeatAction?.Invoke(e);
			}
			else if (keyDownActions.TryGetValue(e.Key, out var downAction)) {
				downAction?.Invoke(e);
			}
		}

		protected override void KeyUp(KeyUpEventArgs e) {
			if (keyUpActions.TryGetValue(e.Key, out var upAction)) {
				upAction?.Invoke(e);
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

		protected override void MouseWheel(MouseWheelEventArgs e)
		{
			cameraController.AddZoom(e.Wheel * 10);
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

		//TODO: Read from config
		void RegisterCameraControlKeys()
		{
			RegisterKeyDownAction(Key.W, StartCameraMoveForward);
			RegisterKeyDownAction(Key.S, StartCameraMoveBackward);
			RegisterKeyDownAction(Key.A, StartCameraMoveLeft);
			RegisterKeyDownAction(Key.D, StartCameraMoveRight);
			RegisterKeyDownAction(Key.E, StartCameraRotationRight);
			RegisterKeyDownAction(Key.Q, StartCameraRotationLeft);
			RegisterKeyDownAction(Key.R, StartCameraRotationUp);
			RegisterKeyDownAction(Key.F, StartCameraRotationDown);
			RegisterKeyDownAction(Key.Shift, CameraSwitchMode);

			RegisterKeyUpAction(Key.W, StopCameraMoveForward);
			RegisterKeyUpAction(Key.S, StopCameraMoveBackward);
			RegisterKeyUpAction(Key.A, StopCameraMoveLeft);
			RegisterKeyUpAction(Key.D, StopCameraMoveRight);
			RegisterKeyUpAction(Key.E, StopCameraRotationRight);
			RegisterKeyUpAction(Key.Q, StopCameraRotationLeft);
			RegisterKeyUpAction(Key.R, StopCameraRotationUp);
			RegisterKeyUpAction(Key.F, StopCameraRotationDown);
		}

		void StartCameraMoveLeft(KeyDownEventArgs e) {
			var movement = cameraController.StaticMovement;
			movement.X = -CameraScrollSensitivity;
			cameraController.SetMovement(movement);
		}

		void StopCameraMoveLeft(KeyUpEventArgs e) {
			var movement = cameraController.StaticMovement;
			if (movement.X == -CameraScrollSensitivity) {
				movement.X = 0;
			}
			cameraController.SetMovement(movement);
		}

		void StartCameraMoveRight(KeyDownEventArgs e) {
			var movement = cameraController.StaticMovement;
			movement.X = CameraScrollSensitivity;
			cameraController.SetMovement(movement);
		}

		void StopCameraMoveRight(KeyUpEventArgs e) {
			var movement = cameraController.StaticMovement;
			if (movement.X == CameraScrollSensitivity) {
				movement.X = 0;
			}
			cameraController.SetMovement(movement);
		}

		void StartCameraMoveForward(KeyDownEventArgs e) {
			var movement = cameraController.StaticMovement;
			movement.Z = CameraScrollSensitivity;
			cameraController.SetMovement(movement);
		}

		void StopCameraMoveForward(KeyUpEventArgs e) {
			var movement = cameraController.StaticMovement;
			if (movement.Z == CameraScrollSensitivity) {
				movement.Z = 0;
			}
			cameraController.SetMovement(movement);
		}

		void StartCameraMoveBackward(KeyDownEventArgs e) {
			var movement = cameraController.StaticMovement;
			movement.Z = -CameraScrollSensitivity;
			cameraController.SetMovement(movement);
		}

		void StopCameraMoveBackward(KeyUpEventArgs e) {
			var movement = cameraController.StaticMovement;
			if (movement.Z == -CameraScrollSensitivity) {
				movement.Z = 0;
			}
			cameraController.SetMovement(movement);
		}

		void StartCameraRotationRight(KeyDownEventArgs e) {
			cameraController.SetYaw(CameraRotationSensitivity);
		}

		void StopCameraRotationRight(KeyUpEventArgs e) {
			if (cameraController.StaticYaw == CameraRotationSensitivity) {
				cameraController.SetYaw(0);
			}
		}

		void StartCameraRotationLeft(KeyDownEventArgs e) {
			cameraController.SetYaw(-CameraRotationSensitivity);
		}

		void StopCameraRotationLeft(KeyUpEventArgs e) {
			if (cameraController.StaticYaw == -CameraRotationSensitivity) {
				cameraController.SetYaw(0);
			}
		}

		void StartCameraRotationUp(KeyDownEventArgs e) {
			cameraController.SetPitch(CameraRotationSensitivity);
		}

		void StopCameraRotationUp(KeyUpEventArgs e) {
			if (cameraController.StaticPitch == CameraRotationSensitivity) {
				cameraController.SetPitch(0);
			}
		}

		void StartCameraRotationDown(KeyDownEventArgs e) {
			cameraController.SetPitch(-CameraRotationSensitivity);
		}

		void StopCameraRotationDown(KeyUpEventArgs e) {
			if (cameraController.StaticPitch == -CameraRotationSensitivity) {
				cameraController.SetPitch(0);
			}
		}

		void CameraSwitchMode(KeyDownEventArgs e) {
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
