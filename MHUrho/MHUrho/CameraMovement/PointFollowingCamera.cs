using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers.Extensions;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.CameraMovement
{
    abstract class PointFollowingCamera : CameraState
    {
		/// <summary>
		/// Limit of zooming so that we don't get too close to the followed point.
		/// </summary>
		protected const float MinZoomDistance = 0.5f;

		/// <summary>
		/// Minimal distance in the vertical direction from the terrain or building below. 
		/// </summary>
		protected const float MinOffsetFromTerrain = 0.5f;

		/// <summary>
		/// Position of the followed point in the game world.
		/// </summary>
		public override Vector3 CameraWorldPosition => CameraHolder.WorldPosition;

		/// <summary>
		/// Rotation of the followed point in the game world.
		/// </summary>
		public override Quaternion CameraWorldRotation => CameraHolder.WorldRotation;

		/// <summary>
		/// Point on the ground
		/// Camera follows this point at constant offset while not in FreeFloat mode
		/// </summary>
		protected Node CameraHolder;
		/// <summary>
		/// Node of the camera itself
		/// </summary>
		protected Node CameraNode;

		/// <summary>
		/// Vertical offset of the camera during Fixed mode without stretch
		/// World Y coordinate difference from CameraHolder
		/// The camera stretches upwards when there is an obstacle
		/// When the camera moves away from the obstacle, it returns to this height
		/// </summary>
		protected float WantedCameraVerticalOffset;

		/// <summary>
		/// Creates new instance representing a camera behavior that follows a set point in a game world.
		/// </summary>
		/// <param name="map">The level map in which the camera exists.</param>
		/// <param name="cameraNode">The node containing the <see cref="Camera"/> component. </param>
		/// <param name="cameraHolder">The point the camera will be offset from and following.</param>
		/// <param name="stateSwitched">The handler to invoke when a state switch occurs.</param>
		protected PointFollowingCamera(IMap map, Node cameraNode, Node cameraHolder, StateSwitchedDelegate stateSwitched)
			:base(map, stateSwitched)
		{
			this.Map = map;
			this.CameraNode = cameraNode;
			this.CameraHolder = cameraHolder;

		}

		/// <summary>
		/// Rotates the camera around the followed point. X represents rotation around the vertical axis,
		/// Y represents the rotation around the axis pointing right from the current direction of camera.
		/// </summary>
		/// <param name="rotation">The applied rotation of the camera, X represents rotation around the vertical axis,
		/// Y represents the rotation around the axis pointing right from the current direction of camera.</param>
		public override void Rotate(Vector2 rotation)
		{
			//Horizontal rotation
			CameraNode.RotateAround(new Vector3(0, 0, 0), Quaternion.FromAxisAngle(Quaternion.Invert(CameraHolder.WorldRotation) * Vector3.UnitY, rotation.X), TransformSpace.Parent);
			//Vertical rotation
			CameraNode.RotateAround(new Vector3(0, 0, 0), Quaternion.FromAxisAngle(CameraNode.Right, rotation.Y), TransformSpace.Parent);
			

			SignalCameraMoved();
		}

		/// <summary>
		/// Moves the camera closer to or further from the followed point, where + means further and - means closer.
		/// </summary>
		/// <param name="delta">The change of distance from the followed point, + means further away, - means closer.</param>
		public override void Zoom(float delta)
		{
			Vector3 newPosition = CameraNode.Position + Vector3.Divide(Vector3.Normalize(CameraNode.Position) * (-delta), CameraHolder.Scale);

			/*If distance to holder (which is at 0,0,0) is less than min allowed distance
			 or the vector changed quadrant, which means it went through 0,0,0
			 */

			if (newPosition.Length < MinZoomDistance ||
				newPosition.Y < 0) {
				CameraNode.Position = Vector3.Normalize(CameraNode.Position) * MinZoomDistance;
			}
			else {
				CameraNode.Position = newPosition;
			}

			SignalCameraMoved();
		}

		/// <inheritdoc />
		public override void PreChangesUpdate()
		{
			//moves the camera to the wanted offset from the ground, in case it was stretched upwards because of terrain or buildings below.
			// applied here so that all the calculations are based off of the correct height.
			CameraNode.Position = CameraHolder.WorldToLocal(CameraNode.WorldPosition.WithY(CameraHolder.WorldPosition.Y + WantedCameraVerticalOffset));
		}

		/// <inheritdoc />
		public override void PostChangesUpdate()
		{
			//After the calculations of movement, rotation, zoom etc. stores the wanted vertical offset from the
			// ground below and then possibly stretches the vertical offset to prevent clipping.
			WantedCameraVerticalOffset = CameraNode.WorldPosition.Y - CameraHolder.WorldPosition.Y;
			FixCameraNodeTerrainClipping();

			//If we set some other value into the Y coord, signal that the camera moved.
			// does not matter if it did just by a small amount, it moved.
			if (CameraNode.WorldPosition.Y != WantedCameraVerticalOffset) {
				SignalCameraMoved();
			}
		}

		

		/// <summary>
		/// Moves camera node in the Y direction above terrain and buildings at the camera XZ coords 
		/// </summary>
		protected void FixCameraNodeTerrainClipping()
		{
			if (Map.IsInside(CameraNode.WorldPosition.XZ2())) {
				//Check if it does not clip anything
				float heightAtCameraNode = Map.GetHeightAt(CameraNode.WorldPosition.XZ2());
				if (CameraNode.WorldPosition.Y < heightAtCameraNode + MinOffsetFromTerrain) {
					//CameraNode.Position.Y may not be height, if the cameraHolder is pitched or rolled
					CameraNode.Position =
						CameraHolder.WorldToLocal(CameraNode.WorldPosition.WithY(heightAtCameraNode + MinOffsetFromTerrain));
				}
			}
			

			CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);
		}

		/// <summary>
		/// Uses the stored information in the previous <see cref="PointFollowingCamera"/> state we are
		/// switching from so that the camera does not suddenly jump and stuff.
		/// </summary>
		/// <param name="pointFollowingCamera"></param>
		protected void SwitchToThisFromPFC(PointFollowingCamera pointFollowingCamera)
		{
			Node prevCameraHolder = CameraNode.Parent;
			//Does not need to be scaled, because it is WORLD Y position of camera
			WantedCameraVerticalOffset = pointFollowingCamera.WantedCameraVerticalOffset;
			//Scale the position from previous scale so it stays the same regardless of cameraholder.scale
			CameraNode.Position = Vector3.Multiply(CameraNode.Position, Vector3.Divide(prevCameraHolder.Scale, CameraHolder.Scale));
			//Rotate it to correct rotation
			CameraNode.Position = Quaternion.Invert(CameraHolder.WorldRotation) * prevCameraHolder.WorldRotation * CameraNode.Position;
			CameraNode.ChangeParent(CameraHolder);

		}

		/// <summary>
		/// Signals that the camera moved.
		/// </summary>
		protected abstract void SignalCameraMoved();
	}
}
