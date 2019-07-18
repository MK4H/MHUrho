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
		/// <inheritdoc />
		public override CameraMode CameraMode => CameraMode.RTS;

		/// <summary>
		/// If the camera moved during the current update calculation
		/// </summary>
		bool cameraMoved;

		/// <summary>
		/// Creates new camera behavior that follows an artificial invisible fixed point in the game world.
		/// </summary>
		/// <param name="map">The map of the level the camera exists in.</param>
		/// <param name="levelNode">The <see cref="Node"/> representing the whole level.</param>
		/// <param name="cameraNode">The <see cref="Node"/> containing the <see cref="Camera"/> component.</param>
		/// <param name="initialPosition">The initial position of the followed point in the game world.</param>
		/// <param name="stateSwitched">The handler to invoke when a state switch occurs.</param>
		public FixedCamera(IMap map, Node levelNode, Node cameraNode, Vector2 initialPosition, StateSwitchedDelegate stateSwitched)
			: base(map, cameraNode, levelNode.CreateChild("CameraHolder"), stateSwitched)
		{
			CameraHolder.Position = new Vector3(initialPosition.X,
												map.GetTerrainHeightAt(initialPosition),
												initialPosition.Y);
		}

		/// <summary>
		/// Moves the followed point to the <paramref name="xzPosition"/> in the XZ plane,
		/// adjusts the height of the followed point to be on the terrain.
		/// </summary>
		/// <param name="xzPosition">The new position of the followed point in the XZ plane.</param>
		public override void MoveTo(Vector2 xzPosition)
		{
			Vector2 newPosition = RoundPositionToMap(xzPosition);
			CameraHolder.Position = new Vector3(newPosition.X,
												Map.GetTerrainHeightAt(newPosition),
												newPosition.Y);
			cameraMoved = true;
		}

		/// <inheritdoc />
		/// <summary>
		/// Moves the followed point to the <paramref name="position"/> in the game world.
		/// </summary>
		/// <param name="position">The new position of the followed point in the game world.</param>
		public override void MoveTo(Vector3 position)
		{
			MoveTo(position.XZ2());
		}

		/// <inheritdoc />
		/// <summary>
		/// Moves the followed point by <paramref name="xzMovement"/> in XZ plane from the
		/// camera perspective. The X coordinate represents movement to the right of the camera,
		/// Z coordinate represents movement forward from the direction of view of the camera WHEN
		/// PROJECTED into the game world XZ plane.
		/// </summary>
		/// <param name="xzMovement">The X coordinate represents movement to the right of the camera,
		/// Z coordinate represents movement forward from the direction of view of the camera WHEN
		/// PROJECTED into the game world XZ plane.</param>
		public override void MoveBy(Vector2 xzMovement)
		{
			MoveHorizontal(xzMovement.X, xzMovement.Y);
			cameraMoved = true;
		}

		/// <summary>
		/// Moves the followed point by <paramref name="movement"/>. The X and Z coordinates
		/// are used as in <see cref="MoveBy(Vector2)"/>, the Y coordinate is used for movement in
		/// the world Y axis direction.
		/// </summary>
		/// <param name="movement">The change of position of the followed point, where X and Z are used as in
		/// <see cref="MoveBy(Vector2)"/> and Y is used for movement in world vertical direction.</param>
		public override void MoveBy(Vector3 movement)
		{
			MoveHorizontal(movement.X, movement.Z);
			MoveVertical(movement.Y);
			cameraMoved = true;
		}

		/// <summary>
		/// Resets the camera to default offset from the followed point and turns the
		/// camera to look at the followed point.
		/// </summary>
		public override void Reset()
		{
			CameraNode.Position = new Vector3(0, 10, -5);
			WantedCameraVerticalOffset = 10;
			CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);
		}

		/// <inheritdoc />
		public override void PreChangesUpdate()
		{
			//Resets the cameraMoved flag so that we can detect camera movement
			// during the current update.
			cameraMoved = false;
			base.PreChangesUpdate();
		}

		/// <inheritdoc />
		public override void PostChangesUpdate()
		{
			//FUTURE: Signal that camera moved if the terrain moved
			Vector2 newHolderXZPosition = RoundPositionToMap(CameraHolder.Position.XZ2());
			CameraHolder.Position = new Vector3(newHolderXZPosition.X,
												Map.GetTerrainHeightAt(newHolderXZPosition),
												newHolderXZPosition.Y);


			base.PostChangesUpdate();

			if (cameraMoved) {
				OnCameraMove();
			}
		}

		/// <inheritdoc />
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
				throw new ArgumentOutOfRangeException(nameof(fromState), "Unknown camera state");
			}
		}

		/// <inheritdoc />
		public override void SwitchFromThis(CameraState toState)
		{

		}

		/// <summary>
		/// Handles the signal from predecessor methods that the camera was moved.
		/// </summary>
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
