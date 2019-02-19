using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.CameraMovement
{
    class FreeFloatCamera : CameraState
    {
		const float MinHeightOffset = 0.2f;

		public override Vector3 CameraWorldPosition => cameraNode.WorldPosition;
		public override Quaternion CameraWorldRotation => cameraNode.WorldRotation;
		public override CameraMode CameraMode => CameraMode.FreeFloating;


		readonly Node levelNode;

		/// <summary>
		/// Node of the camera itself
		/// </summary>
		readonly Node cameraNode;

		public FreeFloatCamera(IMap map, Node levelNode, Node cameraNode, SwitchState switchState)
			:base(map, switchState)
		{
			this.levelNode = levelNode;
			this.cameraNode = cameraNode;
		}

		

		public override void MoveTo(Vector2 xzPosition)
		{
			MoveTo(new Vector3(xzPosition.X, cameraNode.Position.Y, xzPosition.Y));
		}

		public override void MoveTo(Vector3 position)
		{
			cameraNode.Position = RoundPositionToMap(position, true, MinHeightOffset);
			OnCameraMove();
		}

		public override void MoveBy(Vector2 xzMovement)
		{
			Vector3 oldPosition = cameraNode.Position;
			Vector3 newPosition = new Vector3(oldPosition.X + xzMovement.X, oldPosition.Y, oldPosition.Z + xzMovement.Y);
			cameraNode.Position = RoundPositionToMap(newPosition, true, MinHeightOffset);
			OnCameraMove();
		}

		public override void MoveBy(Vector3 movement)
		{
			//Check to prevent division by zero
			if (movement != Vector3.Zero) {
				//Rotate the movement so the camera moves relative to the current looking direction
				movement = cameraNode.WorldRotation * movement;

				cameraNode.Position = RoundPositionToMap(cameraNode.Position + movement, true, MinHeightOffset);
				OnCameraMove();
			}
		}

		public override void Rotate(Vector2 rotation)
		{
			cameraNode.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, rotation.X), TransformSpace.Parent);
			cameraNode.Rotate(Quaternion.FromAxisAngle(cameraNode.Right, rotation.Y), TransformSpace.Parent);
			OnCameraMove();
		}

		public override void Zoom(float zoom)
		{
			//TODO: maybe move forward/backward
		}

		public override void Reset()
		{
			cameraNode.Rotate(Quaternion.FromRotationTo(cameraNode.Direction, new Vector3(0, -1, 1)), TransformSpace.World);
		}

		public override void SwitchToThis(CameraState fromState)
		{
			if (fromState == null) {
				cameraNode.Position = RoundPositionToMap(cameraNode.WorldPosition);
				cameraNode.ChangeParent(levelNode);
				return;
			}

			var fromType = fromState.GetType();
			if (fromType == typeof(FreeFloatCamera)) {
				return;
			}
			else if (fromType == typeof(FixedCamera) || fromType == typeof(EntityFollowingCamera)) {

				cameraNode.Position = RoundPositionToMap(cameraNode.WorldPosition, true, MinHeightOffset);
				cameraNode.Rotation = cameraNode.WorldRotation;
				cameraNode.ChangeParent(levelNode);
			}
			else {
				throw new ArgumentOutOfRangeException(nameof(fromState), "Unknow camera state");
			}
		}





	}
}
