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
		protected const float MinZoomDistance = 0.5f;
		protected const float MinOffsetFromTerrain = 0.5f;

		public override Vector3 CameraWorldPosition => CameraHolder.WorldPosition;
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

		protected PointFollowingCamera(IMap map, Node cameraNode, Node cameraHolder, StateSwitchedDelegate stateSwitched)
			:base(map, stateSwitched)
		{
			this.Map = map;
			this.CameraNode = cameraNode;
			this.CameraHolder = cameraHolder;

		}

		public override void Rotate(Vector2 rotation)
		{
			//Horizontal rotation
			CameraNode.RotateAround(new Vector3(0, 0, 0), Quaternion.FromAxisAngle(Quaternion.Invert(CameraHolder.WorldRotation) * Vector3.UnitY, rotation.X), TransformSpace.Parent);
			//Vertical rotation
			CameraNode.RotateAround(new Vector3(0, 0, 0), Quaternion.FromAxisAngle(CameraNode.Right, rotation.Y), TransformSpace.Parent);
			

			SignalCameraMoved();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="delta"></param>
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

		public override void PreChangesUpdate()
		{
			//Urho.IO.Log.Write(LogLevel.Debug, $"Pre: {WantedCameraHeight}");
			CameraNode.Position = CameraHolder.WorldToLocal(CameraNode.WorldPosition.WithY(CameraHolder.WorldPosition.Y + WantedCameraVerticalOffset));
		}

		public override void PostChangesUpdate()
		{
			WantedCameraVerticalOffset = CameraNode.WorldPosition.Y - CameraHolder.WorldPosition.Y;
			//Urho.IO.Log.Write(LogLevel.Debug, $"Post before Fix: {WantedCameraHeight}");
			FixCameraNodeTerrainClipping();

			//Urho.IO.Log.Write(LogLevel.Debug, $"Post after Fix: {WantedCameraHeight}");

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

		protected abstract void SignalCameraMoved();
	}
}
