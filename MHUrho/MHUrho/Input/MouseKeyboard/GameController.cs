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
using MHUrho.UserInterface.MouseKeyboard;
using Urho.Gui;
using Urho.Urho2D;
using Urho.Resources;

namespace MHUrho.Input.MouseKeyboard
{

	/// <summary>
	/// Encapsulates methods that handle mouse move events.
	/// </summary>
	/// <param name="e">The mouse movement event data.</param>
	public delegate void OnMouseMoveDelegate(MHUrhoMouseMovedEventArgs e);

	/// <summary>
	/// Encapsulates methods that handle mouse button press events.
	/// </summary>
	/// <param name="e">The mouse button press event data.</param>
	public delegate void OnMouseDownDelegate(MouseButtonDownEventArgs e);

	/// <summary>
	/// Encapsulates methods that handle mouse button release events.
	/// </summary>
	/// <param name="e">The mouse button release event data.</param>
	public delegate void OnMouseUpDelegate(MouseButtonUpEventArgs e);

	/// <summary>
	/// Encapsulates methods that handle mouse wheel movement events.
	/// </summary>
	/// <param name="e">The mouse wheel movement event data.</param>
	public delegate void OnMouseWheelDelegate(MouseWheelEventArgs e);

	/// <summary>
	/// Encapsulates methods that handle the events of cursor entering or leaving the border area of game window.
	/// </summary>
	/// <param name="e">The screen border entered or left.</param>
	public delegate void ScreenBorderEventDelegate(ScreenBorder border);

	/// <summary>
	/// Represents the four areas of screen border.
	/// </summary>
	public enum ScreenBorder { Top, Bottom, Left, Right}

	/// <summary>
	/// Captures user input during the game.
	/// Provides control over the game running state.
	/// </summary>
	public class GameController : Controller, IGameController
	{

		/// <summary>
		/// The player user is currently controlling.
		/// </summary>
		public IPlayer Player { get; private set; }

		/// <summary>
		/// The user interface controller.
		/// </summary>
		GameUIManager IGameController.UIManager => UIManager;

		/// <summary>
		/// The user interface controller.
		/// </summary>
		public GameUI UIManager { get; private set; }

		/// <inheritdoc />
		public InputType InputType => InputType.MouseAndKeyboard;

		/// <summary>
		/// If raycasts should only be done until the first intersection,
		///  or if they should be done until some range limit.
		/// </summary>
		public bool DoOnlySingleRaycasts { get; set; }

		/// <summary>
		/// The controlled level.
		/// </summary>
		public ILevelManager Level { get; private set; }

		/// <summary>
		/// Current cursor position in screen coordinates.
		/// </summary>
		public IntVector2 CursorPosition => UI.CursorPosition;

		/// <summary>
		/// Invoked on mouse movement.
		/// </summary>
		public event OnMouseMoveDelegate MouseMove;

		/// <summary>
		/// Invoked on mouse button press.
		/// </summary>
		public event OnMouseDownDelegate MouseDown;

		/// <summary>
		/// Invoked on mouse button release.
		/// </summary>
		public event OnMouseUpDelegate MouseUp;

		/// <summary>
		/// Invoked on mouse wheel movement.
		/// </summary>
		public event OnMouseWheelDelegate MouseWheelMoved;

		/// <summary>
		/// Invoked when the cursor enters an area near the screen border.
		/// </summary>
		public event ScreenBorderEventDelegate EnteredScreenBorder;

		/// <summary>
		/// Invoked when the cursor leaves an area near the screen border.
		/// </summary>
		public event ScreenBorderEventDelegate LeftScreenBorder;

		/// <summary>
		/// The game engine component for raycasting.
		/// </summary>
		readonly Octree octree;

		/// <summary>
		/// The comonent controlling camera movement.
		/// </summary>
		readonly CameraMover camera;

		/// <summary>
		/// Mapping of keys to actions to invoke on the key release.
		/// </summary>
		readonly Dictionary<Key, Action<KeyUpEventArgs>> keyUpActions;

		/// <summary>
		/// Mapping of keys to actions to invoke on key press.
		/// </summary>
		readonly Dictionary<Key, Action<KeyDownEventArgs>> keyDownActions;

		/// <summary>
		/// Mapping of keys to actions to invoke while the key is held down.
		/// </summary>
		readonly Dictionary<Key, Action<KeyDownEventArgs>> keyRepeatActions;


		/// <summary>
		/// Percentage of the screen that counts as a border area.
		/// </summary>
		const float CloseToBorder = 1/100f;

		/// <summary>
		/// Is set to null at the end of MouseDown, MouseMove, MouseUp and ViewMoved handlers
		/// </summary>
		ITile cachedTileUnderCursor;


		/// <summary>
		/// For storing the cursor visibility on game pause
		/// </summary>
		bool cursorVisible;

		/// <summary>
		/// Creates an user input provider and level control facade.
		/// </summary>
		/// <param name="level">The level the new instance will control.</param>
		/// <param name="octree">The engine component used for raycasting.</param>
		/// <param name="player">The player that will own the user input at the start.</param>
		/// <param name="cameraMover">The component for camera movement.</param>
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

		/// <summary>
		/// Removes registrations from the engine input subsystem and releases the UI.
		/// </summary>
		public void Dispose()
		{
			Disable();

			UIManager.Dispose();	
		}

		/// <summary>
		/// Returns all intersections in the game world with a ray cast from camera through the cursor.
		/// Mainly used to see what the user has the mouse over or what he clicked.
		/// </summary>
		/// <returns>All intersections of the ray from camera through the cursor.</returns>
		public List<RayQueryResult> CursorRaycast() {
			var cursorRay = GetCursorRay();
			return octree.Raycast(cursorRay);
		}

		/// <summary>
		/// Returns the closest visible thing the cursor is over or null if the cursor is not over any game objects. 
		/// </summary>
		/// <returns>Returns the closest visible thing the cursor is over, or null if the cursor is not over any game objects.</returns>
		public RayQueryResult? CursorRaycastFirstOnly() {
			var cursorRay = GetCursorRay();
			return octree.RaycastSingle(cursorRay);
		}

		/// <summary>
		/// Returns the height of the cursor above the <paramref name="point"/> when we project the
		/// cursor onto an Y axis going through the <paramref name="point"/>.
		/// </summary>
		/// <param name="point">The world position the height is measured from.</param>
		/// <returns>The height of the cursor above <paramref name="point"/> when we project the
		/// cursor onto an Y axis going through the <paramref name="point"/>.</returns>
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
		/// <returns>The coordinates of the tile corner closest to the intersection of the map with a ray from camera through cursor.</returns>
		public IntVector2? GetClosestTileCorner() {
			return Level.Map.RaycastToVertex(CursorRaycast());
		}

		/// <summary>
		/// Gets the world position of the tile corner closest to the cursor.
		/// </summary>
		/// <returns>The position of the tile corner closest to the intersection of the map with a ray from camera through cursor.</returns>
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

		/// <summary>
		/// Hides the cursor graphical representation, disables cursor movement.
		/// </summary>
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

		/// <summary>
		/// Registers a handler <paramref name="action"/> that will be invoked when the <paramref name="key"/> is released.
		/// </summary>
		/// <param name="key">The key this handler will respond to.</param>
		/// <param name="action">The action to invoke.</param>
		public void RegisterKeyUpAction(Key key, Action<KeyUpEventArgs> action)
		{
			if (keyUpActions.TryGetValue(key, out var actions)) {
				keyUpActions[key] = actions + action;
			}
			else {
				keyUpActions.Add(key, action);
			}
		}

		/// <summary>
		/// Removes the registered handler <paramref name="action"/> from the <paramref name="key"/> release event.
		/// </summary>
		/// <param name="key">The key the handler responded to.</param>
		/// <param name="action">The registered action.</param>
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

		/// <summary>
		/// Registers a handler <paramref name="action"/> that will be invoked when the <paramref name="key"/> is pressed.
		/// </summary>
		/// <param name="key">The key this handler will respond to.</param>
		/// <param name="action">The action to invoke.</param>
		public void RegisterKeyDownAction(Key key, Action<KeyDownEventArgs> action)
		{
			if (keyDownActions.TryGetValue(key, out var actions)) {
				keyDownActions[key] = actions + action;
			}
			else {
				keyDownActions.Add(key, action);
			}
		}

		/// <summary>
		/// Removes the registered handler <paramref name="action"/> from the <paramref name="key"/> pressed event.
		/// </summary>
		/// <param name="key">The key the handler responded to.</param>
		/// <param name="action">The registered action.</param>
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

		/// <summary>
		/// Registers a handler <paramref name="action"/> that will be invoked when the <paramref name="key"/> is held down.
		/// </summary>
		/// <param name="key">The key this handler will respond to.</param>
		/// <param name="action">The action to invoke.</param>
		public void RegisterKeyRepeatAction(Key key, Action<KeyDownEventArgs> action)
		{
			if (keyRepeatActions.TryGetValue(key, out var actions)) {
				keyRepeatActions[key] = actions + action;
			}
			else {
				keyRepeatActions.Add(key, action);
			}
		}

		/// <summary>
		/// Removes the registered handler <paramref name="action"/> from the <paramref name="key"/> held down event.
		/// </summary>
		/// <param name="key">The key the handler responded to.</param>
		/// <param name="action">The registered action.</param>
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

		/// <summary>
		/// Pauses the controlled level.
		/// </summary>
		public void Pause()
		{
			UI.Cursor.Visible = true;
			Disable();
			Level.Pause();
			UIManager.HideUI();
			Game.MenuController.SwitchToPauseMenu(this);
		}

		/// <summary>
		/// Unpauses the controlled level.
		/// </summary>
		public void UnPause()
		{
			UI.Cursor.Visible = cursorVisible;
			Enable();
			UIManager.ShowUI();
			Level.UnPause();
		}

		/// <summary>
		/// Ends level and switches to the End level screen, displaying either victory or defeat
		/// based on <paramref name="victory"/> value.
		/// </summary>
		/// <param name="victory">If he level ended with player's victory.</param>
		public void EndLevelToEndScreen(bool victory)
		{
			Game.MenuController.SwitchToEndScreen(victory);
			Level.End();
		}

		/// <summary>
		/// Stops level and releases all resources held by the level.
		/// </summary>
		public void EndLevel()
		{
			Level.End();
		}

		/// <summary>
		/// Handles game engine key down event and translates it to platform key pressed event handling.
		/// </summary>
		/// <param name="e">The data of the engine key down event.</param>
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

		/// <summary>
		/// Handles game engine key up event and translates it to platform key released event handling.
		/// </summary>
		/// <param name="e">The data of the engine key up event.</param>
		protected override void KeyUp(KeyUpEventArgs e) {
			if (keyUpActions.TryGetValue(e.Key, out var upAction)) {
				InvokeKeyAction(upAction, e);
			}
		}

		/// <summary>
		/// Handles the engine mouse button down event and translates it to platform mouse button pressed event.
		/// </summary>
		/// <param name="e">The engine mouse button down data.</param>
		protected override void MouseButtonDown(MouseButtonDownEventArgs e) {
			InvokeMouseDown(e);
			cachedTileUnderCursor = null;
		}

		/// <summary>
		/// Handles the engine mouse button up event and translates it to platform mouse button released event.
		/// </summary>
		/// <param name="e">The engine mouse button up data.</param>
		protected override void MouseButtonUp(MouseButtonUpEventArgs e) {		
			InvokeMouseUp(e);
			cachedTileUnderCursor = null;
		}

		/// <summary>
		/// Handles the engine mouse moved event and translates it to platform mouse moved event.
		/// </summary>
		/// <param name="e">The engine mouse button down data.</param>
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

			IntVector2 prevMousePos = new IntVector2(args.X - args.DeltaX, args.Y - args.DeltaY);
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

		/// <summary>
		/// Handles the engine mouse wheel event and translates it to platform mouse wheel moved event.
		/// </summary>
		/// <param name="e">The engine mouse wheel data.</param>
		protected override void MouseWheel(MouseWheelEventArgs e)
		{
			InvokeMouseWheelMoved(e);
		}

		/// <summary>
		/// Pauses the level and switches to the pause screen.
		/// </summary>
		/// <param name="e">The key down event data this responds to.</param>
		void SwitchToPause(KeyDownEventArgs e)
		{
			Pause();
		}

		/// <summary>
		/// Resets the cached tile under cursor after the camera is moved.
		/// </summary>
		/// <param name="args">The camera moved event data.</param>
		void OnViewMoved(CameraMovedEventArgs args) {
			cachedTileUnderCursor = null;
		}

		/// <summary>
		/// Gets a ray going from camera through cursor.
		/// </summary>
		/// <returns>A ray going from camera through cursor.</returns>
		Ray GetCursorRay() {
			return camera.Camera.GetScreenRay(UI.Cursor.Position.X / (float)UI.Root.Width,
											UI.Cursor.Position.Y / (float)UI.Root.Height);
		}

		/// <summary>
		/// Returns if the <paramref name="screenPosition"/> is in a border area of the game window.
		/// </summary>
		/// <param name="screenPosition">The position on the screen to evaluate.</param>
		/// <returns>True if it is in a border area, false if it is not.</returns>
		bool IsBorder(IntVector2 screenPosition)
		{
			return screenPosition.X < UI.Root.Width * CloseToBorder ||
					screenPosition.X > UI.Root.Width * (1 - CloseToBorder) ||
					screenPosition.Y < UI.Root.Height * CloseToBorder ||
					screenPosition.Y > UI.Root.Height * (1 - CloseToBorder);
		}

		/// <summary>
		/// Gets all border areas the <paramref name="screenPosition"/> is inside.
		/// </summary>
		/// <param name="screenPosition">The position on the screen to evaluate.</param>
		/// <returns>All border areas the <paramref name="screenPosition"/> is inside.</returns>
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

		/// <summary>
		/// Safely invokes the <see cref="MouseMove"/> event with the given <paramref name="args"/>.
		/// Protects against plugin thrown exceptions.
		/// </summary>
		/// <param name="args">The arguments to invoke the event with</param>
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

		/// <summary>
		/// Safely invokes the <see cref="MouseDown"/> event with the given <paramref name="args"/>.
		/// Protects against plugin thrown exceptions.
		/// </summary>
		/// <param name="args">The arguments to invoke the event with</param>
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

		/// <summary>
		/// Safely invokes the <see cref="MouseUp"/> event with the given <paramref name="args"/>.
		/// Protects against plugin thrown exceptions.
		/// </summary>
		/// <param name="args">The arguments to invoke the event with</param>
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

		/// <summary>
		/// Safely invokes the <see cref="MouseWheelMoved"/> event with the given <paramref name="args"/>.
		/// Protects against plugin thrown exceptions.
		/// </summary>
		/// <param name="args">The arguments to invoke the event with</param>
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

		/// <summary>
		/// Safely invokes the <see cref="EnteredScreenBorder"/> event with the given <paramref name="border"/>.
		/// Protects against plugin thrown exceptions.
		/// </summary>
		/// <param name="border">The border that was entered.</param>
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

		/// <summary>
		/// Safely invokes the <see cref="LeftScreenBorder"/> event with the given <paramref name="border"/>.
		/// Protects against plugin thrown exceptions.
		/// </summary>
		/// <param name="border">The border that was left.</param>
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

		/// <summary>
		/// Safely invokes the action from any of the mappings, with the given <paramref name="args"/>.
		/// Protects against plugin thrown exceptions.
		/// </summary>
		/// <param name="keyAction">The action to invoke.</param>
		/// <param name="args">The arguments to invoke the action with.</param>
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
