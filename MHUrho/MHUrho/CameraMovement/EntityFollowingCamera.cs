using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.CameraMovement
{
    class EntityFollowingCamera : PointFollowingCamera
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

		public EntityFollowingCamera(IMap map, Node cameraNode, SwitchState switchState)
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

		public override void Reset()
		{
			/*
			 * calculate cameraNode.Position (relative to parent node) to be the same world offset
			 * regardless of the new entity.Node.Scale
			 *
			 * Give camera some offset from the followed entity
			*/
			CameraNode.Position = Vector3.Divide(new Vector3(0, 10, -5), Followed.Node.Scale);
			CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);
			cameraDistance = CameraNode.Position.Length;
			cameraWorldDirection = CameraNode.WorldDirection;
			WantedCameraVerticalOffset = 10;
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
				Reset();
			}

			//Store the current direction so the camera does not rotate when locking to entity
			cameraWorldDirection = CameraNode.WorldDirection;

			CameraHolder = Followed.Node;
			if (fromState is PointFollowingCamera pfCamera) {
				SwitchToThisFromPFC(pfCamera);
			}
			else {
				/*
				 * calculate cameraNode.Position (relative to parent node) to be the same world offset
				 * regardless of the new entity.Node.Scale
				 *
				 * Give camera some offset from the followed entity
				*/
				CameraNode.Position = Vector3.Divide(new Vector3(0, 10, -5), Followed.Node.Scale);
				WantedCameraVerticalOffset = 10;
				CameraNode.ChangeParent(CameraHolder);
			}
		
			cameraDistance = CameraNode.Position.Length;
			

			Followed.RotationChanged += OnFollowedRotationChanged;
			Followed.PositionChanged += OnFollowedPositionChanged;

			CorrectWorldDirection();
		}

		public override void SwitchFromThis(CameraState toState)
		{
			ClearFollowed();			
		}

		public void SetFollowedEntity(IEntity entity)
		{
			//Actively following
			if (CameraHolder != null) {
				SwitchFromThis(this);
				Followed = entity;
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
			Vector3 localDirection = Vector3.Normalize(CameraHolder.WorldToLocal(CameraHolder.WorldPosition + cameraWorldDirection));
			//Urho.IO.Log.Write(LogLevel.Debug, $"LocalDirection: {localDirection}");
			CameraNode.Position = -localDirection * cameraDistance;
			//TODO: Check retun value
			CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);
		}
	}
}
