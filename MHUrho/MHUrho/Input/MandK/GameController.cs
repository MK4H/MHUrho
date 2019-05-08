using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using MHUrho.CameraMovement;
using Urho;
using Urho.IO;

using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Control;
using MHUrho.EditorTools;
using MHUrho.Helpers;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MandK;
using Urho.Gui;
using Urho.Urho2D;
using Urho.Resources;

namespace MHUrho.Input.MandK
{

	public delegate void OnMouseMoveDelegate(MHUrhoMouseMovedEventArgs e);

	public delegate void OnMouseDownDelegate(MouseButtonDownEventArgs e);

	public delegate void OnMouseUpDelegate(MouseButtonUpEventArgs e);

	public delegate void OnMouseWheelDelegate(MouseWheelEventArgs e);

	public delegate void ScreenBorderEventDelegate(ScreenBorder border);

	public enum ScreenBorder { Top, Bottom, Left, Right}

	public class GameController : Controller, IGameController
	{


		public IPlayer Player { get; private set; }

		GameUIManager IGameController.UIManager => UIManager;

		public GameUI UIManager { get; private set; }


		public InputType InputType => InputType.MouseAndKeyboard;

		public bool DoOnlySingleRaycasts { get; set; }

		public ILevelManager Level { get; private set; }

		public IntVector2 CursorPosition => UI.CursorPosition;

		public event OnMouseMoveDelegate MouseMove;
		public event OnMouseDownDelegate MouseDown;
		public event OnMouseUpDelegate MouseUp;
		public event OnMouseWheelDelegate MouseWheelMoved;

		public event ScreenBorderEventDelegate EnteredScreenBorder;
		public event ScreenBorderEventDelegate LeftScreenBorder;

	
		readonly Octree octree;
		readonly CameraMover camera;

	
		Dictionary<Key, Action<KeyUpEventArgs>> keyUpActions;
		Dictionary<Key, Action<KeyDownEventArgs>> keyDownActions;
		Dictionary<Key, Action<KeyDownEventArgs>> keyRepeatActions;



		const float CloseToBorder = 1/100f;

		/// <summary>
		/// Is set to null at the end of MouseDown, MouseMove, MouseUp and ViewMoved handlers
		/// </summary>
		ITile cachedTileUnderCursor;

		//For storing the cursor visibility on game pause
		bool cursorVisible;

		public GameController(ILevelManager level, Octree octree, IPlayer player, CameraMover cameraMover) : base() {
			this.camera = cameraMover;
			this.octree = octree;
			this.Level = level;
			this.DoOnlySingleRaycasts = true;
			this.Player = player;
			this.UIManager = new GameUI(this, cameraMover);
			this.keyDownActions = new Dictionary<Key, Action<KeyDownEventArgs>>();
			this.keyUpActions = new Dictionary<Key, Action<KeyUpEventArgs>>();
			this.keyRepeatActions = new Dictionary<Key, Action<KeyDownEventArgs>>();
			this.cursorVisible = UI.Cursor.Visible;

			cameraMover.CameraMoved += OnViewMoved;

			RegisterKeyDownAction(Key.Esc, SwitchToPause);

			Enable();

		}

		public void Dispose()
		{
			Disable();

			UIManager.Dispose();
			
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
			Vector3 resultPoint = camera.GetPointUnderInput(point, new Vector2(UI.Cursor.Position.X / (float) UI.Root.Width,
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

		public void ChangeControllingPlayer(IPlayer newControllingPlayer)
		{
			if (Player != newControllingPlayer) {
				Level.ToolManager.ClearPlayerSpecificState();
				Player = newControllingPlayer;
			}	
		}

		public void HideCursor() {
			UI.Cursor.Visible = false;
			cursorVisible = false;
			IntVector2 prevCursorPosition = CursorPosition;
			UI.Cursor.Position = new IntVector2(UI.Root.Width / 2, UI.Root.Height / 2);


			if (IsBorder(prevCursorPosition)) {
				List<ScreenBorder> prevBorders = GetBorders(prevCursorPosition);
				foreach (var prevBorder in prevBorders) {
					InvokeLeftScreenBorder(prevBorder);
				}
			}
		}

		/// <summary>
		/// Makes cursor visible
		/// </summary>
		/// <param name="abovePoint">world point above which the cursor should show up, or null if does not matter</param>
		public void ShowCursor(Vector3? abovePoint = null) {
			if (abovePoint != null) {
				var screenPoint = camera.Camera.WorldToScreenPoint(abovePoint.Value);
				UI.Cursor.Position = new IntVector2((int)(UI.Root.Width * screenPoint.X), (int)(UI.Root.Height * screenPoint.Y));
			}

			if (IsBorder(UI.Cursor.Position)) {
				List<ScreenBorder> borders = GetBorders(UI.Cursor.Position);
				foreach (var border in borders) {
					InvokeEnteredScreenBorder(border);
				}
			}

			UI.Cursor.Visible = true;
			cursorVisible = true;
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

		public void Pause()
		{
			UI.Cursor.Visible = true;
			Disable();
			Level.Pause();
			UIManager.HideUI();
			Game.MenuController.SwitchToPauseMenu(this);
		}

		public void UnPause()
		{
			UI.Cursor.Visible = cursorVisible;
			Enable();
			UIManager.ShowUI();
			Level.UnPause();
		}

		public void EndLevel()
		{
			Level.End();
		}

		protected override void KeyDown(KeyDownEventArgs e) {
			if (e.Repeat) {
				if (keyRepeatActions.TryGetValue(e.Key, out var repeatAction)) {
					InvokeKeyAction(repeatAction,e);
				}
			}
			else if (keyDownActions.TryGetValue(e.Key, out var downAction)) {
				InvokeKeyAction(downAction,e);
			}
		}

		protected override void KeyUp(KeyUpEventArgs e) {
			if (keyUpActions.TryGetValue(e.Key, out var upAction)) {
				InvokeKeyAction(upAction, e);
			}
		}

		protected override void MouseButtonDown(MouseButtonDownEventArgs e) {

			//Log.Write(LogLevel.Debug, $"Mouse button down at: X={UI.Cursor.Position.X}, Y={UI.Cursor.Position.Y}");

			InvokeMouseDown(e);
			cachedTileUnderCursor = null;
		}

		protected override void MouseButtonUp(MouseButtonUpEventArgs e) {
			//Log.Write(LogLevel.Debug, $"Mouse button up at: X={UI.Cursor.Position.X}, Y={UI.Cursor.Position.Y}");

			
			InvokeMouseUp(e);
			cachedTileUnderCursor = null;
		}

		protected override void MouseMoved(MouseMovedEventArgs e)
		{


			//Because of the software Cursor, the OS cursor stays in the middle of the screen and e.X and e.Y represent the OS cursor position 
			//UI.CursorPosition is updated after this, so i need to move it myself
			var args = new MHUrhoMouseMovedEventArgs(UI.CursorPosition + new IntVector2(e.DX,e.DY), e.DX, e.DY,(MouseButton) e.Buttons, e.Qualifiers);
			

			InvokeMouseMove(args);

			//Invisible cursor cannot be on the border
			if (!cursorVisible) {
				cachedTileUnderCursor = null;
				return;
			}

			IntVector2 prevMousePos = new IntVector2(args.X - args.DX, args.Y - args.DY);
			IntVector2 mousePos = new IntVector2(args.X, args.Y);

			if (IsBorder(prevMousePos) || IsBorder(mousePos)) {
				List<ScreenBorder> prevBorders = GetBorders(prevMousePos);
				List<ScreenBorder> borders = GetBorders(mousePos);

				foreach (var prevBorder in prevBorders) {
					if (!borders.Contains(prevBorder)) {
						InvokeLeftScreenBorder(prevBorder);
					}
				}


				foreach (var border in borders) {
					if (!prevBorders.Contains(border)) {
						InvokeEnteredScreenBorder(border);
					}
				}
			}



			cachedTileUnderCursor = null;
		}

		protected override void MouseWheel(MouseWheelEventArgs e)
		{
			InvokeMouseWheelMoved(e);
		}

		void SwitchToPause(KeyDownEventArgs e)
		{
			Pause();
		}

		void OnViewMoved(CameraMovedEventArgs args) {
			cachedTileUnderCursor = null;
		}

		Ray GetCursorRay() {
			return camera.Camera.GetScreenRay(UI.Cursor.Position.X / (float)UI.Root.Width,
											UI.Cursor.Position.Y / (float)UI.Root.Height);
		}

		bool IsBorder(IntVector2 screenPosition)
		{
			return screenPosition.X < UI.Root.Width * CloseToBorder ||
					screenPosition.X > UI.Root.Width * (1 - CloseToBorder) ||
					screenPosition.Y < UI.Root.Height * CloseToBorder ||
					screenPosition.Y > UI.Root.Height * (1 - CloseToBorder);
		}

		List<ScreenBorder> GetBorders(IntVector2 screenPosition)
		{
			List<ScreenBorder> borders = new List<ScreenBorder>(2);

			if (screenPosition.X < UI.Root.Width * CloseToBorder) {
				borders.Add(ScreenBorder.Left);
			}
			else if (screenPosition.X > UI.Root.Width * (1 - CloseToBorder)) {
				borders.Add(ScreenBorder.Right);
			}

			if (screenPosition.Y < UI.Root.Height * CloseToBorder) {
				borders.Add(ScreenBorder.Top);
			}
			else if (screenPosition.Y > UI.Root.Height * (1 - CloseToBorder)) {
				borders.Add(ScreenBorder.Bottom);
			}

			return borders;
		}


		void InvokeMouseMove(MHUrhoMouseMovedEventArgs args)
		{
			try
			{
				MouseMove?.Invoke(args);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(MouseMove)}: {e.Message}");
			}
		}

		void InvokeMouseDown(MouseButtonDownEventArgs args)
		{
			try
			{
				MouseDown?.Invoke(args);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(MouseDown)}: {e.Message}");
			}
		}

		void InvokeMouseUp(MouseButtonUpEventArgs args)
		{
			try
			{
				MouseUp?.Invoke(args);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(MouseUp)}: {e.Message}");
			}
		}

		void InvokeMouseWheelMoved(MouseWheelEventArgs args)
		{
			try
			{
				MouseWheelMoved?.Invoke(args);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(MouseWheelMoved)}: {e.Message}");
			}
		}

		void InvokeEnteredScreenBorder(ScreenBorder border)
		{
			try
			{
				EnteredScreenBorder?.Invoke(border);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(EnteredScreenBorder)}: {e.Message}");
			}
		}

		void InvokeLeftScreenBorder(ScreenBorder border)
		{
			try
			{
				LeftScreenBorder?.Invoke(border);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(LeftScreenBorder)}: {e.Message}");
			}
		}

		void InvokeKeyAction<T>(Action<T> keyAction, T args)
		{
			try
			{
				keyAction?.Invoke(args);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(keyAction)}: {e.Message}");
			}
		}
	}
}
