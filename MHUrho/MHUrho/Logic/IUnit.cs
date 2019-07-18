using System;
using MHUrho.EntityInfo;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {

	/// <summary>
	/// Unit in the level.
	/// </summary>
	public interface IUnit : IEntity {
		/// <summary>
		/// Get or Set value indicating if units Up vector will always be the same as World up vector (Vector3.UnitY) if true
		///		or if unit can be freely rotated
		/// </summary>
		bool AlwaysVertical { get; set; }


		/// <summary>
		/// Instance plugin of this unit.
		/// </summary>
		UnitInstancePlugin UnitPlugin { get; }

		

		/// <summary>
		/// Get tile currently containinng the units Node, value is equal to IMap.GetContainingTile(unit.Position)
		/// <see cref="WorldMap.IMap.GetContainingTile(Vector3)"/>
		/// </summary>
		ITile Tile { get; }

		/// <summary>
		/// Get unitType instance representing current type of the unit
		/// <see cref="Logic.UnitType"/>
		/// </summary>
		UnitType UnitType { get; }

		/// <summary>
		/// Rotates the unit to face towards the <paramref name="lookPosition"/> point in the game world.
		/// Can rotate straight towards the <paramref name="lookPosition"/> or can only rotate around Y
		/// axis to preserve the current tilt of the unit.
		/// </summary>
		/// <param name="lookPosition">The world position the unit will be looking towards.</param>
		/// <param name="rotateAroundY">If the rotation should be done only around Y axis or it should be full rotation.</param>
		void FaceTowards(Vector3 lookPosition, bool rotateAroundY = false);

		/// <summary>
		/// Moves the unit by <paramref name="moveBy"/> in the XZ plane.
		/// </summary>
		/// <param name="moveBy">The change of position in the XZ plane.</param>
		void MoveBy(Vector2 moveBy);

		/// <summary>
		/// Moves the unit by <paramref name="moveBy"/> relative to the current position.
		/// </summary>
		/// <param name="moveBy">The change in position.</param>
		void MoveBy(Vector3 moveBy);


		/// <summary>
		/// Moves the unit to the <paramref name="newLocation"/> on the map surface.
		/// </summary>
		/// <param name="newLocation">The new location of the unit.</param>
		void MoveTo(Vector2 newLocation);

		/// <summary>
		/// Moves the unit to the <paramref name="newPosition"/>
		/// </summary>
		/// <param name="newPosition">The new position of the unit.</param>
		void MoveTo(Vector3 newPosition);

		/// <summary>
		/// Serializes the units current state into an instance of <see cref="StUnit"/>.
		/// </summary>
		/// <returns>Serialized representation of the units current state.</returns>
		StUnit Save();

		/// <summary>
		/// Sets height of the unit.
		/// </summary>
		/// <param name="newHeight">New height of the unit.</param>
		void SetHeight(float newHeight);

		/// <summary>
		/// Notifies the unit that the height of the tile it is standing on has changed.
		/// </summary>
		/// <param name="tile">The tile the unit is standing on.</param>
		void TileHeightChanged(ITile tile);

		/// <summary>
		/// Notifies the unit that a building on the tile it was standing on was destroyed.
		/// </summary>
		/// <param name="building">The destroyed building.</param>
		/// <param name="tile">The tile this unit is standing on.</param>
		void BuildingDestroyed(IBuilding building, ITile tile);

		/// <summary>
		/// Notifies the unit that a building was built on the tile it was standing on.
		/// </summary>
		/// <param name="building">The new building.</param>
		/// <param name="tile">The tile this unit is standing on.</param>
		void BuildingBuilt(IBuilding building, ITile tile);
	}
}