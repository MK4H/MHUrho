using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic
{

	public interface ITile {


		/// <summary>
		/// Units inside the tile.
		/// </summary>
		IReadOnlyList<IUnit> Units { get; }

		/// <summary>
		/// Building that owns this tile.
		/// </summary>
		IBuilding Building { get; }

		/// <summary>
		/// Tile type of this tile.
		/// </summary>
		TileType Type { get; }

		/// <summary>
		/// The area in the map this tile represents.
		/// </summary>
		IntRect MapArea { get; }

		/// <summary>
		/// Index into map matrix.
		/// </summary>
		IntVector2 MapLocation { get; }

		/// <summary>
		/// Corner of this tile with the minimum X, minimum Z coords.
		/// </summary>
		IntVector2 TopLeft { get; }

		/// <summary>
		/// Corner of this tile with the maximum X, minimum Z coords.
		/// </summary>
		IntVector2 TopRight { get; }

		/// <summary>
		/// Corner of this tile with the minimum X, maximum Z coords.
		/// </summary>
		IntVector2 BottomLeft { get; }

		/// <summary>
		/// Corner of this tile with the maximum X, maximum Z coords.
		/// </summary>
		IntVector2 BottomRight { get; }

		/// <summary>
		/// Coords of the center of the tile
		/// </summary>
		Vector2 Center { get; }

		/// <summary>
		/// 3D coords of the center of the tile
		/// </summary>
		Vector3 Center3 { get; }

		/// <summary>
		/// Complete world position of the top left (minimum X, minimum Z) corner of the tile.
		/// </summary>
		Vector3 TopLeft3 { get; }

		/// <summary>
		/// Complete world position of the top right (maximum X, minimum Z) corner of the tile.
		/// </summary>
		Vector3 TopRight3 { get; }

		/// <summary>
		/// Complete world position of the top right (minimum X, maximum Z) corner of the tile.
		/// </summary>
		Vector3 BottomLeft3 { get; }

		/// <summary>
		/// Complete world position of the top right (maximum X, maximum Z) corner of the tile.
		/// </summary>
		Vector3 BottomRight3 { get; }

		/// <summary>
		/// Height of the top left (minimum X, minimum Z) corner of the tile.
		/// </summary>
		float TopLeftHeight { get; }

		/// <summary>
		/// Height of the top right (maximum X, minimum Z) corner of the tile.
		/// </summary>
		float TopRightHeight { get; }

		/// <summary>
		/// Height of the top right (minimum X, maximum Z) corner of the tile.
		/// </summary>
		float BottomLeftHeight { get; }

		/// <summary>
		/// Height of the top right (maximum X, maximum Z) corner of the tile.
		/// </summary>
		float BottomRightHeight { get; }

		/// <summary>
		/// The map this tile is part of.
		/// </summary>
		IMap Map { get; }

		/// <summary>
		/// Adds unit to the list of units present on this tile.
		/// </summary>
		/// <param name="unit">The unit to add to this tile.</param>
		void AddUnit(IUnit unit);

		/// <summary>
		/// Removes the <paramref name="unit"/> from this tile.
		/// </summary>
		/// <param name="unit">The unit to remove.</param>
		/// <returns>True if the unit was successfully removed, false if it was not present on this tile.</returns>
		bool RemoveUnit(IUnit unit);

		/// <summary>
		/// Sets the new building on this tile. If there is already <see cref="Building"/> present, throws an exception. 
		/// </summary>
		/// <param name="building">New building to set to this tile.</param>
		/// <exception cref="InvalidOperationException">Thrown when there already is building present on this tile.</exception>
		void SetBuilding(IBuilding building);

		/// <summary>
		/// Removes building from this tile, if the provided building is not the <see cref="Building"/> on this tile,
		/// throws an exception.
		/// </summary>
		/// <param name="building">The building to remove.</param>
		/// <exception cref="ArgumentException">Thrown when the provided <paramref name="building"/> is not the <see cref="Building"/> on this tile.</exception>
		void RemoveBuilding(IBuilding building);

		/// <summary>
		/// Serializes the tile into <see cref="StTile"/>.
		/// </summary>
		/// <returns>Serialized tile stored in <see cref="StTile"/></returns>
		StTile Save();

		/// <summary>
		/// Changes the type of the tile.
		/// 
		/// Should only be called by Map class (how i wish C# had friend functions)
		/// </summary>
		/// <param name="newType">New type of the tile.</param>
		void ChangeType(TileType newType);

		/// <summary>
		/// Called by the Map to change height
		/// 
		/// If you want to change height, go through <see cref="Map.ChangeTileHeight(ITile, float)"/>
		/// </summary>
		/// <param name="heightDelta">Amount of change of the height.</param>
		void ChangeTopLeftHeight(float heightDelta);

		/// <summary>
		/// Sets the height of the top left corner of the tile to <paramref name="newHeight"/>.
		/// </summary>
		/// <param name="newHeight">The new height.</param>
		void SetTopLeftHeight(float newHeight);

		/// <summary>
		/// Is called every time any of the 4 corners of the tile change height
		/// </summary>
		void CornerHeightChange();

		/// <summary>
		/// Returns height at the position [<paramref name="x"/>, <paramref name="y"/>] in the XZ plane of the map.
		/// </summary>
		/// <param name="x">Position on the X axis.</param>
		/// <param name="y">Position on the Z axis.</param>
		/// <returns>The height at the position [<paramref name="x"/>, <paramref name="y"/>] in the XZ plane in the map.</returns>
		float GetHeightAt(float x, float y);

		/// <summary>
		/// Returns height at the <paramref name="position"/> in the XZ plane of the map.
		/// </summary>
		/// <param name="position">Position in the XZ plane of the map.</param>
		/// <returns>The height at the <paramref name="position"/> in the XZ plane in the map.</returns>
		float GetHeightAt(Vector2 position);

		/// <summary>
		/// Enumerates all neighboring tiles.
		/// </summary>
		/// <returns>Enumerable of all neighboring tiles.</returns>
		IEnumerable<ITile> GetNeighbours();

		/// <summary>
		/// Checks if it is possible to change the height of the corner at [<paramref name="x"/>,<paramref name="y"/>].
		/// </summary>
		/// <param name="x">The x coord of the corner.</param>
		/// <param name="y">The y coord of the corner.</param>
		/// <returns>If it is possible to change the height of the corner.</returns>
		bool CanChangeCornerHeight(int x, int y);
	}
}
