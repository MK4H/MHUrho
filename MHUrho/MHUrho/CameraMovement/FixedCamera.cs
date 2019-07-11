using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.CameraMovement
{
    class FixedCamera : PointFollowingCamera
    {
		public override CameraMode CameraMode => CameraMode.RTS;

		bool cameraMoved;

		public FixedCamera(IMap map, Node levelNode, Node cameraNode, Vector2 initialPosition, StateSwitchedDelegate stateSwitched)
			: base(map, cameraNode, levelNode.CreateChild("CameraHolder"), stateSwitched)
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

		public override void Reset()
		{
			CameraNode.Position = new Vector3(0, 10, -5);
			WantedCameraVerticalOffset = 10;
			CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);
		}

		public override void PreChangesUpdate()
		{
			cameraMoved = false;
			base.PreChangesUpdate();
		}

		public override void PostChangesUpdate()
		{
			//TODO: Signal that camera moved if the terrain moved
			Vector2 newHolderXZPosition = RoundPositionToMap(CameraHolder.Position.XZ2());
			CameraHolder.Position = new Vector3(newHolderXZPosition.X,
												Map.GetTerrainHeightAt(newHolderXZPosition),
												newHolderXZPosition.Y);


			base.PostChangesUpdate();

			if (cameraMoved) {
				OnCameraMove();
			}
		}

		public override void SwitchToThis(CameraState fromState)
		{
			if (fromState == null) {
				CameraNode.ChangeParent(CameraHolder);
				Reset();
				return;
			}

			if (fromState is FixedCamera)
			{
				return;
			}
			else if (fromState is EntityFollowingCamera followingCamera) {
				CameraHolder.Position = CameraNode.Parent.WorldPosition;
				SwitchToThisFromPFC(followingCamera);

			}
			else if (fromState is FreeFloatCamera freeFloatCam) {

				//Sets the camera to look in the current direction at 45 degrees down at the ground
				Vector2 cameraXZDirection = CameraNode.Direction.XZ2();
				//Check for looking straight up or down, where normalize would crash
				if (cameraXZDirection != Vector2.Zero) {
					cameraXZDirection.Normalize();
				}
				else {
					//If looking straight up or down, just look in positive Z
					cameraXZDirection = Vector2.UnitY;
				}

				//45 degrees down
				Vector3 newCameraDirection = Vector3.Normalize(new Vector3(cameraXZDirection.X, -1, cameraXZDirection.Y));

				//Easy way of raycasting to map
				var results = Map.RaycastToMap(new Ray(CameraNode.Position, newCameraDirection));

				bool found = false;
				foreach (var result in results) {
					CameraHolder.Position = RoundPositionToMap(result.Position, false);
					CameraNode.Position = CameraNode.Position - CameraHolder.Position;
					found = true;
					break;
				}

				//If the raycast failed (outside of the map or something)
				if (!found) {
					float cameraHeightAboveTerrain = CameraNode.Position.Y - Map.GetTerrainHeightAt(CameraNode.Position.XZ2());

					Vector2 offset = cameraXZDirection * cameraHeightAboveTerrain;
					Vector2 holderXZPosition = CameraNode.Position.XZ2() + offset;
					holderXZPosition = RoundPositionToMap(holderXZPosition);
					Vector3 holderPosition =
						new Vector3(holderXZPosition.X, Map.GetTerrainHeightAt(holderXZPosition), holderXZPosition.Y);

					CameraNode.Position = new Vector3(-offset.X, cameraHeightAboveTerrain, -offset.Y);
					CameraHolder.Position = holderPosition;
				}

				WantedCameraVerticalOffset = CameraNode.WorldPosition.Y - CameraHolder.Position.Y;
				CameraNode.ChangeParent(CameraHolder);
				CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);

				
			}
			else {
				throw new ArgumentOutOfRangeException(nameof(fromState), "Unknow camera state");
			}
		}

		public override void SwitchFromThis(CameraState toState)
		{

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
			var worldDelta = Vector3.Normalize(CameraNode.Direction.XZ()) * deltaZ +
							Vector3.Normalize(CameraNode.Right.XZ()) * deltaX;


			Vector2 newPosition = (CameraHolder.Position + worldDelta).XZ2();

			if (!Map.IsInside(newPosition)) {
				newPosition = RoundPositionToMap(newPosition);
			}

			CameraHolder.Position = new Vector3(newPosition.X, Map.GetTerrainHeightAt(newPosition), newPosition.Y);
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
