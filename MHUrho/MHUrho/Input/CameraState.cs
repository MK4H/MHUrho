using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Input
{
	/// <summary>
	/// States of the camera, for now they match to mode like this
	/// RTS == Fixed
	/// Freefloating == FreeFloat
	/// Following == Following
	///
	/// But they are kept separate for future changes
	/// </summary>
	enum CameraStates {
		Fixed,
		Following,
		FreeFloat
	}

	delegate void SwitchState(CameraStates newState);

    abstract class CameraState {

		public event OnCameraMove CameraMoved;

		public abstract Vector3 CameraWorldPosition { get; }
		public abstract Quaternion CameraWorldRotation { get; }

		public abstract CameraMode CameraMode{ get; }

		protected IMap Map;
		protected SwitchState SwitchState;

		protected CameraState(IMap map, SwitchState switchState)
		{
			this.Map = map;
			this.SwitchState = switchState;
		}

		public abstract void MoveTo(Vector2 xzPosition);

		public abstract void MoveTo(Vector3 position);

		public abstract void MoveBy(Vector2 xzMovement);

		public abstract void MoveBy(Vector3 movement);

		public abstract void Rotate(Vector2 rotation);

		public abstract void Zoom(float zoom);

		public virtual void PreChangesUpdate()
		{

		}

		public virtual void PostChangesUpdate()
		{

		}

		/// <summary>
		/// Initializes state when switching to this state
		/// </summary>
		/// <param name="fromState">Previous state or null if starting game</param>
		public virtual void SwitchToThis(CameraState fromState)
		{

		}

		/// <summary>
		/// Cleans up state when switching to another state
		/// </summary>
		/// <param name="toState">State we are switching to</param>
		public virtual void SwitchFromThis(CameraState toState)
		{

		}

		protected virtual void OnCameraMove(IEntity followedEntity = null)
		{
			CameraMoved?.Invoke(new CameraMovedEventArgs(CameraWorldPosition,
														CameraWorldRotation,
														CameraMode,
														followedEntity));
		}

		protected Vector2 RoundPositionToMap(Vector2 position)
		{
			if (position.X < Map.Left) {
				position.X = Map.Left;
			}
			else if (position.X > Map.Left + Map.Width) {
				position.X = Map.Left + Map.Width - 0.01f; ;
			}

			if (position.Y < Map.Top) {
				position.Y = Map.Top;
			}
			else if (position.Y > Map.Top + Map.Length) {
				position.Y = Map.Top + Map.Length - 0.01f;
			}

			return position;
		}


		protected Vector3 RoundPositionToMap(Vector3 position, 
												bool heightFromBuildings = false, 
												float minOffsetHeight = 0, 
												float minOffsetBorder = 0)
		{
			if (position.X < Map.Left + minOffsetBorder) {
				position.X = Map.Left + minOffsetBorder;
			}
			else if (position.X > Map.Left + Map.Width - minOffsetBorder) {
				position.X = Map.Left + Map.Width - 0.01f - minOffsetBorder;
			}

			if (position.Z < Map.Top + minOffsetBorder) {
				position.Z = Map.Top + minOffsetBorder;
			}
			else if (position.Z > Map.Top + Map.Length - minOffsetBorder) {
				position.Z = Map.Top + Map.Length - 0.01f - minOffsetBorder;
			}

			float height = heightFromBuildings ? Map.GetHeightAt(position.X, position.Z) : Map.GetTerrainHeightAt(position.X, position.Z);
			if (position.Y <= height + minOffsetHeight) {
				position.Y = height + minOffsetHeight;
			}

			return position;
		}


		
	}
}
