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
	public delegate void OnCameraMove(Vector3 movement, Vector2 rotation, float timeStep);

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

		public Vector3 Position => cameraHolder.WorldPosition;

		public Vector2 PositionXZ => cameraHolder.WorldPosition.XZ2();

		public IEntity Followed { get; private set; }

		public event OnCameraMove OnFixedMove;
		public event OnCameraMove OnFreeFloatMove;

		IMap map;

		/// <summary>
		/// For storing the default camera holder while following unit or other things
		/// </summary>
		Node defaultCameraHolder;
		
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

		const float FreeFloatHeightOffset = 0.2f;
		const float MinZoomDistance = 0.5f;

		public static CameraMover GetCameraController(Node levelNode, IMap map, Vector2 initialPosition) {
			Node cameraHolder = levelNode.CreateChild(name: "CameraHolder");
			Node cameraNode = cameraHolder.CreateChild(name: "camera");
			Camera camera = cameraNode.CreateComponent<Camera>();

			CameraMover mover = cameraNode.CreateComponent<CameraMover>();
			mover.cameraHolder = cameraHolder;
			mover.cameraNode = cameraNode;
			mover.Camera = camera;
			mover.defaultCameraHolder = cameraHolder;
			mover.map = map;

			cameraHolder.Position = new Vector3(initialPosition.X, map.GetHeightAt(initialPosition), initialPosition.Y);

			cameraNode.Position = new Vector3(0, 10, -5);
			cameraNode.LookAt(cameraHolder.WorldPosition, Vector3.UnitY);

			mover.worldDirection = cameraNode.WorldDirection;
			mover.cameraDistance = cameraNode.Position.Length;

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

		public void MoveTo(Vector2 worldPosition)
		{
			StopFollowing();

			//TODO: signal movement

			if (map.IsInside(worldPosition)) {
				cameraHolder.Position = new Vector3(worldPosition.X, map.GetHeightAt(worldPosition), worldPosition.Y);
			}
			else {
				worldPosition = RoundPositionToMap(worldPosition);
				cameraHolder.Position = new Vector3(worldPosition.X, map.GetHeightAt(worldPosition), worldPosition.Y);
			}
			
		}

		public void MoveBy(Vector2 worldDelta)
		{
			//Need to stop following so the cameraHolder is the correct one
			StopFollowing();
			MoveTo(cameraHolder.WorldPosition.XZ2() + worldDelta);
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

				cameraHolder.Position = RoundPositionToMap(cameraNode.WorldPosition, FreeFloatHeightOffset);
				cameraNode.Position = new Vector3(0, 0, 0);
			}
		}

		public void SwitchToFixed() {
			if (FreeFloat) {
				FreeFloat = false;

				cameraHolder.Position = RoundPositionToMap(cameraHolder.Position - fixedPosition);

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

			//Correct height if the terrain changed
			if (!Following && !FreeFloat) {
				cameraHolder.Position = new Vector3(cameraHolder.Position.X, map.GetHeightAt(cameraHolder.Position.XZ2()), cameraHolder.Position.Z);
			}

			worldDirection = cameraNode.WorldDirection;
			cameraDistance = cameraNode.Position.Length;
		}

		protected override void OnDeleted()
		{
			base.OnDeleted();

			Camera.Dispose();
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


			var newPosition = cameraHolder.Position + worldDelta;
			
			if (map.IsInside(newPosition.XZ2())) {
				newPosition.Y = map.GetHeightAt(newPosition.XZ2());
				cameraHolder.Position = newPosition;
			}
			else {
				Vector2 newPositionXZ = RoundPositionToMap(newPosition.XZ2());
				cameraHolder.Position = new Vector3(newPositionXZ.X, map.GetHeightAt(newPositionXZ), newPositionXZ.Y);
			}
		}

		/// <summary>
		/// Moves camera in the Y axis, + is up, - is down
		/// </summary>
		/// <param name="delta">Amount of movement</param>
		void MoveVertical(float delta) {
			var position = cameraNode.Position;
			position.Y += delta / cameraHolder.Scale.Y;

			if (position.Y > 0.5f) {
				cameraNode.Position = position;
				cameraNode.LookAt(cameraHolder.WorldPosition, Vector3.UnitY);
			}
		}

		void Zoom(float delta)
		{
			Vector3 newPosition = cameraNode.Position + Vector3.Divide(Vector3.Normalize(cameraNode.Position) * (-delta), cameraHolder.Scale);

			/*If distance to holder (which is at 0,0,0) is less than min allowed distance
			 or the vector changed quadrant, which means it went through 0,0,0
			 */

			if (newPosition.Length < MinZoomDistance ||
				newPosition.Y < 0) {
				cameraNode.Position = Vector3.Normalize(cameraNode.Position) * MinZoomDistance;
			}
			else {
				cameraNode.Position = newPosition;
			}
		}

		void MoveRelativeToLookingDirection(Vector3 delta) {
			if (delta != Vector3.Zero) {
				delta = cameraNode.WorldRotation * delta;

				cameraHolder.Position = RoundPositionToMap(cameraHolder.Position + delta, FreeFloatHeightOffset);
			}
		}


		void RotateCameraFixed(Vector2 rot) {
			cameraNode.RotateAround(new Vector3(0,0,0), Quaternion.FromAxisAngle(Vector3.UnitY, rot.X), TransformSpace.Parent);


			if ((5 < cameraNode.Rotation.PitchAngle && rot.Y < 0) || (cameraNode.Rotation.PitchAngle < 85 && rot.Y > 0)) {
				cameraNode.RotateAround(new Vector3(0, 0, 0), Quaternion.FromAxisAngle(cameraNode.WorldRight, rot.Y), TransformSpace.Parent);
			}
		}

		void RotateCameraFree(Vector2 rot) {
			cameraNode.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, rot.X),TransformSpace.Parent);
			cameraNode.Rotate(Quaternion.FromAxisAngle(cameraNode.Right, rot.Y),TransformSpace.Parent);
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

		Vector2 RoundPositionToMap(Vector2 position)
		{
			if (position.X < map.Left) {
				position.X = map.Left;
			}
			else if (position.X > map.Left + map.Width) {
				position.X = map.Left + map.Width - 0.01f; ;
			}

			if (position.Y < map.Top) {
				position.Y = map.Top;
			}
			else if (position.Y > map.Top + map.Length) {
				position.Y = map.Top + map.Length - 0.01f;
			}

			return position;
		}

		Vector3 RoundPositionToMap(Vector3 position, float minOffsetHeight = 0, float minOffsetBorder = 0)
		{
			if (position.X < map.Left + minOffsetBorder) {
				position.X = map.Left + minOffsetBorder;
			}
			else if (position.X > map.Left + map.Width - minOffsetBorder) {
				position.X = map.Left + map.Width - 0.01f - minOffsetBorder; 
			}

			if (position.Z < map.Top + minOffsetBorder) {
				position.Z = map.Top + minOffsetBorder;
			}
			else if (position.Z > map.Top + map.Length - minOffsetBorder) {
				position.Z = map.Top + map.Length - 0.01f - minOffsetBorder;
			}

			float height = map.GetHeightAt(position.X, position.Z);
			if (position.Y <= height + minOffsetHeight) {
				position.Y = height + minOffsetHeight;
			}

			return position;
		}

	}
}
