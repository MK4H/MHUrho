using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Input
{
    class FollowingCamera : ZoomingRotatingCamera
    {
		public override CameraMode CameraMode => CameraMode.Following;

		public IEntity Followed { get; private set; }

		/// <summary>
		/// Direction of the camera so it faces the same directiong
		/// regardless of the followed entity turning
		/// </summary>
		Vector3 cameraWorldDirection;
		float cameraDistance;

		bool cameraMoved;

		public FollowingCamera(IMap map, Node cameraNode, SwitchState switchState)
			:base(map, cameraNode, null, switchState)
		{

		}

		public override void MoveTo(Vector2 xzPosition)
		{
			SwitchState(CameraStates.Fixed);
		}

		public override void MoveTo(Vector3 position)
		{
			SwitchState(CameraStates.Fixed);
		}

		public override void MoveBy(Vector2 xzMovement)
		{
			SwitchState(CameraStates.Fixed);
		}

		public override void MoveBy(Vector3 movement)
		{
			SwitchState(CameraStates.Fixed);
		}

		public override void PreChangesUpdate()
		{
			CorrectWorldDirection();
			base.PreChangesUpdate();
		}

		public override void PostChangesUpdate()
		{
			base.PostChangesUpdate();
			cameraWorldDirection = CameraNode.WorldDirection;
			cameraDistance = CameraNode.Position.Length;
		

			if (cameraMoved) {
				OnCameraMove();
			}
			cameraMoved = false;
		}

		public override void SwitchToThis(CameraState fromState)
		{
			if (Followed == null) {
				throw new InvalidOperationException("Followed entity was not set before switching to the following camera");
			}

			//If this is the initial camera state
			if (fromState == null) {
				//Some initial offset so it is not mashed up inside the unit
				CameraNode.Position = new Vector3(0, 10, -5);
				
			}

			//Store the current direction so the camera does not rotate when locking to entity
			cameraWorldDirection = CameraNode.WorldDirection;
			/*
			 * calculate cameraNode.Position (relative to parent node) to be the same world offset
			 * regardless of the new entity.Node.Scale
			*/
			CameraNode.Position = Vector3.Divide(CameraNode.Position, Followed.Node.Scale);
			CameraHolder = Followed.Node;
			CameraNode.ChangeParent(CameraHolder);
			cameraDistance = CameraNode.Position.Length;
			

			Followed.RotationChanged += OnFollowedRotationChanged;
			Followed.PositionChanged += OnFollowedPositionChanged;

			CorrectWorldDirection();
		}

		public override void SwitchFromThis(CameraState toState)
		{
			//Normalize the position to 1 scale
			CameraNode.Position = Vector3.Multiply(CameraNode.Position, CameraHolder.Scale);
			ClearFollowed();
		}

		public void SetFollowedEntity(IEntity entity)
		{
			//Actively following
			if (CameraHolder != null) {
				Followed = entity;
				SwitchFromThis(this);
				SwitchToThis(this);
			}
			else {
				ClearFollowed();
				Followed = entity;
			}


		}

		protected override void SignalCameraMoved()
		{
			cameraMoved = true;
		}

		void ClearFollowed()
		{
			if (Followed != null) {
				Followed.RotationChanged -= OnFollowedRotationChanged;
				Followed.PositionChanged -= OnFollowedPositionChanged;
				Followed = null;
				CameraHolder = null;
			}
		}

		void OnFollowedRotationChanged(IEntity entity)
		{
			CorrectWorldDirection();
		}

		void OnFollowedPositionChanged(IEntity entity)
		{
			cameraMoved = true;
		}

		void CorrectWorldDirection()
		{
			Vector3 parentDirection = Quaternion.Invert(CameraHolder.WorldRotation) * cameraWorldDirection;
			CameraNode.Position = -parentDirection * cameraDistance;
			CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);
		}
	}
}
