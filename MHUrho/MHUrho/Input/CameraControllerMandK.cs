using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.UserInterface;
using MHUrho.Logic;
using Urho;
using Urho.Gui;

namespace MHUrho.Input
{
    public class CameraControllerMandK : ICameraController
    {
		enum CameraMovementType { Fixed, FreeFloat }

		public float CameraScrollSensitivity { get; set; }

		public float CameraRotationSensitivity { get; set; }

		public float MouseRotationSensitivity { get; set; }

		public float WheelSensitivity { get; set; }

		public bool MouseBorderCameraMovement { get; set; }

		CameraMovementType cameraType;
		
		readonly GameMandKController input;

		readonly MandKGameUI ui;

		readonly CameraMover camera;

		public CameraControllerMandK(GameMandKController input, MandKGameUI ui, CameraMover cameraMover)
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

		//TODO: Read from config
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
					break;
				case ScreenBorder.Bottom:
					horizontalMovement.Y -= CameraScrollSensitivity;
					break;
				case ScreenBorder.Left:
					horizontalMovement.X -= CameraScrollSensitivity;
					break;
				case ScreenBorder.Right:
					horizontalMovement.X += CameraScrollSensitivity;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(border), border, null);
			}

			camera.SetStaticHorizontalMovement(horizontalMovement);
		}


		void OnScreenBorderLeft(ScreenBorder border)
		{
			if (!MouseBorderCameraMovement) return;

			Vector2 horizontalMovement = camera.StaticHorizontalMovement;
			switch (border) {
				case ScreenBorder.Top:
					horizontalMovement.Y -= CameraScrollSensitivity;
					if (FloatHelpers.FloatsEqual(horizontalMovement.Y, 0)) {
						horizontalMovement.Y = 0;
					}
					break;
				case ScreenBorder.Bottom:
					horizontalMovement.Y += CameraScrollSensitivity;
					if (FloatHelpers.FloatsEqual(horizontalMovement.Y, 0)) {
						horizontalMovement.Y = 0;
					}
					break;
				case ScreenBorder.Left:
					horizontalMovement.X += CameraScrollSensitivity;
					if (FloatHelpers.FloatsEqual(horizontalMovement.X, 0)) {
						horizontalMovement.X = 0;
					}
					break;
				case ScreenBorder.Right:
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


		void OnMouseWheel(MouseWheelEventArgs e)
		{
			if (!ui.UIHovering) {
				camera.AddDecayingZoomChange(e.Wheel * WheelSensitivity);
			}
		}

		void OnMouseMoved(MHUrhoMouseMovedEventArgs e)
		{
			if (cameraType == CameraMovementType.FreeFloat) {
				camera.AddDecayingRotation(new Vector2(e.DX, e.DY) * MouseRotationSensitivity);
			}
		}

		void StartCameraMoveLeft(KeyDownEventArgs e)
		{
			var movement = camera.StaticMovement;
			movement.X -= CameraScrollSensitivity;
			camera.SetStaticMovement(movement);
		}

		void StopCameraMoveLeft(KeyUpEventArgs e)
		{
			var movement = camera.StaticMovement;
			movement.X += CameraScrollSensitivity;
			if (FloatHelpers.FloatsEqual(movement.X, 0)) {
				movement.X = 0;
			}
			camera.SetStaticMovement(movement);
		}

		void StartCameraMoveRight(KeyDownEventArgs e)
		{
			var movement = camera.StaticMovement;
			movement.X += CameraScrollSensitivity;
			camera.SetStaticMovement(movement);
		}

		void StopCameraMoveRight(KeyUpEventArgs e)
		{
			var movement = camera.StaticMovement;
			movement.X -= CameraScrollSensitivity;
			if (FloatHelpers.FloatsEqual(movement.X, 0)) {
				movement.X = 0;
			}
			camera.SetStaticMovement(movement);
		}

		void StartCameraMoveForward(KeyDownEventArgs e)
		{
			var movement = camera.StaticMovement;
			movement.Z += CameraScrollSensitivity;
			camera.SetStaticMovement(movement);
		}

		void StopCameraMoveForward(KeyUpEventArgs e)
		{
			var movement = camera.StaticMovement;
			movement.Z -= CameraScrollSensitivity;
			if (FloatHelpers.FloatsEqual(movement.Z, 0)) {
				movement.Z = 0;
			}
			camera.SetStaticMovement(movement);
		}

		void StartCameraMoveBackward(KeyDownEventArgs e)
		{
			var movement = camera.StaticMovement;
			movement.Z -= CameraScrollSensitivity;
			camera.SetStaticMovement(movement);
		}

		void StopCameraMoveBackward(KeyUpEventArgs e)
		{
			var movement = camera.StaticMovement;
			movement.Z += CameraScrollSensitivity;
			if (FloatHelpers.FloatsEqual(movement.Z, 0)) {
				movement.Z = 0;
			}
			camera.SetStaticMovement(movement);
		}

		void StartCameraRotationRight(KeyDownEventArgs e)
		{
			camera.SetStaticYawChange(camera.StaticYaw + CameraRotationSensitivity);
		}

		void StopCameraRotationRight(KeyUpEventArgs e)
		{
			var yaw = camera.StaticYaw - CameraRotationSensitivity;
			if (FloatHelpers.FloatsEqual(yaw, 0)) {
				yaw = 0;
			}
			camera.SetStaticYawChange(yaw);
		}

		void StartCameraRotationLeft(KeyDownEventArgs e)
		{
			camera.SetStaticYawChange(camera.StaticYaw - CameraRotationSensitivity);
		}

		void StopCameraRotationLeft(KeyUpEventArgs e)
		{
			var yaw = camera.StaticYaw + CameraRotationSensitivity;
			if (FloatHelpers.FloatsEqual(yaw, 0)) {
				yaw = 0;
			}
			camera.SetStaticYawChange(yaw);
		}

		void StartCameraRotationUp(KeyDownEventArgs e)
		{
			camera.SetStaticPitchChange(camera.StaticPitch + CameraRotationSensitivity);
		}

		void StopCameraRotationUp(KeyUpEventArgs e)
		{
			var pitch = camera.StaticPitch - CameraRotationSensitivity;
			if (FloatHelpers.FloatsEqual(pitch, 0)) {
				pitch = 0;
			}
			camera.SetStaticPitchChange(pitch);
		}

		void StartCameraRotationDown(KeyDownEventArgs e)
		{
			camera.SetStaticPitchChange(camera.StaticPitch - CameraRotationSensitivity);
		}

		void StopCameraRotationDown(KeyUpEventArgs e)
		{
			var pitch = camera.StaticPitch + CameraRotationSensitivity;
			if (FloatHelpers.FloatsEqual(pitch, 0)) {
				pitch = 0;
			}
			camera.SetStaticPitchChange(pitch);
		}

		void CameraSwitchMode(KeyDownEventArgs e)
		{
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
			}
		}

		public void Dispose()
		{
			camera.Dispose();
		}
	}
}
