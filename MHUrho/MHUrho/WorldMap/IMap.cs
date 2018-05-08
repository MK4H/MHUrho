using System;
using System.Collections.Generic;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;
using Urho.Gui;

namespace MHUrho.WorldMap {

	/// <summary>
	/// Gets new height of the top left corner of the tile [x,y] from previous height and position of this corner
	/// Can and WILL BE CALLED MULTIPLE TIMES FOR THE SAME X,Y COORDINATES
	/// 
	/// Used in a rectangle, for 3x3 rectangle of tiles called 4x4 times, to change even the bottom and right sides
	/// of the bottom and right tiles
	/// 
	/// So for 3x3 rectangle with top left [0,0] it is called even at [3,0],[0,3] and [3,3]
	/// </summary>
	/// <param name="previousHeight">Previous height of the tile corner</param>
	/// <param name="x">X coord of the tile corner</param>
	/// <param name="y">Y coord of the tile corner</param>
	/// <returns>New height of the tile top left corner</returns>
	public delegate float ChangeTileHeightDelegate(float previousHeight, int x, int y);



	/// <summary>
	/// Gets new height of the [x,y] tile corner from previous height and position of the corner
	/// Can and WILL BE CALLED MULTIPLE TIMES FOR THE SAME X,Y COORDINATES
	/// </summary>
	/// <param name="previousHeight">Previous height of the [x,y] corner</param>
	/// <param name="x">X coord of the tile corner</param>
	/// <param name="y">Y coord of the tile corner</param>
	/// <returns>New height of the [x,y] corner</returns>
	public delegate float ChangeCornerHeightDelegate(float previousHeight, int x, int y);

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

		ILevelManager LevelManager { get; }

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

		ITile GetContainingTile(float x, float y);

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

		ITile FindClosestTile(ITile source, Predicate<ITile> predicate);

		ITile FindClosestTile(ITile source, int squareSize, Predicate<ITile> predicate);

		IEnumerable<ITile> GetTilesInSpiral(ITile center);

		//TODO: Maybe remove from this interface
		ITile RaycastToTile(List<RayQueryResult> rayQueryResults);
		//TODO: Maybe remove from this interface
		ITile RaycastToTile(RayQueryResult rayQueryResult);
		//TODO: Maybe remove from this interface
		Vector3? RaycastToVertexPosition(List<RayQueryResult> rayQueryResults);
		//TODO: Maybe remove from this interface
		Vector3? RaycastToVertexPosition(RayQueryResult rayQueryResult);
		//TODO: Maybe remove from this interface
		IntVector2? RaycastToVertex(List<RayQueryResult> rayQueryResults);
		//TODO: Maybe remove from this interface
		IntVector2? RaycastToVertex(RayQueryResult rayQueryResult);

		void ChangeTileType(ITile tile, TileType newType);

		void ChangeTileType(ITile centerTile, IntVector2 rectangleSize, TileType newType);

		/// <summary>
		/// For fast relative height changing in response to every mouse movement
		/// </summary>
		/// <param name="centerTile">center tile of the rectangle</param>
		/// <param name="rectangleSize">Size of the rectangle in which the height changes</param>
		/// <param name="heightDelta">By how much should the hight change</param>
		void ChangeTileHeight(ITile centerTile, IntVector2 rectangleSize, float heightDelta);

		void ChangeTileHeight(ITile centerTile,
							IntVector2 rectangleSize,
							ChangeTileHeightDelegate newHeightFunction);

		float GetTerrainHeightAt(int x, int y);

		float GetTerrainHeightAt(IntVector2 position);

		float GetTerrainHeightAt(float x, float y);

		float GetTerrainHeightAt(Vector2 position);

		float GetHeightAt(float x, float y);

		Vector3 GetUpDirectionAt(float x, float y);

		Vector3 GetUpDirectionAt(Vector2 position);

		Vector3 GetBorderBetweenTiles(ITile tile1, ITile tile2);

		StMap Save();


		void HighlightTileList(IEnumerable<ITile> tiles, Func<ITile, Color> getColor);

		void HighlightTileList(IEnumerable<ITile> tiles, Color color);

		void HighlightRectangle(IntVector2 topLeft, IntVector2 bottomRight, Func<ITile, Color> getColor);
		
		/// <summary>
		/// Highlights rectangle of size <paramref name="size"/> with tile 
		/// <paramref name="center"/> at its center
		/// Squishes the rectangle to map if it does not fit
		/// </summary>
		/// <param name="center">Tile at the center of the rectangle</param>
		/// <param name="size">Size of the highlighted rectangle</param>
		void HighlightRectangle(ITile center, IntVector2 size, Func<ITile, Color> getColor);

		void HighlightRectangle(IntRect rectangle, Func<ITile, Color> getColor);

		void HighlightRectangle(IntVector2 topLeft, IntVector2 bottomRight, Color color);

		void HighlightRectangle(ITile center, IntVector2 size, Color color);

		void HighlightRectangle(IntRect rectangle, Color color);

		void HighlightRectangleBorder(IntVector2 topLeft, IntVector2 bottomRight, Color color);

		void HighlightRectangleBorder(ITile center, IntVector2 size, Color color);

		void HighlightRectangleBorder(IntRect rectangle, Color color);

		/// <summary>
		/// Hides highlight displayed by HighlightArea <see cref="HighlightArea(IntVector2, IntVector2, HighlightMode, Color)"/> or <see cref="HighlightArea(ITile, IntVector2, HighlightMode, Color)"/>
		/// </summary>
		void DisableHighlight();

		void ChangeHeight(List<IntVector2> tileCorners, float heightDelta);

		void ForEachInRectangle(IntVector2 topLeft, IntVector2 bottomRight, Action<ITile> action);

		void ForEachInRectangle(IntRect rectangle, Action<ITile> action);

		void ForEachAroundCorner(IntVector2 cornerCoords, Action<ITile> action);

		IRangeTarget GetRangeTarget(Vector3 position);
	}
}