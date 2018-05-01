using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Urho;
using Urho.IO;

using MHUrho.Helpers;

namespace MHUrho.Input
{
	public delegate void OnCameraMove(float timeStep);

	public class CameraController : Component {

		public bool SmoothMovement { get; set; } = true;


		public bool FreeFloat { get; private set; } = false;

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
		

		Vector3 decayingMovement;
		Vector3 staticMovement;

		/// <summary>
		/// Only yaw and pitch, no roll
		/// </summary>
		Vector2 decayingRotation;
		Vector2 staticRotation;

		Vector3 fixedPosition;
		Quaternion fixedRotation;

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
			cameraNode.LookAt(cameraHolder.Position, Vector3.UnitY);

			return controller;
		}

		public CameraController() {
			this.ReceiveSceneUpdates = true;
		}


		public void AddVerticalMovement(float movement) {
			decayingMovement.Y += movement;
		}

		public void SetVerticalSpeed(float movement) {
			staticMovement.Y = movement;
		}

		public void SoftResetVerticalSpeed(float movement) {
			if (staticMovement.Y == movement) {
				staticMovement.Y = 0;
			}
		}

		public void HardResetVerticalSpeed() {
			staticMovement.Y = 0;
		}

		public void AddHorizontalMovement(Vector2 movement) {
			decayingMovement.X += movement.X;
			decayingMovement.Z += movement.Y;
		}

		public void SetHorizontalMovement(Vector2 movement) {
			staticMovement.X = movement.X;
			staticMovement.Z = movement.Y;
		}

		public void SoftResetHorizontalMovement(Vector2 movement) {
			if (staticMovement.X == movement.X && staticMovement.Z == movement.Y) {
				staticMovement.X = 0;
				staticMovement.Z = 0;
			}
		}

		public void HardResetHorizontalMovement() {
			staticMovement.X = 0;
			staticMovement.Z = 0;
		}

		public void AddMovement(Vector3 movement) {
			decayingMovement += movement;
		}

		public void SetMovement(Vector3 movement) {
			staticMovement = movement;
		}

		public void SoftResetMovement(Vector3 movement) {
			if (staticMovement == movement) {
				staticMovement = Vector3.Zero;
			}
		}

		public void HardResetMovement() {
			staticMovement = Vector3.Zero;
		}

		public void AddYaw(float yaw) {
			decayingRotation.Y += yaw;
		}

		public void SetYaw(float yaw) {
			staticRotation.Y = yaw;
		}

		public void SoftResetYaw(float yaw) {
			if (staticRotation.Y == yaw) {
				staticRotation.Y = 0;
			}
		}

		public void HardResetYaw() {
			staticRotation.Y = 0;
		}

		public void AddPitch(float pitch) {
			decayingRotation.X += pitch;
		}

		public void SetPitch(float pitch) {
			staticRotation.X = pitch;
		}

		public void SoftResetPitch(float pitch) {
			if (staticRotation.X == pitch) {
				staticRotation.X = 0;
			}
		}

		public void HardResetPitch(float pitch) {
			staticRotation.X = 0;
		}

		public void AddRotation(Vector2 rotation) {
			decayingRotation += rotation;
		}

		public void SetRotation(Vector2 rotation) {
			staticRotation = rotation;
		}

		public void SoftResetRotation(Vector2 rotation) {
			if (staticRotation == rotation) {
				staticRotation = Vector2.Zero;
			}
		}

		public void HardResetRotation() {
			staticRotation = Vector2.Zero;
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
			if (timeStep > 0 && (staticMovement.LengthSquared > NearZero || 
								 decayingMovement.LengthSquared > NearZero || 
								 staticRotation.LengthSquared > NearZero ||
								 decayingRotation.LengthSquared > NearZero)) {

				Vector3 tickMovement = (staticMovement + decayingMovement) * timeStep ;
				Vector2 tickRotation = (staticRotation + decayingRotation) * timeStep ;

				if (FreeFloat) {
					MoveRelativeToLookingDirection(tickMovement);
					RotateCameraFree(tickRotation);
					OnFreeFloatMove?.Invoke(timeStep);
				}
				else {
					MoveHorizontal(tickMovement.X, tickMovement.Z);
					MoveVertical(tickMovement.Y);
					RotateCameraFixed(tickRotation);
					OnFixedMove?.Invoke(timeStep);
				}

				if (SmoothMovement) {
					decayingMovement /= (1 + Drag * timeStep);
					decayingRotation /= (1 + Drag * timeStep);
				}
				else {
					decayingMovement = Vector3.Zero;
					decayingRotation = Vector2.Zero;
				}
			}
		}

		/// <summary>
		/// Moves camera in the XZ plane, parallel to the ground
		/// X axis is right(+)/ left(-), 
		/// Z axis is in the direction of camera(+)/ in the direction opposite of the camera
		/// </summary>
		/// <param name="deltaX">Movement of the camera in left/right direction</param>
		/// <param name="deltaZ">Movement of the camera in forward/backward direction</param>
		void MoveHorizontal(float deltaX, float deltaZ) {
			var delta3D = new Vector3(deltaX, 0, deltaZ);
			var rotation = Quaternion.FromAxisAngle(cameraHolder.Up, cameraHolder.Rotation.YawAngle);

			var worldDelta = rotation * delta3D;
			cameraHolder.Translate(worldDelta,TransformSpace.World);
		}

		/// <summary>
		/// Moves camera in the Y axis, + is up, - is down
		/// </summary>
		/// <param name="delta">Amount of movement</param>
		void MoveVertical(float delta) {
			var position = cameraNode.Position;
			position.Y += delta;
			cameraNode.Position = position;
			cameraNode.LookAt(cameraHolder.Position, Vector3.UnitY);
		}

		void MoveRelativeToLookingDirection(Vector3 delta) {
			if (delta != Vector3.Zero) {
				delta = cameraNode.WorldRotation * delta;
				cameraNode.Translate(new Vector3(0, delta.Y, 0), TransformSpace.Parent);
				cameraHolder.Translate(new Vector3(delta.X, 0, delta.Z), TransformSpace.Parent);
			}
		}

		//TODO: WEIRD vertical rotation, changes with distance from [0,0]
		void RotateCameraFixed(Vector2 rot) {
			cameraHolder.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, rot.Y));

			if ((5 < cameraNode.Rotation.PitchAngle && rot.X < 0) || (cameraNode.Rotation.PitchAngle < 85 && rot.X > 0)) {
				cameraNode.RotateAround(cameraHolder.Position, Quaternion.FromAxisAngle(Vector3.UnitX, rot.X), TransformSpace.Parent);
			}
			
		}

		void RotateCameraFree(Vector2 rot) {
			cameraNode.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, -rot.Y),TransformSpace.Parent);
			cameraNode.Rotate(Quaternion.FromAxisAngle(cameraNode.Right, rot.X),TransformSpace.Parent);
		}

		
	}
}
