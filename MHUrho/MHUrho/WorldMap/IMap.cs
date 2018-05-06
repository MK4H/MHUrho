using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.WorldMap {
	public interface IMap {
			   
		/// <summary>
		/// Coordinates of the top left corner of the map
		/// </summary>
		IntVector2 TopLeft { get; }

		/// <summary>
		/// Coordinates of the bottom right corner of the map
		/// </summary>
		IntVector2 BottomRight { get; }

		IntVector2 TopRight { get; }

		IntVector2 BottomLeft { get; }

		int Width { get; }
		int Length { get; }

		/// <summary>
		/// X coordinate of the left row of the map
		/// </summary>
		int Left { get; }

		/// <summary>
		/// X coordinate of the right row of the map
		/// </summary>
		int Right { get; }

		/// <summary>
		/// Y coordinate of the top row of the map
		/// </summary>
		int Top { get; }

		/// <summary>
		/// Y coordinate of the bottom row of the map
		/// </summary>
		int Bottom { get; }

		bool IsInside(int x, int y);

		bool IsInside(float x, float y);

		/// <summary>
		/// Checks if the point is inside the map, which means it could be used for indexing into the map
		/// </summary>
		/// <param name="point">the point to check</param>
		/// <returns>True if it is inside, False if not</returns>
		bool IsInside(IntVector2 point);

		bool IsInside(Vector2 point);

		bool IsInside(Vector3 point);

		bool IsXInside(int x);
		bool IsXInside(IntVector2 vector);
		bool IsYInside(int y);
		bool IsYInside(IntVector2 vector);

		/// <summary>
		/// Compares x with the coords of Left and Right side, returns where the x is
		/// </summary>
		/// <param name="x">x coord to copare with the map boundaries</param>
		/// <returns>-1 if X is to the left, 0 if inside, 1 if to the right of the map rectangle</returns>
		int WhereIsX(int x);

		/// <summary>
		/// Compares x with the coords of Left and Right side, returns where the x is
		/// </summary>
		/// <param name="vector">compares x coord of this vector</param>
		/// <returns>-1 if X is to the left, 0 if inside, 1 if to the right of the map rectangle</returns>
		int WhereIsX(IntVector2 vector);

		/// <summary>
		/// Compares y with the coords of Top and Bottom side, returns where the y is
		/// </summary>
		/// <param name="y">y coord to copare with the map boundaries</param>
		/// <returns>-1 if Y is above, 0 if inside, 1 if below the map rectangle</returns>
		int WhereIsY(int y);

		/// <summary>
		/// Compares y with the coords of Top and Bottom side, returns where the y is
		/// </summary>
		/// <param name="vector">compares y of this vector</param>
		/// <returns>-1 if Y is above, 0 if inside, 1 if below the map rectangle</returns>
		int WhereIsY(IntVector2 vector);

		ITile GetTileByMapLocation(int x, int y);

		ITile GetTileByMapLocation(IntVector2 mapLocation);

		ITile GetTileByTopLeftCorner(int x, int y);

		ITile GetTileByTopLeftCorner(IntVector2 topLeftCorner);

		ITile GetTileByTopRightCorner(int x, int y);

		ITile GetTileByTopRightCorner(IntVector2 topRightCorner);

		ITile GetTileByBottomLeftCorner(int x, int y);

		ITile GetTileByBottomLeftCorner(IntVector2 bottomLeftCorner);

		ITile GetTileByBottomRightCorner(int x, int y);

		ITile GetTileByBottomRightCorner(IntVector2 bottomRightCorner);

		ITile GetContainingTile(Vector3 point);

		/// <summary>
		/// Gets tile containing <paramref name="point"/> in the XZ plane
		/// </summary>
		/// <param name="point">The point in the XZ plane</param>
		/// <returns>The tile containing <paramref name="point"/></returns>
		ITile GetContainingTile(Vector2 point);


		/// <summary>
		/// Moves the rectangle defined by topLeft and bottomRight corners so that
		/// the whole rectangle is inside the map
		/// </summary>
		/// <param name="topLeft">top left corner of the rectangle</param>
		/// <param name="bottomRight">bottom right corner of the rectangle</param>
		/// <returns>True if it is possible to snap to map, false if it is not possible</returns>
		bool SnapToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight);

		void SquishToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight);
		ITile FindClosestEmptyTile(ITile closestTo);

		float GetTerrainHeightAt(int x, int y);

		float GetTerrainHeightAt(IntVector2 position);

		float GetTerrainHeightAt(float x, float y);

		float GetTerrainHeightAt(Vector2 position);

		Vector3 GetBorderBetweenTiles(ITile tile1, ITile tile2);

		StMap Save();

		/// <summary>
		/// Highlights rectangle of size <paramref name="size"/> with tile 
		/// <paramref name="center"/> at its center
		/// Squishes the rectangle to map if it does not fit
		/// </summary>
		/// <param name="center">Tile at the center of the rectangle</param>
		/// <param name="size">Size of the highlighted rectangle</param>
		void HighlightArea(ITile center, IntVector2 size, HighlightMode mode, Color color);

		void HighlightArea(IntVector2 topLeft, IntVector2 bottomRight, HighlightMode mode, Color color);

		/// <summary>
		/// Hides highlight displayed by HighlightArea <see cref="HighlightArea(IntVector2, IntVector2, HighlightMode, Color)"/> or <see cref="HighlightArea(ITile, IntVector2, HighlightMode, Color)"/>
		/// </summary>
		void DisableHighlight();


	}
}