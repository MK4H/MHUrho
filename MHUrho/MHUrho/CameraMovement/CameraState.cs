using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.CameraMovement
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

	/// <summary>
	/// The delegate for methods handling the state switch event.
	/// </summary>
	/// <param name="newState">The new state we are switching to.</param>
	delegate void StateSwitchedDelegate(CameraStates newState);

	/// <summary>
	/// Implementation of the state design pattern to
	/// implement three different behaviors of the camera.
	/// </summary>
    abstract class CameraState {

		/// <summary>
		/// Invoked when camera changes position, rotation or zoom.
		/// </summary>
		public event OnCameraMoveDelegate CameraMoved;

		/// <summary>
		/// Position of the camera in the game world.
		/// </summary>
		public abstract Vector3 CameraWorldPosition { get; }

		/// <summary>
		/// Rotation of the camera in the game world.
		/// </summary>
		public abstract Quaternion CameraWorldRotation { get; }

		/// <summary>
		/// The current mode of the camera.
		/// </summary>
		public abstract CameraMode CameraMode{ get; }

		protected IMap Map;
		protected StateSwitchedDelegate StateSwitched;

		/// <summary>
		/// Creates new instance of the representation of camera behavior.
		/// </summary>
		/// <param name="map">The level map in which the camera exists.</param>
		/// <param name="stateSwitched">The handler to invoke when a state switch occurs.</param>
		protected CameraState(IMap map, StateSwitchedDelegate stateSwitched)
		{
			this.Map = map;
			this.StateSwitched = stateSwitched;
		}

		/// <summary>
		/// Sets the camera position to be at the <paramref name="xzPosition"/> in the XZ plane.
		/// Leaves the height unchanged.
		/// </summary>
		/// <param name="xzPosition">The new position of the camera in the XZ plane.</param>
		public abstract void MoveTo(Vector2 xzPosition);

		/// <summary>
		/// Sets the camera position to be at the <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The new position of the camera.</param>
		public abstract void MoveTo(Vector3 position);

		/// <summary>
		/// Moves camera by <paramref name="xzMovement"/> in the XZ plane from the current position.
		/// Does not change the height of the camera.
		/// </summary>
		/// <param name="xzMovement">The change of position in the XZ plane.</param>
		public abstract void MoveBy(Vector2 xzMovement);

		/// <summary>
		/// Moves camera by <paramref name="movement"/> from the current position.
		/// </summary>
		/// <param name="movement">The change of position of the camera.</param>
		public abstract void MoveBy(Vector3 movement);

		/// <summary>
		/// Rotates the camera around the vertical axis by <paramref name="rotation"/>.X and
		/// around the horizontal axis by <paramref name="rotation"/>.Y.
		/// </summary>
		/// <param name="rotation">Rotation of the camera around the vertical and horizontal axes.</param>
		public abstract void Rotate(Vector2 rotation);

		/// <summary>
		/// Changes the zoom of the camera by <paramref name="zoom"/>.
		/// </summary>
		/// <param name="zoom">The change of the zoom of the camera.</param>
		public abstract void Zoom(float zoom);

		/// <summary>
		/// Resets the camera to the default position for the state.
		/// </summary>
		public abstract void Reset();

		/// <summary>
		/// Update invoked before the changes to the position, rotation and zoom are executed.
		/// </summary>
		public virtual void PreChangesUpdate()
		{

		}

		/// <summary>
		/// Update invoked after the changes to the position, rotation and zoom are executed.
		/// </summary>
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

		/// <summary>
		/// Invokes the <see cref="CameraMoved"/> event.
		/// </summary>
		/// <param name="followedEntity">If we are invoking the event while following an entity, contains the reference to the followed entity.</param>
		protected virtual void OnCameraMove(IEntity followedEntity = null)
		{
			CameraMoved?.Invoke(new CameraMovedEventArgs(CameraWorldPosition,
														CameraWorldRotation,
														CameraMode,
														followedEntity));
		}

		/// <summary>
		/// If the given <paramref name="position"/> is outside the map borders, rounds it
		/// to the closest border. Otherwise does not change the <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The position to round to be inside the map.</param>
		/// <returns>Position rounded to be inside the map if it was outside the map, or <paramref name="position"/> if
		/// it was already inside the map.</returns>
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

		/// <summary>
		/// If the given <paramref name="position"/> is outside the map borders or closer to the borders than <paramref name="minOffsetBorder"/>,
		/// rounds it to be at least <paramref name="minOffsetBorder"/> from the closest border.
		///	If the <paramref name="position"/> is underneath the map, moves it vertically above the terrain to be at least <paramref name="minOffsetHeight"/>
		/// above the height of the terrain.
		/// If <paramref name="heightFromBuildings"/> is true, takes into the consideration the height of the buildings, not only the height of the terrain.
		/// Otherwise does not change the <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The position to round to be inside the map with specified offsets.</param>
		/// <param name="heightFromBuildings">Take into consideration height of the buildings above the height of the map itself.</param>
		/// <param name="minOffsetBorder">Minimal offset of the camera from the border.</param>
		/// <param name="minOffsetHeight">Minimal offset of the camera in the vertical direction from the terrain height or building height, based on <paramref name="heightFromBuildings"/>.</param>
		/// <returns>Position rounded inside the map at least <paramref name="minOffsetBorder"/> from the closest border, above the terrain
		/// at least <paramref name="minOffsetHeight"/> above, or if <paramref name="heightFromBuildings"/> above the possible buildings.</returns>
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
