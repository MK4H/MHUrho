using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Input
{
    class FixedCamera : ZoomingRotatingCamera
    {
		public override CameraMode CameraMode => CameraMode.RTS;

		Vector3 storedCameraOffset;
		bool cameraMoved;

		public FixedCamera(IMap map, Node levelNode, Node cameraNode, Vector2 initialPosition, SwitchState switchState)
			: base(map, cameraNode, levelNode.CreateChild("CameraHolder"), switchState)
		{
			CameraHolder.Position = new Vector3(initialPosition.X,
												map.GetTerrainHeightAt(initialPosition),
												initialPosition.Y);
		}

		public override void MoveTo(Vector2 xzPosition)
		{
			Vector2 newPosition = RoundPositionToMap(xzPosition);
			CameraHolder.Position = new Vector3(newPosition.X,
												Map.GetTerrainHeightAt(newPosition),
												newPosition.Y);
			cameraMoved = true;
		}

		public override void MoveTo(Vector3 position)
		{
			MoveTo(position.XZ2());
		}

		public override void MoveBy(Vector2 xzMovement)
		{
			MoveHorizontal(xzMovement.X, xzMovement.Y);
			cameraMoved = true;
		}

		public override void MoveBy(Vector3 movement)
		{
			MoveHorizontal(movement.X, movement.Z);
			MoveVertical(movement.Y);
			cameraMoved = true;
		}

		public override void PreChangesUpdate()
		{
			cameraMoved = false;
			base.PreChangesUpdate();
		}

		public override void PostChangesUpdate()
		{
			CameraHolder.Position = RoundPositionToMap(CameraHolder.Position, false);
			base.PostChangesUpdate();

			if (cameraMoved) {
				OnCameraMove();
			}
		}

		public override void SwitchToThis(CameraState fromState)
		{
			if (fromState == null) {
				CameraNode.ChangeParent(CameraHolder);
				CameraNode.Position = new Vector3(0, 10, -5);
				CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);
				return;
			}

			if (fromState is FixedCamera)
			{
				return;
			}
			else if (fromState is FollowingCamera followingCamera) {
				CameraHolder.Position = followingCamera.Followed.Position;
				SwitchToThisFromZRC(followingCamera);

			}
			else if (fromState is FreeFloatCamera freeFloatCam) {

				CameraHolder.Position = RoundPositionToMap(CameraNode.Position - storedCameraOffset, false);

				//Restore the fixed position relative to holder
				CameraNode.Position = storedCameraOffset;
				//TODO: Check if i need to store rotation
				//cameraNode.Rotation = storedCameraRotation;
				CameraNode.ChangeParent(CameraHolder);
			}
			else {
				throw new ArgumentOutOfRangeException(nameof(fromState), "Unknow camera state");
			}
		}

		public override void SwitchFromThis(CameraState toState)
		{
			storedCameraOffset = CameraNode.Position;
		}

		protected override void SignalCameraMoved()
		{
			cameraMoved = true;
		}

		/// <summary>
		/// Moves camera in the XZ plane, parallel to the ground
		/// X axis is right(+)/ left(-), 
		/// Z axis is in the direction of camera(+)/ in the direction opposite of the camera
		/// Camera clipping to buildings and things is handled every tick in PostChangeUpdate
		/// </summary>
		/// <param name="deltaX">Movement of the camera in left/right direction</param>
		/// <param name="deltaZ">Movement of the camera in forward/backward direction</param>
		void MoveHorizontal(float deltaX, float deltaZ)
		{
			var worldDelta = Vector3.Normalize(CameraNode.WorldDirection.XZ()) * deltaZ +
							Vector3.Normalize(CameraNode.Right.XZ()) * deltaX;


			var newPosition = CameraHolder.Position + worldDelta;

			if (Map.IsInside(newPosition.XZ2())) {
				newPosition.Y = Map.GetHeightAt(newPosition.XZ2());
				CameraHolder.Position = newPosition;
			}
			else {
				Vector2 newPositionXZ = RoundPositionToMap(newPosition.XZ2());
				CameraHolder.Position = new Vector3(newPositionXZ.X, Map.GetTerrainHeightAt(newPositionXZ), newPositionXZ.Y);
			}
		}

		/// <summary>
		/// Moves camera in the Y axis, + is up, - is down
		/// Camera clipping to buildings and things is handled every tick in PostChangeUpdate
		/// </summary>
		/// <param name="delta">Amount of movement</param>
		void MoveVertical(float delta)
		{
			var position = CameraNode.Position;
			position.Y += delta / CameraHolder.Scale.Y;
			CameraNode.Position = position;
		}





	}
}
