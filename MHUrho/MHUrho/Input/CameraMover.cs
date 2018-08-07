using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Urho;
using Urho.IO;

using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.WorldMap;

namespace MHUrho.Input
{
	public delegate void OnCameraMove(CameraMovedEventArgs args);

	public class CameraMover : Component {

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

		public float StaticYaw => staticRotation.X;

		public float StaticPitch => staticRotation.Y;

		public Camera Camera { get; private set; }

		public Vector3 Position => state.CameraWorldPosition;

		public Vector2 PositionXZ => state.CameraWorldPosition.XZ2();

		public IEntity Followed => followingCameraState.Followed;

		public event OnCameraMove CameraMoved;

		float decayingZoom;
		float staticZoom;

		Vector3 decayingMovement;
		Vector3 staticMovement;

		/// <summary>
		/// Only yaw and pitch, no roll
		/// </summary>
		Vector2 decayingRotation;
		Vector2 staticRotation;

		CameraState state;


		const float NearZero = 0.001f;


		FixedCamera fixedCameraState;
		FollowingCamera followingCameraState;
		FreeFloatCamera freeFloatCameraState;

		public static CameraMover GetCameraController(Node levelNode, IMap map, Vector2 initialPosition) {
			Node cameraNode = levelNode.CreateChild(name: "CameraNode");
			Camera camera = cameraNode.CreateComponent<Camera>();

			CameraMover mover = cameraNode.CreateComponent<CameraMover>();
			mover.Camera = camera;
			mover.fixedCameraState = new FixedCamera(map, levelNode, cameraNode, initialPosition, mover.SwitchToState);
			mover.followingCameraState = new FollowingCamera(map, cameraNode, mover.SwitchToState);
			mover.freeFloatCameraState = new FreeFloatCamera(map, levelNode, cameraNode, mover.SwitchToState);

			mover.fixedCameraState.CameraMoved += mover.OnCameraMoved;
			mover.followingCameraState.CameraMoved += mover.OnCameraMoved;
			mover.freeFloatCameraState.CameraMoved += mover.OnCameraMoved;

			mover.state = mover.fixedCameraState;
			mover.state.SwitchToThis(null);

			return mover;
		}

		public CameraMover() {
			this.ReceiveSceneUpdates = true;
		}


		public void AddDecayingVerticalMovement(float movement)
		{
			decayingMovement.Y += movement;
		}

		public void SetStaticVerticalSpeed(float movement)
		{
			staticMovement.Y = movement;
		}

		public void AddDecayingHorizontalMovement(Vector2 movement)
		{
			StopFollowing();

			decayingMovement.X += movement.X;
			decayingMovement.Z += movement.Y;
		}

		public void SetStaticHorizontalMovement(Vector2 movement)
		{
			staticMovement.X = movement.X;
			staticMovement.Z = movement.Y;
		}

		public void AddDecayingMovement(Vector3 movement)
		{
			decayingMovement += movement;
		}

		public void SetStaticMovement(Vector3 movement)
		{
			staticMovement = movement;
		}

		public void AddStaticYawChange(float yaw)
		{
			decayingRotation.X += yaw;
		}

		public void SetStaticYawChange(float yaw)
		{
			staticRotation.X = yaw;
		}

		public void AddDecayingPitchChange(float pitch)
		{ 
			decayingRotation.Y += pitch;
		}

		public void SetStaticPitchChange(float pitch)
		{
			staticRotation.Y = pitch;
		}

		/// <summary>
		/// Adds <paramref name="rotation"/> to decaying rotation, which decays with (is divided by) <see cref="Drag"/> every tick
		/// </summary>
		/// <param name="rotation">The initial rotation of the camera, <see cref="Vector2.X"/> is yaw, <see cref="Vector2.Y"/> is pitch</param>
		public void AddDecayingRotation(Vector2 rotation)
		{
			decayingRotation += rotation;
		}

		/// <summary>
		/// Sets the static rotation of the camera, which will rotate every tick based on <see cref="staticRotation"/> + <see cref="decayingRotation"/>
		/// Has the same value until set otherwise
		/// </summary>
		/// <param name="rotation">The rotation of the camera, <see cref="Vector2.X"/> is yaw, <see cref="Vector2.Y"/> is pitch</param>
		public void SetStaticRotation(Vector2 rotation)
		{
			staticRotation = rotation;
		}

		public void AddDecayingZoomChange(float zoom)
		{
			decayingZoom += zoom;
		}

		public void SetStaticZoomChange(float zoom)
		{
			staticZoom = zoom;
		}

		public void MoveTo(Vector2 xzPosition)
		{
			state.MoveTo(xzPosition);
		}

		public void MoveTo(Vector3 position)
		{
			state.MoveTo(position);
		}

		public void MoveBy(Vector2 xzDelta)
		{
			state.MoveBy(xzDelta);


		}

		public void MoveBy(Vector3 delta)
		{
			state.MoveBy(delta);
		}

		public void StopAllCameraMovement()
		{
			staticMovement = Vector3.Zero;
			staticRotation = Vector2.Zero;
			decayingMovement = Vector3.Zero;
			decayingRotation = Vector2.Zero;
		}

		/// <summary>
		/// Switches camera to free mode, freely flying above the terrain
		/// Stops all camera movement at the time of the switch
		/// </summary>
		public void SwitchToFree() {
			StopAllCameraMovement();

			SwitchToState(CameraStates.FreeFloat);
		}

		/// <summary>
		/// Switches camera to fixed mode, following the terrain.
		/// Typical RTS camera
		/// Stops all camera movement at the time of the switch
		/// </summary>
		public void SwitchToFixed() {
			StopAllCameraMovement();

			SwitchToState(CameraStates.Fixed);
		}

		public void Follow(IEntity entity)
		{
			StopAllCameraMovement();

			followingCameraState.SetFollowedEntity(entity);
			SwitchToState(CameraStates.Following);
		}

		public void StopFollowing()
		{
			SwitchToState(CameraStates.Fixed);
		}

		/// <summary>
		/// Gets a point pointed at by touch or mouse (represented as normalized screen coords) <paramref name="normalizedScreenPos"/> 
		/// in the vertical plane perpendicular to camera direction in XZ
		/// </summary>
		/// <param name="point">World point in the desired plane</param>
		/// <param name="normalizedScreenPos">Normalized screen position of the input</param>
		/// <returns>Point in the desired plane pointed at by the input</returns>
		public Vector3 GetPointUnderInput(Vector3 point, Vector2 normalizedScreenPos) {
			Plane plane = new Plane(Camera.Node.Direction.XZ(), point);

			var cameraRay = Camera.GetScreenRay(normalizedScreenPos.X, normalizedScreenPos.Y);
			var hitDist = cameraRay.HitDistance(plane);

			var result = cameraRay.Origin + cameraRay.Direction * hitDist;

			Debug.Assert(FloatHelpers.FloatsEqual(result.X, point.X) && FloatHelpers.FloatsEqual(result.Z, point.Z));

			return result;
		}


		protected override void OnUpdate(float timeStep)
		{
			//TODO: Probably isnt needed, had a problem with 0 timeStep ticks
			if (timeStep <= 0) {
				return;
			}

			state.PreChangesUpdate();

			if (staticMovement.LengthSquared > NearZero || decayingMovement.LengthSquared > NearZero) {

				state.MoveBy((staticMovement + decayingMovement) * timeStep);
			}

			if (staticRotation.LengthSquared > NearZero ||
				decayingRotation.LengthSquared > NearZero) {

				state.Rotate((staticRotation + decayingRotation) * timeStep);
			}

			if (!FloatHelpers.FloatsEqual(staticZoom, 0, NearZero) ||
				!FloatHelpers.FloatsEqual(decayingZoom, 0, NearZero)) {

				state.Zoom((staticZoom + decayingZoom) * timeStep);
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

			state.PostChangesUpdate();
		}

		protected override void OnDeleted()
		{
			base.OnDeleted();

			Camera.Dispose();
		}

		void SwitchToState(CameraStates newState)
		{
			CameraState newStateInstance = GetStateInstance(newState);

			state.SwitchFromThis(newStateInstance);
			newStateInstance.SwitchToThis(state);

			state = newStateInstance;
		}

		CameraState GetStateInstance(CameraStates newState)
		{
			switch (newState) {
				case CameraStates.Fixed:
					return fixedCameraState;
				case CameraStates.Following:
					return followingCameraState;
				case CameraStates.FreeFloat:
					return freeFloatCameraState;
				default:
					throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
			}
		}

		void OnCameraMoved(CameraMovedEventArgs args)
		{
			CameraMoved?.Invoke(args);
		}
	}
}
