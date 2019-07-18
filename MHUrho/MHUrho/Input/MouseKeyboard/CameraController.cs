using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.Helpers;
using MHUrho.UserInterface;
using MHUrho.Logic;
using MHUrho.UserInterface.MouseKeyboard;
using Urho;
using Urho.Gui;

namespace MHUrho.Input.MouseKeyboard
{
	/// <summary>
	/// Translates user input into camera movement.
	/// </summary>
    public class CameraController : ICameraController
    {
		/// <summary>
		/// Mode of the camera movement.
		/// </summary>
		enum CameraMovementType { Fixed, FreeFloat }

		/// <summary>
		/// Represents current movement of the camera.
		/// </summary>
		struct CameraMovements {
			public bool MoveForward;
			public bool MoveBackward;
			public bool MoveLeft;
			public bool MoveRight;
			public bool RotateUp;
			public bool RotateDown;
			public bool RotateLeft;
			public bool RotateRight;
			public bool BorderMovementUp;
			public bool BorderMovementDown;
			public bool BorderMovementLeft;
			public bool BorderMovementRight;

			/// <summary>
			/// Represents that all camera movement was stopped.
			/// </summary>
			public void StopAll()
			{
				MoveForward = false;
				MoveBackward = false;
				MoveLeft = false;
				MoveRight = false;
				RotateUp = false;
				RotateDown = false;
				RotateLeft = false;
				RotateRight = false;
				BorderMovementUp = false;
				BorderMovementDown = false;
				BorderMovementLeft = false;
				BorderMovementRight = false;
			}
		}

		/// <summary>
		/// Camera movement sensitivity (speed)
		/// </summary>
		public float CameraScrollSensitivity { get; set; }

		/// <summary>
		/// Camera rotation sensitivity (speed)
		/// </summary>
		public float CameraRotationSensitivity { get; set; }

		/// <summary>
		/// Mouse sensitivity when translating to camera rotation.
		/// </summary>
		public float MouseRotationSensitivity { get; set; }

		/// <summary>
		/// Zoom sensitivity.
		/// </summary>
		public float WheelSensitivity { get; set; }

		/// <summary>
		/// If the camera should be moved when the cursor gets close to the app window border.
		/// </summary>
		public bool MouseBorderCameraMovement { get; set; }

		/// <summary>
		/// Current mode of the camera movement
		/// </summary>
		CameraMovementType cameraType;
		
		/// <summary>
		/// Input provider.
		/// </summary>
		readonly GameController input;

		/// <summary>
		/// UI control.
		/// </summary>
		readonly GameUI ui;

		/// <summary>
		/// Component directing camera movement.
		/// </summary>
		readonly CameraMover camera;

		/// <summary>
		/// Current camera movement representation.
		/// </summary>
		CameraMovements activeCameraMovement = new CameraMovements();

		/// <summary>
		/// Creates a translator from user input to camera movement.
		/// </summary>
		/// <param name="input">The user input provider.</param>
		/// <param name="ui">User interface controller.</param>
		/// <param name="cameraMover">The component directing the camera movement.</param>
		public CameraController(GameController input, GameUI ui, CameraMover cameraMover)
		{
			this.input = input;
			this.ui = ui;
			this.camera = cameraMover;
			this.CameraRotationSensitivity = 10.0f;
			this.CameraScrollSensitivity = 10.0f;
			this.WheelSensitivity = 10.0f;
			this.MouseRotationSensitivity = 0.5f;
			this.MouseBorderCameraMovement = true;
			this.cameraType = CameraMovementType.Fixed;

			input.MouseMove += OnMouseMoved;
			input.MouseWheelMoved += OnMouseWheel;
			input.EnteredScreenBorder += OnScreenBorderEntered;
			input.LeftScreenBorder += OnScreenBorderLeft;

			RegisterCameraControlKeys();
		}

		/// <summary>
		/// Removes all registered handlers for events, releases camera.
		/// </summary>
		public void Dispose()
		{
			input.MouseMove -= OnMouseMoved;
			input.MouseWheelMoved -= OnMouseWheel;
			input.EnteredScreenBorder -= OnScreenBorderEntered;
			input.LeftScreenBorder -= OnScreenBorderLeft;
			camera.Dispose();
		}

		/// <summary>
		/// Registers handlers for keyboard events.
		/// </summary>
		/// <remarks>
		/// In future could be read from config file.
		/// </remarks>
		//FUTURE: Read from config
		void RegisterCameraControlKeys()
		{
			input.RegisterKeyDownAction(Key.W, StartCameraMoveForward);
			input.RegisterKeyDownAction(Key.S, StartCameraMoveBackward);
			input.RegisterKeyDownAction(Key.A, StartCameraMoveLeft);
			input.RegisterKeyDownAction(Key.D, StartCameraMoveRight);
			input.RegisterKeyDownAction(Key.E, StartCameraRotationRight);
			input.RegisterKeyDownAction(Key.Q, StartCameraRotationLeft);
			input.RegisterKeyDownAction(Key.R, StartCameraRotationUp);
			input.RegisterKeyDownAction(Key.F, StartCameraRotationDown);
			input.RegisterKeyDownAction(Key.Shift, CameraSwitchMode);
			input.RegisterKeyDownAction(Key.Backspace, ResetCamera);

			input.RegisterKeyUpAction(Key.W, StopCameraMoveForward);
			input.RegisterKeyUpAction(Key.S, StopCameraMoveBackward);
			input.RegisterKeyUpAction(Key.A, StopCameraMoveLeft);
			input.RegisterKeyUpAction(Key.D, StopCameraMoveRight);
			input.RegisterKeyUpAction(Key.E, StopCameraRotationRight);
			input.RegisterKeyUpAction(Key.Q, StopCameraRotationLeft);
			input.RegisterKeyUpAction(Key.R, StopCameraRotationUp);
			input.RegisterKeyUpAction(Key.F, StopCameraRotationDown);
		}

		/// <summary>
		/// Invoked when the mouse cursor enters area near the game window border.
		/// </summary>
		/// <param name="border">Which border area the cursor entered</param>
		void OnScreenBorderEntered(ScreenBorder border)
		{
			if (!MouseBorderCameraMovement) return;

			Vector2 horizontalMovement = camera.StaticHorizontalMovement;
			switch (border) {
				case ScreenBorder.Top:
					horizontalMovement.Y += CameraScrollSensitivity;
					activeCameraMovement.BorderMovementUp = true;
					break;
				case ScreenBorder.Bottom:
					horizontalMovement.Y -= CameraScrollSensitivity;
					activeCameraMovement.BorderMovementDown = true;
					break;
				case ScreenBorder.Left:
					horizontalMovement.X -= CameraScrollSensitivity;
					activeCameraMovement.BorderMovementLeft = true;
					break;
				case ScreenBorder.Right:
					horizontalMovement.X += CameraScrollSensitivity;
					activeCameraMovement.BorderMovementRight = true;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(border), border, null);
			}

			camera.SetStaticHorizontalMovement(horizontalMovement);
			
		}

		/// <summary>
		/// Invoked when the mouse cursor leaves an area near the game window border.
		/// </summary>
		/// <param name="border">The border area the cursor left.</param>
		void OnScreenBorderLeft(ScreenBorder border)
		{
			if (!MouseBorderCameraMovement) {
				return;
			}

			Vector2 horizontalMovement = camera.StaticHorizontalMovement;
			switch (border) {
				case ScreenBorder.Top:
					if (!activeCameraMovement.BorderMovementUp) {
						return;
					}
					activeCameraMovement.BorderMovementUp = false;

					horizontalMovement.Y -= CameraScrollSensitivity;
					if (FloatHelpers.FloatsEqual(horizontalMovement.Y, 0)) {
						horizontalMovement.Y = 0;
					}

					break;
				case ScreenBorder.Bottom:
					if (!activeCameraMovement.BorderMovementDown) {
						return;
					}
					activeCameraMovement.BorderMovementDown = false;

					horizontalMovement.Y += CameraScrollSensitivity;
					if (FloatHelpers.FloatsEqual(horizontalMovement.Y, 0)) {
						horizontalMovement.Y = 0;
					}

					break;
				case ScreenBorder.Left:
					if (!activeCameraMovement.BorderMovementLeft) {
						return;
					}
					activeCameraMovement.BorderMovementLeft = false;

					horizontalMovement.X += CameraScrollSensitivity;
					if (FloatHelpers.FloatsEqual(horizontalMovement.X, 0)) {
						horizontalMovement.X = 0;
					}

					break;
				case ScreenBorder.Right:
					if (!activeCameraMovement.BorderMovementRight) {
						return;
					}
					activeCameraMovement.BorderMovementRight = false;

					horizontalMovement.X -= CameraScrollSensitivity;
					if (FloatHelpers.FloatsEqual(horizontalMovement.X, 0)) {
						horizontalMovement.X = 0;
					}

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(border), border, null);
			}

			camera.SetStaticHorizontalMovement(horizontalMovement);
		}

		/// <summary>
		/// Handles when mouse wheel moves.
		/// </summary>
		/// <param name="args">The mouse wheel moved event data.</param>
		void OnMouseWheel(MouseWheelEventArgs args)
		{
			if (!ui.UIHovering) {
				camera.AddDecayingZoomChange(args.Wheel * WheelSensitivity);
			}
		}

		/// <summary>
		/// Handles mouse moved event.
		/// </summary>
		/// <param name="args">The mouse moved event data.</param>
		void OnMouseMoved(MHUrhoMouseMovedEventArgs args)
		{
			if (cameraType == CameraMovementType.FreeFloat) {
				camera.AddDecayingRotation(new Vector2(args.DeltaX, args.DeltaY) * MouseRotationSensitivity);
			}
		}

		/// <summary>
		/// Starts camera movement to the left.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void StartCameraMoveLeft(KeyDownEventArgs args)
		{
			var movement = camera.StaticMovement;
			movement.X -= CameraScrollSensitivity;
			camera.SetStaticMovement(movement);

			activeCameraMovement.MoveLeft = true;
		}

		/// <summary>
		/// Stops the camera movement to the left.
		/// </summary>
		/// <param name="args">The key up event data.</param>
		void StopCameraMoveLeft(KeyUpEventArgs args)
		{
			//If the camera movement was stoped by other means, dont stop it again
			if (!activeCameraMovement.MoveLeft) {
				return;
			}
			activeCameraMovement.MoveLeft = false;

			var movement = camera.StaticMovement;
			movement.X += CameraScrollSensitivity;
			if (FloatHelpers.FloatsEqual(movement.X, 0)) {
				movement.X = 0;
			}
			camera.SetStaticMovement(movement);
		}

		/// <summary>
		/// Starts camera movement to the right.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void StartCameraMoveRight(KeyDownEventArgs args)
		{
			var movement = camera.StaticMovement;
			movement.X += CameraScrollSensitivity;
			camera.SetStaticMovement(movement);

			activeCameraMovement.MoveRight = true;
		}

		/// <summary>
		/// Stops the camera movement to the right. 
		/// </summary>
		/// <param name="args">The key up event data.</param>
		void StopCameraMoveRight(KeyUpEventArgs args)
		{
			//If the camera movement was stoped by other means, dont stop it again
			if (!activeCameraMovement.MoveRight) {
				return;
			}
			activeCameraMovement.MoveRight = false;

			var movement = camera.StaticMovement;
			movement.X -= CameraScrollSensitivity;
			if (FloatHelpers.FloatsEqual(movement.X, 0)) {
				movement.X = 0;
			}
			camera.SetStaticMovement(movement);
		}

		/// <summary>
		/// Starts camera movement to the forward.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void StartCameraMoveForward(KeyDownEventArgs args)
		{
			var movement = camera.StaticMovement;
			movement.Z += CameraScrollSensitivity;
			camera.SetStaticMovement(movement);

			activeCameraMovement.MoveForward = true;
		}

		/// <summary>
		/// Stops the camera movement forward.
		/// </summary>
		/// <param name="args">The key up event data.</param>
		void StopCameraMoveForward(KeyUpEventArgs args)
		{
			//If the camera movement was stoped by other means, dont stop it again
			if (!activeCameraMovement.MoveForward) {
				return;
			}
			activeCameraMovement.MoveForward = false;

			var movement = camera.StaticMovement;
			movement.Z -= CameraScrollSensitivity;
			if (FloatHelpers.FloatsEqual(movement.Z, 0)) {
				movement.Z = 0;
			}
			camera.SetStaticMovement(movement);
		}

		/// <summary>
		/// Starts camera movement backward.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void StartCameraMoveBackward(KeyDownEventArgs args)
		{
			var movement = camera.StaticMovement;
			movement.Z -= CameraScrollSensitivity;
			camera.SetStaticMovement(movement);

			activeCameraMovement.MoveBackward = true;
		}

		/// <summary>
		/// Stops the camera movement backwards.
		/// </summary>
		/// <param name="args">The key up event data.</param>
		void StopCameraMoveBackward(KeyUpEventArgs args)
		{
			//If the camera movement was stoped by other means, dont stop it again
			if (!activeCameraMovement.MoveBackward) {
				return;
			}
			activeCameraMovement.MoveBackward = false;

			var movement = camera.StaticMovement;
			movement.Z += CameraScrollSensitivity;
			if (FloatHelpers.FloatsEqual(movement.Z, 0)) {
				movement.Z = 0;
			}
			camera.SetStaticMovement(movement);
		}

		/// <summary>
		/// Starts camera rotation to the right.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void StartCameraRotationRight(KeyDownEventArgs args)
		{
			camera.SetStaticYawChange(camera.StaticYaw + CameraRotationSensitivity);

			activeCameraMovement.RotateRight = true;
		}

		/// <summary>
		/// Stops the camera rotation to the right. 
		/// </summary>
		/// <param name="args">The key up event data.</param>
		void StopCameraRotationRight(KeyUpEventArgs args)
		{
			if (!activeCameraMovement.RotateRight) {
				return;
			}
			activeCameraMovement.RotateRight = false;

			var yaw = camera.StaticYaw - CameraRotationSensitivity;
			if (FloatHelpers.FloatsEqual(yaw, 0)) {
				yaw = 0;
			}
			camera.SetStaticYawChange(yaw);
		}

		/// <summary>
		/// Starts camera rotation to the left.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void StartCameraRotationLeft(KeyDownEventArgs args)
		{
			camera.SetStaticYawChange(camera.StaticYaw - CameraRotationSensitivity);

			activeCameraMovement.RotateLeft = true;
		}

		/// <summary>
		/// Stops the camera rotation to the left, 
		/// </summary>
		/// <param name="args">The key up event data.</param>
		void StopCameraRotationLeft(KeyUpEventArgs args)
		{
			if (!activeCameraMovement.RotateLeft) {
				return;
			}
			activeCameraMovement.RotateLeft = false;

			var yaw = camera.StaticYaw + CameraRotationSensitivity;
			if (FloatHelpers.FloatsEqual(yaw, 0)) {
				yaw = 0;
			}
			camera.SetStaticYawChange(yaw);
		}

		/// <summary>
		/// Starts camera rotation up.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void StartCameraRotationUp(KeyDownEventArgs args)
		{
			camera.SetStaticPitchChange(camera.StaticPitch + CameraRotationSensitivity);

			activeCameraMovement.RotateUp = true;
		}

		/// <summary>
		/// Stops the camera rotation up, 
		/// </summary>
		/// <param name="args">The key up event data.</param>
		void StopCameraRotationUp(KeyUpEventArgs args)
		{
			if (!activeCameraMovement.RotateUp) {
				return;
			}
			activeCameraMovement.RotateUp = false;

			var pitch = camera.StaticPitch - CameraRotationSensitivity;
			if (FloatHelpers.FloatsEqual(pitch, 0)) {
				pitch = 0;
			}
			camera.SetStaticPitchChange(pitch);
		}

		/// <summary>
		/// Starts camera rotation down.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void StartCameraRotationDown(KeyDownEventArgs args)
		{
			camera.SetStaticPitchChange(camera.StaticPitch - CameraRotationSensitivity);

			activeCameraMovement.RotateDown = true;
		}

		/// <summary>
		/// Stops the camera rotation down.
		/// </summary>
		/// <param name="args">The key up event data.</param>
		void StopCameraRotationDown(KeyUpEventArgs args)
		{
			if (!activeCameraMovement.RotateDown) {
				return;
			}
			activeCameraMovement.RotateDown = false;

			var pitch = camera.StaticPitch + CameraRotationSensitivity;
			if (FloatHelpers.FloatsEqual(pitch, 0)) {
				pitch = 0;
			}
			camera.SetStaticPitchChange(pitch);
		}

		/// <summary>
		/// Switches camera mode to/from free float mode,
		/// based on the current camera mode.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void CameraSwitchMode(KeyDownEventArgs args)
		{
			activeCameraMovement.StopAll();

			if (cameraType == CameraMovementType.FreeFloat) {
				camera.SwitchToFixed();
				cameraType = CameraMovementType.Fixed;
				input.ShowCursor();
			}
			else {
				camera.SwitchToFree();
				cameraType = CameraMovementType.FreeFloat;
				input.HideCursor();
				input.Level.Map.DisableHighlight();
				input.Level.ToolManager.DisableTools();
			}
		}

		/// <summary>
		/// Resets the camera to it's default offset and rotation.
		/// </summary>
		/// <param name="args">The key down event data.</param>
		void ResetCamera(KeyDownEventArgs args)
		{
			camera.ResetCamera();
		}
	}
}
