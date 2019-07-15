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
    public class CameraController : ICameraController
    {
		enum CameraMovementType { Fixed, FreeFloat }

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

		public float CameraScrollSensitivity { get; set; }

		public float CameraRotationSensitivity { get; set; }

		public float MouseRotationSensitivity { get; set; }

		public float WheelSensitivity { get; set; }

		public bool MouseBorderCameraMovement { get; set; }

		CameraMovementType cameraType;
		
		readonly GameController input;

		readonly GameUI ui;

		readonly CameraMover camera;

		CameraMovements activeCameraMovement = new CameraMovements();

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

		public void Dispose()
		{
			input.MouseMove -= OnMouseMoved;
			input.MouseWheelMoved -= OnMouseWheel;
			input.EnteredScreenBorder -= OnScreenBorderEntered;
			input.LeftScreenBorder -= OnScreenBorderLeft;
			camera.Dispose();
		}

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


		void OnMouseWheel(MouseWheelEventArgs args)
		{
			if (!ui.UIHovering) {
				camera.AddDecayingZoomChange(args.Wheel * WheelSensitivity);
			}
		}

		void OnMouseMoved(MHUrhoMouseMovedEventArgs args)
		{
			if (cameraType == CameraMovementType.FreeFloat) {
				camera.AddDecayingRotation(new Vector2(args.DX, args.DY) * MouseRotationSensitivity);
			}
		}

		void StartCameraMoveLeft(KeyDownEventArgs args)
		{
			var movement = camera.StaticMovement;
			movement.X -= CameraScrollSensitivity;
			camera.SetStaticMovement(movement);

			activeCameraMovement.MoveLeft = true;
		}

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

		void StartCameraMoveRight(KeyDownEventArgs args)
		{
			var movement = camera.StaticMovement;
			movement.X += CameraScrollSensitivity;
			camera.SetStaticMovement(movement);

			activeCameraMovement.MoveRight = true;
		}

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

		void StartCameraMoveForward(KeyDownEventArgs args)
		{
			var movement = camera.StaticMovement;
			movement.Z += CameraScrollSensitivity;
			camera.SetStaticMovement(movement);

			activeCameraMovement.MoveForward = true;
		}

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

		void StartCameraMoveBackward(KeyDownEventArgs args)
		{
			var movement = camera.StaticMovement;
			movement.Z -= CameraScrollSensitivity;
			camera.SetStaticMovement(movement);

			activeCameraMovement.MoveBackward = true;
		}

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

		void StartCameraRotationRight(KeyDownEventArgs args)
		{
			camera.SetStaticYawChange(camera.StaticYaw + CameraRotationSensitivity);

			activeCameraMovement.RotateRight = true;
		}

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

		void StartCameraRotationLeft(KeyDownEventArgs args)
		{
			camera.SetStaticYawChange(camera.StaticYaw - CameraRotationSensitivity);

			activeCameraMovement.RotateLeft = true;
		}

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

		void StartCameraRotationUp(KeyDownEventArgs args)
		{
			camera.SetStaticPitchChange(camera.StaticPitch + CameraRotationSensitivity);

			activeCameraMovement.RotateUp = true;
		}

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

		void StartCameraRotationDown(KeyDownEventArgs args)
		{
			camera.SetStaticPitchChange(camera.StaticPitch - CameraRotationSensitivity);

			activeCameraMovement.RotateDown = true;
		}

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

		void ResetCamera(KeyDownEventArgs args)
		{
			camera.ResetCamera();
		}
	}
}
