using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Urho;
using Urho.IO;

using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.WorldMap;

namespace MHUrho.CameraMovement
{
	public delegate void OnCameraMoveDelegate(CameraMovedEventArgs args);

	/// <summary>
	/// Implements movement of the camera in the game world.
	/// Has two distinct values for movement, decaying and static.
	/// Static movement changes only by calls to the Set methods.
	/// Decaying movement decays based on the <see cref="Drag"/> and
	/// <see cref="SmoothMovement"/> setting.
	///
	/// Both of these movements are added together to create total movement of the camera.
	/// </summary>
	public class CameraMover : Component {

		/// <summary>
		/// If the movement decaying movement should gradually slow down or it should stop
		/// immediately after applying the movement.
		/// </summary>
		public bool SmoothMovement { get; set; } = true;

		/// <summary>
		/// If the camera is in free floating mode.
		/// Mutually exclusive with <see cref="Following"/>.
		/// </summary>
		public bool FreeFloat { get; private set; } = false;

		/// <summary>
		/// If the camera is following an entity.
		/// If true, the entity is accessible as <see cref="Followed"/>.
		/// </summary>
		public bool Following => Followed != null;

		/// <summary>
		/// How much does the camera slow down per tick.
		/// </summary>
		public float Drag { get; set; } = 2;

		/// <summary>
		/// Current movement applied each tick that does not change without explicit request.
		/// </summary>
		public Vector3 StaticMovement => staticMovement;

		/// <summary>
		/// <see cref="StaticMovement"/> in the XZ plane. Basically a projection.
		/// </summary>
		public Vector2 StaticHorizontalMovement => new Vector2(staticMovement.X, staticMovement.Z);

		/// <summary>
		/// <see cref="StaticMovement"/> in the vertical axis.
		/// </summary>
		public float StaticVerticalMovement => staticMovement.Y;

		/// <summary>
		/// Rotation around the vertical axis that does not change without explicit request.
		/// </summary>
		public float StaticYaw => staticRotation.X;

		/// <summary>
		/// Rotation around the horizontal axis that does not change without explicit request.
		/// </summary>
		public float StaticPitch => staticRotation.Y;

		/// <summary>
		/// The camera component of the UrhoSharp engine.
		/// </summary>
		public Camera Camera { get; private set; }

		/// <summary>
		/// Position of the camera in the game world.
		/// </summary>
		public Vector3 Position => state.CameraWorldPosition;

		/// <summary>
		/// Position of the camera projected into the XZ plane.
		/// </summary>
		public Vector2 PositionXZ => state.CameraWorldPosition.XZ2();

		/// <summary>
		/// If camera is following an entity, holds a reference to the followed entity.
		/// Otherwise is null.
		/// </summary>
		public IEntity Followed => entityFollowingCameraState.Followed;

		/// <summary>
		/// Event invoked on every change of position or rotation of the camera.
		/// </summary>
		public event OnCameraMoveDelegate CameraMoved;

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
		EntityFollowingCamera entityFollowingCameraState;
		FreeFloatCamera freeFloatCameraState;

		/// <summary>
		/// Creates ad initializes the camera and the component for camera movement control.
		/// </summary>
		/// <param name="levelNode">The node representing the level.</param>
		/// <param name="map">Map of the level.</param>
		/// <param name="initialPosition">Initial position of the camera at the start of the game.</param>
		/// <returns>The component responsible for moving the camera.</returns>
		public static CameraMover GetCameraController(Node levelNode, IMap map, Vector2 initialPosition) {
			Node cameraNode = levelNode.CreateChild(name: "CameraNode");
			Camera camera = cameraNode.CreateComponent<Camera>();

			CameraMover mover = cameraNode.CreateComponent<CameraMover>();
			mover.Camera = camera;
			mover.fixedCameraState = new FixedCamera(map, levelNode, cameraNode, initialPosition, mover.SwitchToState);
			mover.entityFollowingCameraState = new EntityFollowingCamera(map, cameraNode, mover.SwitchToState);
			mover.freeFloatCameraState = new FreeFloatCamera(map, levelNode, cameraNode, mover.SwitchToState);

			mover.fixedCameraState.CameraMoved += mover.OnCameraMoved;
			mover.entityFollowingCameraState.CameraMoved += mover.OnCameraMoved;
			mover.freeFloatCameraState.CameraMoved += mover.OnCameraMoved;

			mover.state = mover.fixedCameraState;
			mover.state.SwitchToThis(null);

			return mover;
		}

		/// <summary>
		/// Initializes the CameraMover to receive scene updates.
		/// Needs to be public so it can be created by <see cref="Node.CreateComponent{T}(CreateMode, uint)"/>.
		/// </summary>
		public CameraMover() {
			this.ReceiveSceneUpdates = true;
		}

		/// <summary>
		/// Adds movement along the vertical axis that decays in time, based on
		/// the setting of <see cref="SmoothMovement"/> and <see cref="Drag"/>.
		/// </summary>
		/// <param name="movement">The added movement to the current decaying movement. Additional distance
		/// the camera should move per second.</param>

		public void AddDecayingVerticalMovement(float movement)
		{
			decayingMovement.Y += movement;
		}

		/// <summary>
		/// Sets movement along the vertical axis that is applied each second until
		/// set otherwise.
		/// </summary>
		/// <param name="movement">The distance the camera should move per second.</param>
		public void SetStaticVerticalSpeed(float movement)
		{
			staticMovement.Y = movement;
		}

		/// <summary>
		/// Adds movement along the horizontal plane that decays in time, based on
		/// the setting of <see cref="SmoothMovement"/> and <see cref="Drag"/>.
		/// </summary>
		/// <param name="movement">The change of position of the camera per second.</param>
		public void AddDecayingHorizontalMovement(Vector2 movement)
		{
			StopFollowing();

			decayingMovement.X += movement.X;
			decayingMovement.Z += movement.Y;
		}

		/// <summary>
		/// Sets movement along the horizontal plane that is applied each second until set otherwise.
		/// </summary>
		/// <param name="movement">The change of the position of the camera per second.</param>
		public void SetStaticHorizontalMovement(Vector2 movement)
		{
			staticMovement.X = movement.X;
			staticMovement.Z = movement.Y;
		}

		/// <summary>
		/// Adds movement in the 3D space that decays in time, based on
		/// the setting of <see cref="SmoothMovement"/> and <see cref="Drag"/>.
		/// </summary>
		/// <param name="movement">The change of position of the camera per second added to current movement.</param>
		public void AddDecayingMovement(Vector3 movement)
		{
			decayingMovement += movement;
		}

		/// <summary>
		/// Sets movement along the horizontal plane that is applied each second until set otherwise.
		/// </summary>
		/// <param name="movement">The change of position of the camera per second.</param>
		public void SetStaticMovement(Vector3 movement)
		{
			staticMovement = movement;
		}

		/// <summary>
		/// Adds rotation along the vertical axis that decays in time, based on the
		/// setting of <see cref="SmoothMovement"/> and <see cref="Drag"/>.
		/// </summary>
		/// <param name="yaw">The additional change of rotation per second.</param>
		public void AddDecayingYawChange(float yaw)
		{
			decayingRotation.X += yaw;
		}

		/// <summary>
		/// Sets rotation along the vertical axis that is applied each second until set otherwise.
		/// </summary>
		/// <param name="yaw">The change of rotation per second.</param>
		public void SetStaticYawChange(float yaw)
		{
			staticRotation.X = yaw;
		}

		/// <summary>
		/// Adds rotation along the horizontal axis that decays in time, based on the
		/// setting of <see cref="SmoothMovement"/> and <see cref="Drag"/>.
		/// </summary>
		/// <param name="pitch">The additional change of rotation per second.</param>
		public void AddDecayingPitchChange(float pitch)
		{ 
			decayingRotation.Y += pitch;
		}

		/// <summary>
		/// Sets rotation along the horizontal axis that is applied each second until set otherwise.
		/// </summary>
		/// <param name="pitch">The change of rotation per second.</param>
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

		/// <summary>
		/// Adds the <paramref name="zoom"/> to the current change of zoom per second that changes every tick based on
		/// the setting of <see cref="SmoothMovement"/> and <see cref="Drag"/>.
		/// </summary>
		/// <param name="zoom">The additional change of the zoom per second.</param>
		public void AddDecayingZoomChange(float zoom)
		{
			decayingZoom += zoom;
		}

		/// <summary>
		/// Sets the change of the zoom that is applied every second until set otherwise.
		/// </summary>
		/// <param name="zoom">The constant change in zoom each second.</param>
		public void SetStaticZoomChange(float zoom)
		{
			staticZoom = zoom;
		}

		/// <summary>
		/// Sets the camera position to be at the <paramref name="xzPosition"/> in the XZ plane.
		/// Leaves the height unchanged.
		/// </summary>
		/// <param name="xzPosition">The new position of the camera in the XZ plane.</param>
		public void MoveTo(Vector2 xzPosition)
		{
			state.MoveTo(xzPosition);
		}

		/// <summary>
		/// Sets the camera position to be at the <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The new position of the camera.</param>
		public void MoveTo(Vector3 position)
		{
			state.MoveTo(position);
		}

		/// <summary>
		/// Moves camera by <paramref name="xzDelta"/> in the XZ plane from the current position.
		/// Does not change the height of the camera.
		/// </summary>
		/// <param name="xzDelta">The change of position in the XZ plane.</param>
		public void MoveBy(Vector2 xzDelta)
		{
			state.MoveBy(xzDelta);
		}

		/// <summary>
		/// Moves camera by <paramref name="delta"/> from the current position.
		/// </summary>
		/// <param name="delta">The change of position of the camera.</param>
		public void MoveBy(Vector3 delta)
		{
			state.MoveBy(delta);
		}

		/// <summary>
		/// Stops all movement of the camera, both decaying and static.
		/// </summary>
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

		/// <summary>
		/// Switches camera to entity following mode, in which
		/// the camera moves based on the movement of the followed <paramref name="entity"/>.
		///
		/// Is switched back to Fixed when any attempt at movement (not rotation) is made.
		/// </summary>
		/// <param name="entity"></param>
		public void Follow(IEntity entity)
		{
			StopAllCameraMovement();

			entityFollowingCameraState.SetFollowedEntity(entity);
			if (state != entityFollowingCameraState) {
				SwitchToState(CameraStates.Following);
			}
		}

		/// <summary>
		/// Stops following an entity and switches back to fixed mode.
		/// </summary>
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

		/// <summary>
		/// Resets camera to it's default position in the current mode.
		/// </summary>
		public void ResetCamera()
		{
			StopAllCameraMovement();
			state.Reset();
		}

		/// <summary>
		/// Handles scene update, moves the camera based on the set movement, rotation and zoom, calculates
		/// the movement decay for decaying movement.
		/// </summary>
		/// <param name="timeStep">The time elapsed since the last update.</param>
		protected override void OnUpdate(float timeStep)
		{
			if (IsDeleted || !EnabledEffective)
			{
				return;
			}

			//NOTE: Probably isn't needed, had a problem with 0 timeStep ticks
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

		/// <summary>
		/// Handles the disposal of the component.
		/// </summary>
		protected override void OnDeleted()
		{
			base.OnDeleted();

			Camera.Dispose();
		}

		/// <summary>
		/// Switches state of the camera to the <paramref name="newState"/>
		/// </summary>
		/// <param name="newState">The new state of the camera to switch to.</param>
		void SwitchToState(CameraStates newState)
		{
			CameraState newStateInstance = GetStateInstance(newState);

			state.SwitchFromThis(newStateInstance);
			newStateInstance.SwitchToThis(state);

			state = newStateInstance;
		}
		
		/// <summary>
		/// Gets the state instance corresponding to the <paramref name="newState"/> value.
		/// </summary>
		/// <param name="newState">The state of which we want the implementing instance.</param>
		/// <returns>The instance of the class implementing the behavior of the given state.</returns>
		CameraState GetStateInstance(CameraStates newState)
		{
			switch (newState) {
				case CameraStates.Fixed:
					return fixedCameraState;
				case CameraStates.Following:
					return entityFollowingCameraState;
				case CameraStates.FreeFloat:
					return freeFloatCameraState;
				default:
					throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
			}
		}

		/// <summary>
		/// Safely invokes the event <see cref="CameraMoved"/>.
		/// </summary>
		/// <param name="args">The arguments to invoke the event with.</param>
		void OnCameraMoved(CameraMovedEventArgs args)
		{
			try {
				CameraMoved?.Invoke(args);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(CameraMoved)}: {e.Message}");
			}
		}
	}
}
