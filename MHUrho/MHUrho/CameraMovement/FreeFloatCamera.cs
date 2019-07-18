using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.CameraMovement
{
    class FreeFloatCamera : CameraState
    {
		/// <summary>
		/// Minimal distance from the ground right below the current position of the camera.
		/// </summary>
		const float MinHeightOffset = 0.2f;

		/// <summary>
		/// Position of the camera in the game world.
		/// </summary>
		public override Vector3 CameraWorldPosition => cameraNode.WorldPosition;

		/// <summary>
		/// Rotation of the camera compared to the game world.
		/// </summary>
		public override Quaternion CameraWorldRotation => cameraNode.WorldRotation;

		/// <inheritdoc />
		public override CameraMode CameraMode => CameraMode.FreeFloating;

		/// <summary>
		/// Node representing the whole level in the game engine.
		/// </summary>
		readonly Node levelNode;

		/// <summary>
		/// Node of the camera itself.
		/// </summary>
		readonly Node cameraNode;

		/// <summary>
		/// Creates a camera behavior that moves the camera freely inside the bounds of the level.
		/// </summary>
		/// <param name="map">The map the camera will be moving around in.</param>
		/// <param name="levelNode">The <see cref="Node"/> representing the whole level.</param>
		/// <param name="cameraNode">The <see cref="Node"/> containing the <see cref="Camera"/> component.</param>
		/// <param name="stateSwitched">Handler that will be called when a state switch occurs.</param>
		public FreeFloatCamera(IMap map, Node levelNode, Node cameraNode, StateSwitchedDelegate stateSwitched)
			:base(map, stateSwitched)
		{
			this.levelNode = levelNode;
			this.cameraNode = cameraNode;
		}

		
		/// <summary>
		/// Moves the camera to the <paramref name="xzPosition"/> in the XZ plane,
		/// does not change the Y coordinate of the camera.
		/// </summary>
		/// <param name="xzPosition">The position in the XZ plane to move the camera to.</param>
		public override void MoveTo(Vector2 xzPosition)
		{
			MoveTo(new Vector3(xzPosition.X, cameraNode.Position.Y, xzPosition.Y));
		}

		/// <summary>
		/// Moves the camera to the <paramref name="position"/> in the game world.
		/// If the given position falls outside the map, moves the camera as
		/// close as possible to the <paramref name="position"/> while still
		/// being inside the map.
		/// </summary>
		/// <param name="position">The new position of the camera.</param>
		public override void MoveTo(Vector3 position)
		{
			cameraNode.Position = RoundPositionToMap(position, true, MinHeightOffset);
			OnCameraMove();
		}

		/// <summary>
		/// Moves the camera by <paramref name="xzMovement"/> in the XZ plane.
		/// Does not change the Y coordinate of the camera position.
		/// If the movement would be outside the map bounds, moves the camera to the border of the map.
		/// </summary>
		/// <param name="xzMovement">The change of position in the XZ plane.</param>
		public override void MoveBy(Vector2 xzMovement)
		{
			Vector3 oldPosition = cameraNode.Position;
			Vector3 newPosition = new Vector3(oldPosition.X + xzMovement.X, oldPosition.Y, oldPosition.Z + xzMovement.Y);
			cameraNode.Position = RoundPositionToMap(newPosition, true, MinHeightOffset);
			OnCameraMove();
		}

		/// <summary>
		/// Moves the camera by <paramref name="movement"/> from the point of view of the camera.
		/// X coordinate represents movement to the right, Z coordinate movement forward, Y coordinate
		/// up.
		/// </summary>
		/// <param name="movement">The change of position of the camera based on the direction of view of the camera.</param>
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

		/// <summary>
		/// Rotates the camera around it's current position.
		/// </summary>
		/// <param name="rotation">The rotation to apply to the camera, X around vertical axis, Y around the axis pointing to the right
		/// from the current point of view of the camera.</param>
		public override void Rotate(Vector2 rotation)
		{
			cameraNode.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, rotation.X), TransformSpace.Parent);
			cameraNode.Rotate(Quaternion.FromAxisAngle(cameraNode.Right, rotation.Y), TransformSpace.Parent);
			OnCameraMove();
		}

		/// <summary>
		/// This type of camera does not zoom.
		/// </summary>
		/// <param name="zoom">Does nothing.</param>
		public override void Zoom(float zoom)
		{
			//ALT: maybe move forward/backward
		}

		/// <summary>
		/// Resets the camera rotation so that it will face in the default direction.
		/// </summary>
		public override void Reset()
		{
			cameraNode.Rotate(Quaternion.FromRotationTo(cameraNode.Direction, new Vector3(0, -1, 1)), TransformSpace.World);
		}

		/// <inhertidoc />
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
