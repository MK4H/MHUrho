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

		/// <inheritdoc />
		public override CameraMode CameraMode => CameraMode.Following;

		/// <summary>
		/// The entity being followed by the camera.
		/// </summary>
		public IEntity Followed { get; private set; }

		/// <summary>
		/// Direction of the camera so it faces the same directiong
		/// regardless of the followed entity turning
		/// </summary>
		Vector3 cameraWorldDirection;
		float cameraDistance;

		bool cameraMoved;

		public EntityFollowingCamera(IMap map, Node cameraNode, StateSwitchedDelegate stateSwitched)
			:base(map, cameraNode, null, stateSwitched)
		{

		}

		/// <inheritdoc />
		/// <summary>
		/// Switches to Fixed camera mode, manual movement of the camera
		/// is not allowed when following an entity.
		/// </summary>
		public override void MoveTo(Vector2 xzPosition)
		{
			StateSwitched(CameraStates.Fixed);
		}

		/// <inheritdoc />
		/// <summary>
		/// Switches to Fixed camera mode, manual movement of the camera
		/// is not allowed when following an entity.
		/// </summary>
		public override void MoveTo(Vector3 position)
		{
			StateSwitched(CameraStates.Fixed);
		}

		/// <inheritdoc />
		/// /// <summary>
		/// Switches to Fixed camera mode, manual movement of the camera
		/// is not allowed when following an entity.
		/// </summary>
		public override void MoveBy(Vector2 xzMovement)
		{
			StateSwitched(CameraStates.Fixed);
		}

		/// <inheritdoc />
		/// /// <summary>
		/// Switches to Fixed camera mode, manual movement of the camera
		/// is not allowed when following an entity.
		/// </summary>
		public override void MoveBy(Vector3 movement)
		{
			StateSwitched(CameraStates.Fixed);
		}

		/// <inheritdoc />
		/// <summary>
		/// Resets the camera to default position relative to the followed entity.
		/// </summary>
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

		/// <summary>
		/// Corrects the direction, canceling any movement of the followed entity.
		/// </summary>
		public override void PreChangesUpdate()
		{
			CorrectWorldDirection();
			base.PreChangesUpdate();
		}

		/// <summary>
		/// Stores the wanted camera direction and position, in case the followed entity
		/// moves or rotates. Invokes CameraMoved if the camera moved.
		/// </summary>
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public override void SwitchFromThis(CameraState toState)
		{
			ClearFollowed();			
		}

		/// <summary>
		/// Sets the entity to follow by this camera.
		/// If already following, switches from the current entity to new <paramref name="entity"/>
		/// </summary>
		/// <param name="entity">The entity to follow.</param>
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

		/// <summary>
		/// Informs everyone that the camera moved.
		/// </summary>
		protected override void SignalCameraMoved()
		{
			cameraMoved = true;
		}

		/// <summary>
		/// Removes the handlers watching for followed entity movement and rotation from the entity
		/// and stops following the entity.
		/// </summary>
		void ClearFollowed()
		{
			if (Followed != null) {
				Followed.RotationChanged -= OnFollowedRotationChanged;
				Followed.PositionChanged -= OnFollowedPositionChanged;
				Followed = null;
				CameraHolder = null;
			}
		}

		/// <summary>
		/// Counteracts the rotation of the entity, so that the camera remains
		/// facing the stored direction <see cref="cameraWorldDirection"/>.
		/// </summary>
		/// <param name="entity">The followed entity.</param>
		void OnFollowedRotationChanged(IEntity entity)
		{
			CorrectWorldDirection();
		}

		/// <summary>
		/// Handles the movement of the followed entity, remembers to signal
		/// that the camera moved in the game world.
		/// </summary>
		/// <param name="entity">The followed entity.</param>
		void OnFollowedPositionChanged(IEntity entity)
		{
			cameraMoved = true;
		}

		/// <summary>
		/// Counteracts the rotation of the followed entity so that the camera always faces the <see cref="cameraWorldDirection"/>.
		/// </summary>
		void CorrectWorldDirection()
		{
			Vector3 localDirection = Vector3.Normalize(CameraHolder.WorldToLocal(CameraHolder.WorldPosition + cameraWorldDirection));
			//Urho.IO.Log.Write(LogLevel.Debug, $"LocalDirection: {localDirection}");
			CameraNode.Position = -localDirection * cameraDistance;
			//NOTE: Should probably check return value, but don't know what to do on failure.
			CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);
		}
	}
}
