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
		/// Units inside the tile
		/// </summary>
		IReadOnlyList<IUnit> Units { get; }

		/// <summary>
		/// Building that owns this tile
		/// </summary>
		IBuilding Building { get; }

		/// <summary>
		/// Tile type of this tile
		/// </summary>
		TileType Type { get; }

		/// <summary>
		/// The area in the map this tile represents
		/// </summary>
		IntRect MapArea { get; }

		/// <summary>
		/// Index into map matrix
		/// </summary>
		IntVector2 MapLocation { get; }

		IntVector2 TopLeft { get; }

		IntVector2 TopRight { get; }

		IntVector2 BottomLeft { get; }

		IntVector2 BottomRight { get; }

		/// <summary>
		/// Coords of the center of the tile
		/// </summary>
		Vector2 Center { get; }

		/// <summary>
		/// 3D coords of the center of the tile
		/// </summary>
		Vector3 Center3 { get; }

		Vector3 TopLeft3 { get; }

		Vector3 TopRight3 { get; }

		Vector3 BottomLeft3 { get; }

		Vector3 BottomRight3 { get; }

		/// <summary>
		/// Heigth of the top left corner of the tile
		/// </summary>
		float TopLeftHeight { get; }

		float TopRightHeight { get; }

		float BottomLeftHeight { get; }

		float BottomRightHeight { get; }

		IMap Map { get; }

		/// <summary>
		/// Continues loading by connecting references
		/// </summary>
		void ConnectReferences(ILevelManager level);

		void FinishLoading();

		void AddUnit(IUnit unit);

		/// <summary>
		/// Removes a unit from this tile
		/// </summary>
		/// <param name="unit">the unit to remove</param>
		void RemoveUnit(IUnit unit);

		void AddBuilding(IBuilding building);

		void RemoveBuilding(IBuilding building);

		StTile Save();

		/// <summary>
		/// Should only be called by Map class (how i wish C# had friend functions)
		/// </summary>
		/// <param name="newType"></param>
		void ChangeType(TileType newType);

		/// <summary>
		/// Called by the Map to change height
		/// 
		/// If you want to change height, go through <see cref="Map.ChangeTileHeight(ITile, float)"/>
		/// </summary>
		/// <param name="heightDelta"></param>

		void ChangeTopLeftHeight(float heightDelta);

		/// <summary>
		/// Sets the height of the top left corner of the tile to <paramref name="newHeight"/>
		/// </summary>
		/// <param name="newHeight">the height to set</param>
		void SetTopLeftHeight(float newHeight);

		/// <summary>
		/// Is called every time any of the 4 corners of the tile change height
		/// </summary>
		void CornerHeightChange();

		float GetHeightAt(float x, float y);

		float GetHeightAt(Vector2 position);

		IEnumerable<ITile> GetNeighbours();
	}
}
