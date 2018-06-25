﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Urho;
using Urho.IO;

using MHUrho.Helpers;
using MHUrho.Logic;

namespace MHUrho.Input
{
	public delegate void OnCameraMove(Vector3 movement, Vector2 rotation, float timeStep);

	public class CameraController : Component {

		public bool SmoothMovement { get; set; } = true;


		public bool FreeFloat { get; private set; } = false;

		public bool Following => Followed != null;

		/// <summary>
		/// How much does the camera slow down per tick
		/// </summary>
		public float Drag { get; set; } = 2;

		public Vector3 StaticMovement => staticMovement;

		public Vector2 StaticHorizontalMovement => new Vector2(staticMovement.X, staticMovement.Z);

		public float StaticVerticalMovement => staticMovement.Y;

		public float StaticYaw => staticRotation.Y;

		public float StaticPitch => staticRotation.X;

		public Camera Camera { get; private set; }

		public Vector3 CameraPosition => cameraNode.WorldPosition;

		public Vector2 CameraXZPosition => cameraNode.WorldPosition.XZ2();

		public IEntity Followed { get; private set; }

		public event OnCameraMove OnFixedMove;
		public event OnCameraMove OnFreeFloatMove;

		/// <summary>
		/// For storing the default camera holder while following unit or other things
		/// </summary>
		Node defaultCameraHolder;

		Vector3 storedFixedOffset;
		
		/// <summary>
		/// Point on the ground
		/// Camera follows this point at constant offset while not in FreeFloat mode
		/// </summary>
		Node cameraHolder;
		/// <summary>
		/// Node of the camera itself
		/// </summary>
		Node cameraNode;

		float decayingZoom;
		float staticZoom;

		Vector3 decayingMovement;
		Vector3 staticMovement;

		/// <summary>
		/// Only yaw and pitch, no roll
		/// </summary>
		Vector2 decayingRotation;
		Vector2 staticRotation;

		Vector3 fixedPosition;
		Quaternion fixedRotation;

		Vector3 worldDirection;
		float cameraDistance;

		const float NearZero = 0.001f;

		public static CameraController GetCameraController(Scene scene) {
			Node cameraHolder = scene.CreateChild(name: "CameraHolder");
			Node cameraNode = cameraHolder.CreateChild(name: "camera");
			Camera camera = cameraNode.CreateComponent<Camera>();

			CameraController controller = cameraNode.CreateComponent<CameraController>();
			controller.cameraHolder = cameraHolder;
			controller.cameraNode = cameraNode;
			controller.Camera = camera;
			controller.defaultCameraHolder = cameraHolder;
			

			cameraNode.Position = new Vector3(0, 10, -5);
			cameraNode.LookAt(cameraHolder.WorldPosition, Vector3.UnitY);

			controller.worldDirection = cameraNode.WorldDirection;
			controller.cameraDistance = cameraNode.Position.Length;

			return controller;
		}

		public CameraController() {
			this.ReceiveSceneUpdates = true;
		}


		public void AddVerticalMovement(float movement)
		{
			decayingMovement.Y += movement;
		}

		public void SetVerticalSpeed(float movement)
		{
			staticMovement.Y = movement;
		}

		public void SoftResetVerticalSpeed(float movement)
		{
			if (staticMovement.Y == movement) {
				staticMovement.Y = 0;
			}
		}

		public void HardResetVerticalSpeed()
		{
			staticMovement.Y = 0;
		}

		public void AddHorizontalMovement(Vector2 movement)
		{
			StopFollowing();

			decayingMovement.X += movement.X;
			decayingMovement.Z += movement.Y;
		}

		public void SetHorizontalMovement(Vector2 movement)
		{
			staticMovement.X = movement.X;
			staticMovement.Z = movement.Y;
		}

		public void SoftResetHorizontalMovement(Vector2 movement)
		{
			if (staticMovement.X == movement.X && staticMovement.Z == movement.Y) {
				staticMovement.X = 0;
				staticMovement.Z = 0;
			}
		}

		public void HardResetHorizontalMovement()
		{
			staticMovement.X = 0;
			staticMovement.Z = 0;
		}

		public void AddMovement(Vector3 movement)
		{
			decayingMovement += movement;
		}

		public void SetMovement(Vector3 movement)
		{
			staticMovement = movement;
		}

		public void SoftResetMovement(Vector3 movement)
		{
			if (staticMovement == movement) {
				staticMovement = Vector3.Zero;
			}
		}

		public void HardResetMovement()
		{
			staticMovement = Vector3.Zero;
		}

		public void AddYaw(float yaw)
		{
			decayingRotation.Y += yaw;
		}

		public void SetYaw(float yaw)
		{
			staticRotation.Y = yaw;
		}

		public void SoftResetYaw(float yaw)
		{
			if (staticRotation.Y == yaw) {
				staticRotation.Y = 0;
			}
		}

		public void HardResetYaw()
		{
			staticRotation.Y = 0;
		}

		public void AddPitch(float pitch)
		{ 
			decayingRotation.X += pitch;
		}

		public void SetPitch(float pitch)
		{
			staticRotation.X = pitch;
		}

		public void SoftResetPitch(float pitch)
		{
			if (staticRotation.X == pitch) {
				staticRotation.X = 0;
			}
		}

		public void HardResetPitch(float pitch)
		{
			staticRotation.X = 0;
		}

		public void AddRotation(Vector2 rotation)
		{
			decayingRotation += rotation;
		}

		public void SetRotation(Vector2 rotation)
		{
			staticRotation = rotation;
		}

		public void SoftResetRotation(Vector2 rotation)
		{
			if (staticRotation == rotation) {
				staticRotation = Vector2.Zero;
			}
		}

		public void HardResetRotation()
		{
			staticRotation = Vector2.Zero;
		}

		public void AddZoom(float zoom)
		{
			decayingZoom += zoom;
		}

		public void SetZoom(float zoom)
		{
			staticZoom = zoom;
		}

		public void SoftResetZoom(float zoom)
		{
			if (staticZoom == zoom) {
				staticZoom = 0;
			}
		}

		public void HardResetZoom()
		{
			staticZoom = 0;
		}

		public void StopAllCameraMovement()
		{
			staticMovement = Vector3.Zero;
			staticRotation = Vector2.Zero;
			decayingMovement = Vector3.Zero;
			decayingRotation = Vector2.Zero;
		}

		public void SwitchToFree() {
			if (!FreeFloat) {
				FreeFloat = true;
				//Save the fixed position relative to holder
				fixedPosition = cameraNode.Position;
				fixedRotation = cameraNode.Rotation;

				cameraHolder.Position = cameraNode.WorldPosition.XZ();
				cameraNode.Position = new Vector3(0,cameraNode.Position.Y, 0);
			}
		}

		public void SwitchToFixed() {
			if (FreeFloat) {
				FreeFloat = false;
				//Restore the fixed position relative to holder
				cameraNode.Position = fixedPosition;
				cameraNode.Rotation = fixedRotation;
			}
		}

		public void Follow(IEntity entity)
		{
			StopAllCameraMovement();

			Followed = entity;

			/*
			 * calculate cameraNode.Position (relative to parent node) to be the same world offset
			 * regardless of the new entity.Node.Scale
			*/
			cameraNode.Position = Vector3.Multiply(cameraNode.Position, 
													Vector3.Divide(cameraHolder.Scale,
																	entity.Node.Scale));

			cameraDistance = cameraNode.Position.Length;

			cameraHolder = entity.Node;
			cameraNode.ChangeParent(cameraHolder);

			CorrectWorldDirection();
			Followed.RotationChanged += OnFollowedRotationChanged;
		}

		public void StopFollowing()
		{
			if (!Following) return;

			Followed.RotationChanged -= OnFollowedRotationChanged;
			Followed = null;

			cameraNode.Position = Vector3.Multiply(cameraNode.Position,
													Vector3.Divide(cameraHolder.Scale,
																	defaultCameraHolder.Scale));

			cameraDistance = cameraNode.Position.Length;
			defaultCameraHolder.Position = cameraHolder.WorldPosition.XZ();
			cameraHolder = defaultCameraHolder;

			cameraNode.ChangeParent(cameraHolder);

			CorrectWorldDirection();
		}

		/// <summary>
		/// Gets a point pointed at by touch or mouse (represented as normalized screen coords) <paramref name="normalizedScreenPos"/> 
		/// in the vertical plane perpendicular to camera direction in XZ
		/// </summary>
		/// <param name="point">World point in the desired plane</param>
		/// <param name="normalizedScreenPos">Normalized screen position of the input</param>
		/// <returns>Point in the desired plane pointed at by the input</returns>
		public Vector3 GetPointUnderInput(Vector3 point, Vector2 normalizedScreenPos) {
			Plane plane = new Plane(cameraNode.Direction.XZ(), point);

			var cameraRay = Camera.GetScreenRay(normalizedScreenPos.X, normalizedScreenPos.Y);
			var hitDist = cameraRay.HitDistance(plane);

			var result = cameraRay.Origin + cameraRay.Direction * hitDist;

			Debug.Assert(FloatHelpers.FloatsEqual(result.X, point.X) && FloatHelpers.FloatsEqual(result.Z, point.Z));

			return result;
		}

		protected override void OnUpdate(float timeStep) {
			if (Following) {
				CorrectWorldDirection();
			}

			bool movement = false;
			if (timeStep > 0 && ((movement = staticMovement.LengthSquared > NearZero || decayingMovement.LengthSquared > NearZero) || 
								 staticRotation.LengthSquared > NearZero ||
								 decayingRotation.LengthSquared > NearZero ||
								!FloatHelpers.FloatsEqual(staticZoom,0,NearZero) ||
								!FloatHelpers.FloatsEqual(decayingZoom,0,NearZero))) {
				
				if (movement) {
					StopFollowing();
				}

				Vector3 tickMovement = (staticMovement + decayingMovement) * timeStep;
				Vector2 tickRotation = (staticRotation + decayingRotation) * timeStep ;
				float tickZoom = (staticZoom + decayingZoom) * timeStep;
				//Log.Write(LogLevel.Debug, $"StaticMovement: {staticMovement}, Static rotation: {staticRotation}");


				if (FreeFloat) {
					if (movement) {
						MoveRelativeToLookingDirection(tickMovement);
					}
					RotateCameraFree(tickRotation);
					OnFreeFloatMove?.Invoke(tickMovement, tickRotation, timeStep);
				}
				else {
					if (movement) {
						MoveHorizontal(tickMovement.X, tickMovement.Z);
						MoveVertical(tickMovement.Y);
					}
					RotateCameraFixed(tickRotation);
					Zoom(tickZoom);
					OnFixedMove?.Invoke(tickMovement, tickRotation, timeStep);
				}

				if (SmoothMovement) {
					decayingMovement /= (1 + Drag * timeStep);
					decayingRotation /= (1 + Drag * timeStep);
					decayingZoom /= (1 + Drag * timeStep);
				}
				else {
					decayingMovement = Vector3.Zero;
					decayingRotation = Vector2.Zero;
					decayingZoom = 0;
				}
			}

			worldDirection = cameraNode.WorldDirection;
			cameraDistance = cameraNode.Position.Length;
		}

		/// <summary>
		/// Moves camera in the XZ plane, parallel to the ground
		/// X axis is right(+)/ left(-), 
		/// Z axis is in the direction of camera(+)/ in the direction opposite of the camera
		/// </summary>
		/// <param name="deltaX">Movement of the camera in left/right direction</param>
		/// <param name="deltaZ">Movement of the camera in forward/backward direction</param>
		void MoveHorizontal(float deltaX, float deltaZ)
		{
			var worldDelta = Vector3.Normalize(cameraNode.WorldDirection.XZ()) * deltaZ + 
							Vector3.Normalize(cameraNode.Right.XZ()) * deltaX;

			cameraHolder.Translate(worldDelta,TransformSpace.World);
		}

		/// <summary>
		/// Moves camera in the Y axis, + is up, - is down
		/// </summary>
		/// <param name="delta">Amount of movement</param>
		void MoveVertical(float delta) {
			var position = cameraNode.Position;
			position.Y += delta / cameraHolder.Scale.Y;
			cameraNode.Position = position;
			cameraNode.LookAt(cameraHolder.WorldPosition, Vector3.UnitY);
			//Log.Write(LogLevel.Debug, $"Camera position: {cameraNode.WorldPosition}");
		}

		void Zoom(float delta)
		{
			cameraNode.Position += Vector3.Divide(Vector3.Normalize(cameraNode.Position) * delta, cameraHolder.Scale);
		}

		void MoveRelativeToLookingDirection(Vector3 delta) {
			if (delta != Vector3.Zero) {
				delta = cameraNode.WorldRotation * delta;
				cameraNode.Translate(new Vector3(0, delta.Y, 0), TransformSpace.Parent);
				cameraHolder.Translate(new Vector3(delta.X, 0, delta.Z), TransformSpace.Parent);
			}
		}


		void RotateCameraFixed(Vector2 rot) {
			rot = -rot;
			cameraNode.RotateAround(cameraHolder.WorldPosition, Quaternion.FromAxisAngle(Vector3.UnitY, rot.Y), TransformSpace.World);
			
			if ((5 < cameraNode.Rotation.PitchAngle && rot.X < 0) || (cameraNode.Rotation.PitchAngle < 85 && rot.X > 0)) {
				cameraNode.RotateAround(cameraHolder.WorldPosition, Quaternion.FromAxisAngle(cameraNode.WorldRight, rot.X), TransformSpace.World);
			}
			
		}

		void RotateCameraFree(Vector2 rot) {
			cameraNode.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, -rot.Y),TransformSpace.Parent);
			cameraNode.Rotate(Quaternion.FromAxisAngle(cameraNode.Right, rot.X),TransformSpace.Parent);
		}

		void CorrectWorldDirection()
		{
			Vector3 parentDirection = Quaternion.Invert(cameraHolder.WorldRotation) * worldDirection;
			cameraNode.Position = -parentDirection * cameraDistance;
			cameraNode.LookAt(cameraHolder.WorldPosition, Vector3.UnitY);
		}

		void OnFollowedRotationChanged(IEntity entity)
		{
			CorrectWorldDirection();
		}

	}
}
