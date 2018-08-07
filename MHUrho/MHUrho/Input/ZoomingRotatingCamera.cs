using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Input
{
    abstract class ZoomingRotatingCamera : CameraState
    {
		protected const float MinZoomDistance = 0.5f;
		protected const float MinOffsetFromTerrain = 0.5f;

		public override Vector3 CameraWorldPosition => CameraNode.WorldPosition;
		public override Quaternion CameraWorldRotation => CameraNode.WorldRotation;

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
		/// Height of the camera during Fixed mode without stretch
		/// The camera stretches upwards when there is an obstacle
		/// When the camera moves away from the obstacle, it returns to this height
		/// </summary>
		float wantedCameraHeight;

		protected ZoomingRotatingCamera(IMap map, Node cameraNode, Node cameraHolder, SwitchState switchState)
			:base(map, switchState)
		{
			this.Map = map;
			this.CameraNode = cameraNode;
			this.CameraHolder = cameraHolder;

		}

		public override void Rotate(Vector2 rotation)
		{
			CameraNode.RotateAround(new Vector3(0, 0, 0), Quaternion.FromAxisAngle(Vector3.UnitY, rotation.X), TransformSpace.Parent);


			if ((5 < CameraNode.Rotation.PitchAngle && rotation.Y < 0) || (CameraNode.Rotation.PitchAngle < 85 && rotation.Y > 0)) {
				CameraNode.RotateAround(new Vector3(0, 0, 0), Quaternion.FromAxisAngle(CameraNode.WorldRight, rotation.Y), TransformSpace.Parent);
			}

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
			CameraNode.Position = CameraNode.Position.WithY(wantedCameraHeight);
		}

		public override void PostChangesUpdate()
		{
			wantedCameraHeight = CameraNode.Position.Y;
			FixCameraNodeTerrainClipping();

			if (CameraNode.Position.Y != wantedCameraHeight) {
				SignalCameraMoved();
			}
		}

		

		/// <summary>
		/// Moves camera node in the Y direction above terrain and buildings at the camera XZ coords 
		/// </summary>
		protected void FixCameraNodeTerrainClipping()
		{
			if (Map.IsInside(CameraNode.WorldPosition)) {
				//Check if it does not clip anything
				float heightAtCameraNode = Map.GetHeightAt(CameraNode.WorldPosition.XZ2());
				if (CameraNode.WorldPosition.Y < heightAtCameraNode + MinOffsetFromTerrain) {
					CameraNode.Position = CameraNode.Position.WithY(CameraNode.Position.Y +
																	(heightAtCameraNode + MinOffsetFromTerrain - CameraNode.WorldPosition.Y));
				}
			}
			

			CameraNode.LookAt(CameraHolder.WorldPosition, Vector3.UnitY);
		}

		protected void SwitchToThisFromZRC(ZoomingRotatingCamera zoomingRotatingCamera)
		{
			wantedCameraHeight = zoomingRotatingCamera.wantedCameraHeight;
			//Scale the position from 1 scale so it stays the same regardless of cameraholder.scale
			CameraNode.Position = Vector3.Divide(CameraNode.Position, CameraHolder.Scale);
			CameraNode.ChangeParent(CameraHolder);

		}

		protected abstract void SignalCameraMoved();
	}
}
