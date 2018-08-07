using System;
using System.Collections.Generic;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;
using Urho.Gui;

namespace MHUrho.WorldMap {


	/// <summary>
	/// Gets new height of the [x,y] tile corner from previous height and position of the corner
	/// Can and WILL BE CALLED MULTIPLE TIMES FOR THE SAME X,Y COORDINATES
	/// </summary>
	/// <param name="previousHeight">Previous height of the [x,y] corner</param>
	/// <param name="x">X coord of the tile corner</param>
	/// <param name="y">Y coord of the tile corner</param>
	/// <returns>New height of the [x,y] corner</returns>
	public delegate float ChangeCornerHeightDelegate(float previousHeight, int x, int y);

	/// <summary>
	/// Represents a level map, with XZ plane horizontal and Y plane vertical. <para/>
	/// The map contains tiles with topLeft corners from <see cref="IMap.Left"/> to <see cref="IMap.Right"/> in X and
	/// <see cref="IMap.Top"/> to <see cref="IMap.Bottom"/> in Y. <para/>
	/// Map is not bounded in vertical direction.
	/// </summary>
	public interface IMap {

		IPathFindAlg PathFinding { get; }

		/// <summary>
		/// Gets the coordinates of the top left corner tile of the map
		/// </summary>
		IntVector2 TopLeft { get; }

		/// <summary>
		/// Gets the coordinates of the bottom right corner tile of the map
		/// </summary>
		IntVector2 BottomRight { get; }

		/// <summary>
		/// Gets the coordinates of the top right corner tile of the map
		/// </summary>
		IntVector2 TopRight { get; }

		/// <summary>
		/// Gets the coordinates of the bottom left corner tile of the map
		/// </summary>
		IntVector2 BottomLeft { get; }

		/// <summary>
		/// Gets width of the map in tiles (and in World units as well, because tile is 1x1)
		/// </summary>
		int Width { get; }

		/// <summary>
		/// Gets length of the map in tiles (and in World units as well, because tile is 1x1)
		/// </summary>
		int Length { get; }

		/// <summary>
		/// Gets X coordinate of the left row of the map
		/// </summary>
		int Left { get; }

		/// <summary>
		/// Gets X coordinate of the right row of the map
		/// </summary>
		int Right { get; }

		/// <summary>
		/// Gets Z coordinate of the top row of the map
		/// </summary>
		int Top { get; }

		/// <summary>
		/// Gets Z coordinate of the bottom row of the map
		/// </summary>
		int Bottom { get; }

		/// <summary>
		/// Gets the level manager responsible for this level
		/// </summary>
		ILevelManager LevelManager { get; }

		/// <summary>
		/// Occurs when height of any tile in the map changes
		/// Gets called with the changed tile as arguments
		/// </summary>
		event Action<ITile> TileHeightChanged;

		/// <summary>
		/// Returns whether there exists a tile with <see cref="ITile.MapLocation"/> equal to [<paramref name="x"/>,<paramref name="z"/>]
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="z">Z coordinate</param>
		/// <returns>Returns whether there exists a tile with <see cref="ITile.MapLocation"/> equal to [<paramref name="x"/>,<paramref name="z"/>]</returns>
		bool IsInside(int x, int z);

		/// <summary>
		/// Returns whether there exists an XZ projection of a tile containing the point [<paramref name="x"/>,<paramref name="z"/>]
		/// </summary>
		/// <param name="x">X world coordinate</param>
		/// <param name="z">Z world coordinate</param>
		/// <returns>Returns whether there exists a tile containing the point [<paramref name="x"/>,<paramref name="z"/>] </returns>
		bool IsInside(float x, float z);

		/// <summary>
		/// Returns whether there exists a tile with <see cref="ITile.MapLocation"/> equal to <paramref name="point"/>
		/// </summary>
		/// <param name="point">point in the XZ plane</param>
		/// <returns>Returns whether there exists a tile with <see cref="ITile.MapLocation"/> equal to <paramref name="point"/></returns>
		bool IsInside(IntVector2 point);

		/// <summary>
		/// Returns whether there exists an XZ projection of a tile containing the point <paramref name="point"/>
		/// </summary>
		/// <param name="point">point in the XZ plane</param>
		/// <returns>Returns whether there exists a tile containing the point <paramref name="point"/></returns>
		bool IsInside(Vector2 point);

		/// <summary>
		/// Returns whether there exists a tile whose XZ projection contains the XZ projection of <paramref name="point"/>
		/// </summary>
		/// <param name="point">point, whose XZ projection you want to test</param>
		/// <returns>Returns whether there exists a tile containing the XZ projection of <paramref name="point"/></returns>
		bool IsInside(Vector3 point);

		/// <summary>
		/// Returns whether there exists a tile with <see cref="ITile.MapLocation"/> with X coord equal <paramref name="x"/>
		/// </summary>
		/// <param name="x">X coord to test</param>
		/// <returns>Returns whether there exists a tile with <see cref="ITile.MapLocation"/> with X coord equal <paramref name="x"/></returns>
		bool IsXInside(int x);

		/// <summary>
		/// Returns whether there exist a tile with <see cref="ITile.MapLocation"/> with X coord equal <paramref name="point.X"/>
		/// </summary>
		/// <param name="point">point with X coord to test</param>
		/// <returns>Returns whether there exist a tile with <see cref="ITile.MapLocation"/> with X coord equal <paramref name="point.X"/></returns>
		bool IsXInside(IntVector2 point);

		/// <summary>
		/// Returns whether there exists a tile with <see cref="ITile.MapLocation"/> with X coord equal <paramref name="z"/>
		/// </summary>
		/// <param name="z">Z coord to test</param>
		/// <returns>Returns whether there exists a tile with <see cref="ITile.MapLocation"/> with X coord equal <paramref name="z"/></returns>
		bool IsZInside(int z);

		/// <summary>
		/// Returns whether there exist a tile with <see cref="ITile.MapLocation"/> with Y coord equal <paramref name="point.Y"/>
		/// </summary>
		/// <param name="point">point with Y coord to test</param>
		/// <returns>Returns whether there exist a tile with <see cref="ITile.MapLocation"/> with Y coord equal <paramref name="point.Y"/></returns>
		bool IsZInside(IntVector2 point);

		/// <summary>
		/// Compares <paramref name="x"/> with the coords of <see cref="Left"/> and <see cref="Right"/>,
		/// returns -1 if <paramref name="x"/> is to the left of <see cref="Left"/>, 0 if inside, 1 if to the right of <see cref="Right"/>
		/// </summary>
		/// <param name="x">x coord to copare with the map boundaries</param>
		/// <returns>Returns -1 if X is to the left of <see cref="Left"/>, 0 if inside, 1 if to the right of <see cref="Right"/></returns>
		int WhereIsX(int x);

		/// <summary>
		/// Compares X coords of <paramref name="point"/> with the coords of <see cref="Left"/> and <see cref="Right"/>,
		/// returns -1 if X is to the left of <see cref="Left"/>, 0 if inside, 1 if to the right of <see cref="Right"/>
		/// </summary>
		/// <param name="point">point whose X coord to compare</param>
		/// <returns>returns -1 if X is to the left of <see cref="Left"/>, 0 if inside, 1 if to the right of <see cref="Right"/></returns>
		int WhereIsX(IntVector2 point);

		/// <summary>
		/// Compares <paramref name="z"/> with the coords of <see cref="Top"/> and <see cref="Bottom"/>,
		/// returns -1 if <paramref name="z"/> is above <see cref="Top"/>, 0 if inside, 1 if below <see cref="Bottom"/>
		/// </summary>
		/// <param name="z">y coord to copare with the map boundaries</param>
		/// <returns>Returns -1 if <paramref name="z"/> is above <see cref="Top"/>, 0 if inside, 1 if below <see cref="Bottom"/></returns>
		int WhereIsZ(int z);

		/// <summary>
		/// Compares <paramref name="point"/> Y coord with the coords of <see cref="Top"/> and <see cref="Bottom"/>,
		/// returns -1 if it is above <see cref="Top"/>, 0 if inside, 1 if below <see cref="Bottom"/>
		/// </summary>
		/// <param name="point">compares y of this point</param>
		/// <returns>Returns -1 if it is above <see cref="Top"/>, 0 if inside, 1 if below <see cref="Bottom"/></returns>
		int WhereIsZ(IntVector2 point);

		/// <summary>
		/// Returns the tile with <see cref="ITile.MapLocation"/> equal to [<paramref name="x"/>, <paramref name="z"/>]
		/// </summary>
		/// <param name="x">X coord of the <see cref="ITile.MapLocation"/></param>
		/// <param name="z">y coord of the <see cref="ITile.MapLocation"/> </param>
		/// <returns>The tile with <see cref="ITile.MapLocation"/>  equal to [<paramref name="x"/>, <paramref name="z"/>], or null if none exists</returns>
		ITile GetTileByMapLocation(int x, int z);

		/// <summary>
		/// Returns the tile with <see cref="ITile.MapLocation"/> equal to <paramref name="mapLocation"/>
		/// </summary>
		/// <param name="mapLocation">the mapLocation of the tile to get</param>
		/// <returns>The tile with <see cref="ITile.MapLocation"/>  equal to <paramref name="mapLocation"/>, or null if none exists</returns>
		ITile GetTileByMapLocation(IntVector2 mapLocation);

		/// <summary>
		/// Returns the tile with <see cref="ITile.TopLeft"/> equal to [<paramref name="x"/>,<paramref name="z"/>]
		/// </summary>
		/// <param name="x">x coord of the topLeft corner</param>
		/// <param name="z">y coord of the topLeft corner</param>
		/// <returns>Returns the tile with <see cref="ITile.TopLeft"/> equal to [<paramref name="x"/>,<paramref name="z"/>]
		/// or null if there is none ([<paramref name="x"/>, <paramref name="z"/>] is outside of the map)</returns>
		ITile GetTileByTopLeftCorner(int x, int z);

		/// <summary>
		/// Returns the tile with <see cref="ITile.TopLeft"/> equal to <paramref name="topLeftCorner"/>
		/// </summary>
		/// <param name="topLeftCorner">the coords of the top left corner</param>
		/// <returns>Returns the tile with <see cref="ITile.TopLeft"/> equal to <paramref name="topLeftCorner"/>
		/// or null if there is none (<paramref name="topLeftCorner"/> is outside of the map)</returns>
		ITile GetTileByTopLeftCorner(IntVector2 topLeftCorner);

		/// <summary>
		/// Returns the tile with <see cref="ITile.TopRight"/> equal to [<paramref name="x"/>,<paramref name="z"/>]
		/// </summary>
		/// <param name="x">x coord of the top right corner of the wanted tile</param>
		/// <param name="z">z coord of the top right corner of the wanted tile</param>
		/// <returns>Returns the tile with <see cref="ITile.TopRight"/> equal to [<paramref name="x"/>,<paramref name="z"/>]
		/// or null if there is none ([<paramref name="x"/>, <paramref name="z"/>] is outside of the map)</returns>
		ITile GetTileByTopRightCorner(int x, int z);

		/// <summary>
		/// Returns the tile with <see cref="ITile.TopRight"/> equal to <paramref name="topRightCorner"/>
		/// </summary>
		/// <param name="topRightCorner">the coords in the XZ plane of the topRight corner of the wanted tile</param>
		/// <returns>Returns the tile with <see cref="ITile.TopRight"/> equal to <paramref name="topRightCorner"/>
		/// or null if there is none (<paramref name="topRightCorner"/> is outside of the map)</returns>
		ITile GetTileByTopRightCorner(IntVector2 topRightCorner);

		/// <summary>
		/// Returns the tile with <see cref="ITile.BottomLeft"/> equal to [<paramref name="x"/>, <paramref name="z"/>]
		/// </summary>
		/// <param name="x">x coord of the bottom left corner of the wanted tile</param>
		/// <param name="z">z coord of the bottom left corner of the wanted tile</param>
		/// <returns>Returns the tile with <see cref="ITile.BottomLeft"/> equal to [<paramref name="x"/>, <paramref name="z"/>
		/// or null if there is none ([<paramref name="x"/>, <paramref name="z"/>] is outside of the map)</returns>
		ITile GetTileByBottomLeftCorner(int x, int z);

		/// <summary>
		/// Returns the tile with <see cref="ITile.BottomLeft"/> equal to <paramref name="bottomLeftCorner"/>
		/// </summary>
		/// <param name="bottomLeftCorner">the coords in the XZ plane of the bottomLeft corner of the wanted tile</param>
		/// <returns>Returns the tile with <see cref="ITile.BottomLeft"/> equal to <paramref name="bottomLeftCorner"/>
		/// or null if there is none (<paramref name="bottomLeftCorner"/> is outside of the map)</returns>
		ITile GetTileByBottomLeftCorner(IntVector2 bottomLeftCorner);

		/// <summary>
		/// Returns the tile with <see cref="ITile.BottomRight"/> equal to [<paramref name="x"/>, <paramref name="z"/>]
		/// </summary>
		/// <param name="x">x coord of the bottom right corner of the wanted tile</param>
		/// <param name="z">z coord of the bottom right corner of the wanted tile</param>
		/// <returns>Returns the tile with <see cref="ITile.BottomRight"/> equal to [<paramref name="x"/>, <paramref name="z"/>]
		/// or null if there is none ([<paramref name="x"/>, <paramref name="z"/>] is outside of the map)</returns>
		ITile GetTileByBottomRightCorner(int x, int z);

		/// <summary>
		/// Returns the tile with <see cref="ITile.BottomRight"/> equal to <paramref name="bottomRightCorner"/>
		/// </summary>
		/// <param name="bottomRightCorner">the coords in the XZ plane of the bottomRight corner of the wanted tile</param>
		/// <returns>Returns the tile with <see cref="ITile.BottomRight"/> equal to <paramref name="bottomRightCorner"/>
		/// or null if there is none (<paramref name="bottomRightCorner"/> is outside of the map)</returns>
		ITile GetTileByBottomRightCorner(IntVector2 bottomRightCorner);

		/// <summary>
		/// Returns the tile containing the projection of <paramref name="point"/> to XZ plane
		/// Ignores height, is equal to <see cref="GetContainingTile(Vector2)"/> with the X and Z members of <paramref name="point"/>
		/// </summary>
		/// <param name="point">point in the WorldSpace</param>
		/// <returns>Returns the tile containing the projection of <paramref name="point"/> to XZ plane, or null if <paramref name="point"/> is outside of the map</returns>
		ITile GetContainingTile(Vector3 point);

		/// <summary>
		/// Gets tile containing <paramref name="point"/> in the XZ plane
		/// </summary>
		/// <param name="point">The point in the XZ plane</param>
		/// <returns>The tile containing <paramref name="point"/></returns>
		ITile GetContainingTile(Vector2 point);

		/// <summary>
		/// Gets tile containing [<paramref name="x"/>, <paramref name="z"/>] in the XZ plane
		/// </summary>
		/// <param name="x">x coord of the point</param>
		/// <param name="z">z coord of the point</param>
		/// <returns>The tile containing [<paramref name="x"/>, <paramref name="z"/>] or null if [<paramref name="x"/>, <paramref name="z"/>] is outside of the map</returns>
		ITile GetContainingTile(float x, float z);

		/// <summary>
		/// Moves the rectangle defined by <paramref name="topLeft"/> and <paramref name="bottomRight"/> corners so that
		/// the whole rectangle is inside the map
		/// </summary>
		/// <param name="topLeft">top left corner of the rectangle</param>
		/// <param name="bottomRight">bottom right corner of the rectangle</param>
		/// <returns>True if it is possible to snap to map, false if it is not possible</returns>
		bool SnapToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight);

		/// <summary>
		/// Changes the size of the rectangle in XZ plane defined by <paramref name="topLeft"/> and <paramref name="bottomRight"/> corners
		/// so that no part extends outside of the map. <para/>
		/// If any side of the rectangle is outside of the map, changes just that side to the map border value. <para/>
		/// For example if <paramref name="topLeft"/>.X is less than <see cref="IMap.Left"/>, then <paramref name="topLeft"/>.X = <see cref="IMap.Left"/>
		/// </summary>
		/// <param name="topLeft">top left corner of the rectangle to squish</param>
		/// <param name="bottomRight">bottom right corner of the rectangle to squish</param>
		void SquishToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight);

		/// <summary>
		/// Finds the closest tile to <paramref name="source"/> which matches the condition specified by the <paramref name="predicate"/>
		/// This version is unbounded, so it continues outwards until it finds a match or searches the whole map.
		/// For bounded version, see <see cref="FindClosestTile(ITile, int, Predicate{ITile})"/><para/>
		/// The search starts with the source tile and continues with growing "concentric" squares.
		/// The order inside the square is implementation dependent.
		/// </summary>
		/// <param name="source">center tile, from which the search starts in squares</param>
		/// <param name="predicate">the predicate defining the condition</param>
		/// <returns></returns>
		ITile FindClosestTile(ITile source, Predicate<ITile> predicate);

		/// <summary>
		/// Finds the closest tile to <paramref name="source"/> which matches the condition specified by the <paramref name="predicate"/>
		/// This version is bounded, so the search stops with a square of size <paramref name="squareSize"/>.
		/// For unbounded version, see <see cref="FindClosestTile(ITile, Predicate{ITile})"/><para/>
		/// The search starts with the source tile and continues with growing "concentric" squares.
		/// The order inside the square is implementation dependent.
		/// </summary>
		/// <param name="source">center tile, from which the search starts in squares</param>
		/// <param name="squareSize"></param>
		/// <param name="predicate"></param>
		ITile FindClosestTile(ITile source, int squareSize, Predicate<ITile> predicate);

		/// <summary>
		/// Returns an <see cref="IEnumerable{ITile}"/> which enumerates tiles in a spiral, starting from <paramref name="center"/>
		/// </summary>
		/// <param name="center">Center tile of the spiral. Starting point of the spiral</param>
		/// <returns>Returns an <see cref="IEnumerable{ITile}"/> which enumerates the tiles in a spiral, starting from <paramref name="center"/></returns>
		IEnumerable<ITile> GetTilesInSpiral(ITile center);

		/// <summary>
		/// Returns an <see cref="IFormationController"/> that orders provided units to tiles around the <paramref name="center"/>
		/// </summary>
		/// <param name="center">Center tile the units should be ordered around</param>
		/// <returns>Returns an <see cref="IFormationController"/> that orders provided units to tiles around the <paramref name="center"/></returns>
		IFormationController GetFormationController(ITile center);

		/// <summary>
		/// Returns an enumerable that iterates over all the tiles in the given rectangle,
		/// specified by the <paramref name="topLeft"/> and <paramref name="bottomRight"/> corners
		/// Skips the parts of the rectangle outside of the map borders
		/// </summary>
		/// <param name="topLeft">top left corner of the rectangle</param>
		/// <param name="bottomRight">bottom right corner of the rectangle</param>
		/// <returns>Returns an enumerable that iterates over the tiles inside the rectangle</returns>
		IEnumerable<ITile> GetTilesInRectangle(IntVector2 topLeft, IntVector2 bottomRight);

		/// <summary>
		/// Returns an enumerable that iterates over all the tiles in the given rectangle
		/// Skips the parts of the rectangle outside of the map borders
		/// </summary>
		/// <param name="rectangle">The rectangle which the enumerable should iterate over</param>
		/// <returns>Returns an enumerable that iterates over the tiles inside the rectangle</returns>
		IEnumerable<ITile> GetTilesInRectangle(IntRect rectangle);

		//TODO: Comment
		IEnumerable<ITile> GetTilesAroundCorner(int x, int y);

		//TODO: Comment
		IEnumerable<ITile> GetTilesAroundCorner(IntVector2 cornerCoords);

		/// <summary>
		/// Returns whether the <paramref name="rayQueryResult"/> is the Map (the ray hit the map), or if it the ray hit something else
		/// </summary>
		/// <param name="rayQueryResult">RayQueryResult to check</param>
		/// <returns>Returns whether the <paramref name="rayQueryResult"/> is the Map</returns>
		bool IsRaycastToMap(RayQueryResult rayQueryResult);

		ITile RaycastToTile(List<RayQueryResult> rayQueryResults);

		ITile RaycastToTile(RayQueryResult rayQueryResult);

		Vector3? RaycastToVertexPosition(List<RayQueryResult> rayQueryResults);

		Vector3? RaycastToVertexPosition(RayQueryResult rayQueryResult);

		IntVector2? RaycastToVertex(List<RayQueryResult> rayQueryResults);

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
							ChangeCornerHeightDelegate newHeightFunction);

		float GetTerrainHeightAt(int x, int y);

		float GetTerrainHeightAt(IntVector2 position);

		float GetTerrainHeightAt(float x, float y);

		float GetTerrainHeightAt(Vector2 position);

		float GetHeightAt(float x, float y);

		float GetHeightAt(Vector2 position);

		Vector3 GetUpDirectionAt(float x, float y);

		Vector3 GetUpDirectionAt(Vector2 position);

		Vector3 GetBorderBetweenTiles(ITile tile1, ITile tile2);

		StMap Save();


		void HighlightCornerList(IEnumerable<IntVector2> corners, Color color);

		void HighlightCornerList(IEnumerable<IntVector2> corners, Func<IntVector2, Color> getColor);

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

		void ChangeHeight(IEnumerable<IntVector2> tileCorners, float heightDelta);

		void ChangeHeightTo(IEnumerable<IntVector2> tileCorners, float newHeight);

		/// <summary>
		/// Invokes <paramref name="action"/> for each tile with <see cref="ITile.MapLocation"/> 
		/// X between <paramref name="topLeft"/> and <paramref name="bottomRight"/>, including both <paramref name="topLeft"/> and <paramref name="bottomRight"/>
		/// Y between <paramref name="topLeft"/> and <paramref name="bottomRight"/>, including both <paramref name="topLeft"/> and <paramref name="bottomRight"/>
		/// </summary>
		/// <param name="topLeft">top left corner of the rectangle to iterate over</param>
		/// <param name="bottomRight">bottom right corner of the rectangle to iterate over</param>
		/// <param name="action">Action to invoke on each tile</param>
		void ForEachInRectangle(IntVector2 topLeft, IntVector2 bottomRight, Action<ITile> action);

		/// <summary>
		/// Invokes <paramref name="action"/> for each tile with <see cref="ITile.MapLocation"/> 
		/// X between <see cref="IntRect.Left"/> and <see cref="IntRect.Right"/>, including both <see cref="IntRect.Left"/> and <see cref="IntRect.Right"/>
		/// Y between <see cref="IntRect.Top"/> and <see cref="IntRect.Bottom"/>, including both <see cref="IntRect.Top"/> and <see cref="IntRect.Bottom"/>
		/// </summary>
		/// <param name="rectangle">Rectangle to iterate over</param>
		/// <param name="action">Action to invoke on each tile</param>
		void ForEachInRectangle(IntRect rectangle, Action<ITile> action);

		/// <summary>
		/// Invokes <paramref name="action"/> for each tile with the corner <paramref name="cornerCoords"/>
		/// For most corners, it will be the 4 tiles containing this corner
		/// For corners around the border, it may be less
		/// </summary>
		/// <param name="cornerCoords">Corner around which to get the tiles</param>
		/// <param name="action">Action to call for every tile around the corner</param>
		void ForEachAroundCorner(IntVector2 cornerCoords, Action<ITile> action);

		/// <summary>
		/// Gets a range target providing the ability to aim at the <paramref name="position"/>
		/// </summary>
		/// <param name="position">Position of the new range target</param>
		/// <returns>Returns a range target at the <paramref name="position"/></returns>
		IRangeTarget GetRangeTarget(Vector3 position);
	}
}