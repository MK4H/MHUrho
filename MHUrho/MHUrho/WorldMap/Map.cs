using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MHUrho.Control;
using Urho;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.UnitComponents;


namespace MHUrho.WorldMap
{
	public partial class Map : IMap, IDisposable {

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

		internal class Loader : ILoader {
			
			public Map Map { get; private set; }


			/// <summary>
			/// Loads map data from storedMap
			/// 
			/// After everything in the level Started loading,
			/// Next step is to call ConnectReferences() to connect references
			/// 
			/// Last step is to FinishLoading, after all references are connected
			/// </summary>
			/// <param name="mapNode">Scene node of the map</param>
			/// <param name="storedMap">Protocol Buffers class containing stored map</param>
			/// <returns>Map with loaded data, but without connected references and without geometry</returns>
			public static Loader StartLoading(LevelManager level, Node mapNode, StMap storedMap) {
				var loader = new Loader();
				loader.Load(level, mapNode, storedMap);
				return loader;
			}

			public void ConnectReferences(LevelManager level) {
				foreach (var tile in Map.tiles) {
					tile.ConnectReferences(level);
				}
			}

			/// <summary>
			/// Builds geometry and releases stored data
			/// </summary>
			public void FinishLoading() {
				foreach (var tile in Map.tiles) {
					tile.FinishLoading();
				}

				Map.BuildGeometry();
			}

			private void Load(LevelManager level, Node mapNode, StMap storedMap) {
				Map = new Map(mapNode, storedMap);
				Map.levelManager = level;
				var tiles = storedMap.Tiles.GetEnumerator();
				var borderTiles = storedMap.BorderTiles.GetEnumerator();

				try {

					for (int y = 0; y < Map.LengthWithBorders; y++) {
						for (int x = 0; x < Map.WidthWithBorders; x++) {
							ITile newTile;
							if (Map.IsBorder(x, y)) {
								if (!borderTiles.MoveNext()) {
									//TODO: Exception
									throw new Exception("Corrupted save file");
								}

								newTile = new BorderTile(borderTiles.Current, Map);
							}
							else {
								if (!tiles.MoveNext()) {
									//TODO: Exception
									throw new Exception("Corrupted save file");
								}

								newTile = Tile.StartLoading(tiles.Current, Map);
							}

							Map.tiles[Map.GetTileIndex(x, y)] = newTile;
						}
					}
				}
				catch (IndexOutOfRangeException e) {
					//TODO: Logging
					throw;
				}
				catch (NullReferenceException e) {
					//TODO: Logging
					throw;
				}
				finally {
					tiles?.Dispose();
					borderTiles?.Dispose();
				}

			}
		}

		private class BorderTile : ITile {
			Building ITile.Building => throw new InvalidOperationException("Cannot add building to Border tile");

			Unit ITile.Unit => throw new InvalidOperationException("Cannot add unit to Border tile");

			IReadOnlyList<Unit> ITile.PassingUnits => throw new InvalidOperationException("Cannot add unit to Border tile");

			float ITile.MovementSpeedModifier => throw new InvalidOperationException("Cannot move through Border tile");

			public TileType Type { get; private set; }


			public IntRect MapArea { get; private set; }

			public IntVector2 MapLocation => TopLeft;

			/// <summary>
			/// Location in the Map matrix
			/// </summary>
			public IntVector2 TopLeft => new IntVector2(MapArea.Left, MapArea.Top);

			public IntVector2 TopRight => new IntVector2(MapArea.Right, MapArea.Top);

			public IntVector2 BottomLeft => new IntVector2(MapArea.Left, MapArea.Bottom);

			public IntVector2 BottomRight => new IntVector2(MapArea.Right, MapArea.Bottom);

			public Vector2 Center => new Vector2(MapArea.Left + 0.5f, MapArea.Top + 0.5f);

			public Vector3 Center3 => new Vector3(Center.X, Map.GetHeightAt(Center), Center.Y);

			public Vector3 TopLeft3 => new Vector3(MapArea.Left, Map.GetHeightAt(MapArea.Left, MapArea.Top), MapArea.Top);

			public Vector3 TopRight3 => new Vector3(MapArea.Right, Map.GetHeightAt(MapArea.Right, MapArea.Top), MapArea.Top);

			public Vector3 BottomLeft3 => new Vector3(MapArea.Left, Map.GetHeightAt(MapArea.Left, MapArea.Bottom), MapArea.Bottom);

			public Vector3 BottomRight3 => new Vector3(MapArea.Right, Map.GetHeightAt(MapArea.Right, MapArea.Bottom), MapArea.Bottom);

			public Map Map { get; private set; }

			public float Height {
				get => TopLeftHeight;
				private set => TopLeftHeight = value;
			}

			//TODO: WHEN SETTING THESE CORNERHEIGHTCHANGE is NOT called
			public float TopLeftHeight { get; set; }
			public float TopRightHeight { get; set; }
			public float BottomLeftHeight { get; set; }
			public float BottomRightHeight { get; set; }

			public BorderType BorderType { get; private set; }

			

			private StBorderTile storage;

			public void ConnectReferences(ILevelManager level) {
				Type = PackageManager.Instance.ActiveGame.GetTileType(storage.TileTypeID);
			}

			public void FinishLoading() {
				storage = null;
			}

			void ITile.AddPassingUnit(Unit unit) {
				throw new InvalidOperationException("Cannot add unit to Border tile");
			}

			bool ITile.TryAddOwningUnit(Unit unit) {
				throw new InvalidOperationException("Cannot add unit to Border tile");
			}

			void ITile.RemoveUnit(Unit unit) {
				throw new InvalidOperationException("Cannot remove unit from Border tile");
			}

			public void AddBuilding(Building building) {
				throw new InvalidOperationException("Cannot add building to Border tile");
			}

			public void RemoveBuilding(Building building) {
				throw new InvalidOperationException("Cannot remove building from Border tile");
			}

			IEnumerable<Unit> ITile.GetAllUnits() {
				return Enumerable.Empty<Unit>();
			}

			StTile ITile.Save() {
				throw new InvalidOperationException("Cannot save BorderTile as a tile");
			}

			public StBorderTile Save() {
				var stBorderTile = new StBorderTile();
				stBorderTile.TopLeftPosition = TopLeft.ToStIntVector2();
				stBorderTile.TileTypeID = Type.ID;
				stBorderTile.TopLeftHeight = TopLeftHeight;
				stBorderTile.TopRightHeight = TopRightHeight;
				stBorderTile.BotLeftHeight = BottomLeftHeight;
				stBorderTile.BotRightHeight = BottomRightHeight;

				return stBorderTile;
			}

			public void ChangeType(TileType newType) {
				Type = newType;
			}

			public void ChangeTopLeftHeight(float heightDelta, bool signalNeighbours = true) {
				Height += heightDelta;
				if (signalNeighbours) {
					Map.ForEachAroundCorner(MapLocation, (tile) => { tile.CornerHeightChange(); });
				}
			}

			public void SetTopLeftHeight(float newHeight, bool signalNeighbours = true) {
				Height = newHeight;
				if (signalNeighbours) {
					Map.ForEachAroundCorner(MapLocation, (tile) => { tile.CornerHeightChange(); });
				}
			}

			public void CornerHeightChange() {
				//Nothing
			}

			public Path GetPath(Unit forUnit) {
				return null;
			}

			public BorderTile(StBorderTile stBorderTile, Map map) {
				this.storage = stBorderTile;
				this.MapArea = new IntRect(stBorderTile.TopLeftPosition.X, 
										   stBorderTile.TopLeftPosition.Y, 
										   stBorderTile.TopLeftPosition.X + 1, 
										   stBorderTile.TopLeftPosition.Y + 1);
				this.TopLeftHeight = stBorderTile.TopLeftHeight;
				this.TopRightHeight = stBorderTile.TopRightHeight;
				this.BottomLeftHeight = stBorderTile.BotLeftHeight;
				this.BottomRightHeight = stBorderTile.BotRightHeight;
				this.Map = map;
				BorderType = map.GetBorderType(this.MapLocation);
			}

			public BorderTile(int x, int y, TileType tileType, BorderType borderType, Map map) {
				MapArea = new IntRect(x, y, x + 1, y + 1);
				this.Type = tileType;
				this.BorderType = borderType;
				this.TopLeftHeight = 0;
				this.TopRightHeight = 0;
				this.BottomLeftHeight = 0;
				this.BottomRightHeight = 0;
				this.Map = map;
			}
		}


		public IPathFindAlg PathFinding { get; private set; }

		/// <summary>
		/// Coordinates of the top left tile of the playing map
		/// </summary>
		public IntVector2 TopLeft { get; private set; }


		/// <summary>
		/// Coordinates of the bottom right tile of the playing map
		/// </summary>
		public IntVector2 BottomRight { get; private set; }

		public IntVector2 TopRight => new IntVector2(Right, Top);

		public IntVector2 BottomLeft => new IntVector2(Left, Bottom);

		/// <summary>
		/// Width of the whole playing field
		/// </summary>
		public int Width => Right - Left + 1;
		/// <summary>
		/// Length of the whole playing field
		/// </summary>
		public int Length => Bottom - Top + 1;

		/// <summary>
		/// X coordinate of the left row of the map
		/// </summary>
		public int Left => TopLeft.X;
		/// <summary>
		/// X coordinate of the right row of the map
		/// </summary>
		public int Right => BottomRight.X;

		/// <summary>
		/// Y coordinate of the top row of the map
		/// </summary>
		public int Top => TopLeft.Y;
		/// <summary>
		/// Y coordinate of the bottom row of the map
		/// </summary>
		public int Bottom => BottomRight.Y;

		public IntRect? HighlightedArea { get; private set; }

		public ILevelManager LevelManager => levelManager;

		private readonly ITile[] tiles;

		private readonly Node node;

		private MapGraphics graphics;

		private LevelManager levelManager;

		private Dictionary<Vector3, MapRangeTarget> mapRangeTargets;

		/// <summary>
		/// Width of the whole map with borders included
		/// </summary>
		private int WidthWithBorders => Width + 2;
		/// <summary>
		/// Length of the whole map with the borders included
		/// </summary>
		private int LengthWithBorders => Length + 2;

		private int LeftWithBorders => Left - 1;

		private int RightWithBorders => Right + 1;

		private int TopWithBorders => Top - 1;

		private int BottomWithBorders => Bottom + 1;

		

		/// <summary>
		/// Creates default map at height 0 with all tiles with the default type
		/// </summary>
		/// <param name="mapNode">Node to connect the map to</param>
		/// <param name="size">Size of the playing field, excluding the borders</param>
		/// <returns>Fully created map</returns>
		internal static Map CreateDefaultMap(LevelManager level, Node mapNode, IntVector2 size) {
			Map newMap = new Map(mapNode, size.X, size.Y);
			newMap.levelManager = level;

			TileType defaultTileType = PackageManager.Instance.ActiveGame.DefaultTileType;

			for (int i = 0; i < newMap.tiles.Length; i++) {
				IntVector2 tilePosition = new IntVector2(i % newMap.WidthWithBorders, i / newMap.LengthWithBorders);
				if (newMap.IsBorder(tilePosition)) {
					BorderType borderType = newMap.GetBorderType(tilePosition.X, tilePosition.Y);

					Debug.Assert(borderType != BorderType.None,
								 "Error in implementation of IsBorder or GetBorderType");

					newMap.tiles[i] = new BorderTile(tilePosition.X, tilePosition.Y, defaultTileType, borderType, newMap);
				}
				else {
					newMap.tiles[i] = new Tile(tilePosition.X, tilePosition.Y, defaultTileType, newMap);
				}
			}

			newMap.BuildGeometry();
			return newMap;
		}

		public StMap Save() {
			var storedMap = new StMap();
			var stSize = new StIntVector2();
			stSize.X = Width;
			stSize.Y = Length;
			storedMap.Size = stSize;

			var storedTiles = storedMap.Tiles;
			var storedBorderTiles = storedMap.BorderTiles;

			foreach (var tile in tiles) {
				if (IsBorder(tile.MapLocation)) {
					storedBorderTiles.Add(((BorderTile)tile).Save());
				}
				else {
					storedTiles.Add(tile.Save());
				} 
			}

			return storedMap;
		}

		protected Map(Node mapNode, StMap storedMap)
			:this(mapNode, storedMap.Size.X, storedMap.Size.Y) {

		}

		/// <summary>
		/// Creates map connected to mapNode with the PLAYING FIELD of width <paramref name="width"/> and length <paramref name="length"/>
		/// </summary>
		/// <param name="mapNode"></param>
		/// <param name="width">Width of the playing field without borders</param>
		/// <param name="length">Length of the playing field without borders</param>
		protected Map(Node mapNode, int width, int length) {
			this.node = mapNode;
			this.TopLeft = new IntVector2(1, 1);
			this.BottomRight = TopLeft + new IntVector2(width - 1, length - 1);
			this.mapRangeTargets = new Dictionary<Vector3, MapRangeTarget>();

			this.tiles = new ITile[WidthWithBorders *  LengthWithBorders];
			this.PathFinding = new AStar(this);
		}

		public bool IsInside(int x, int y) {
			return Left <= x && x <= Right && Top <= y && y <= Bottom;
		}

		public bool IsInside(float x, float y) {
			return Left <= x && x < Left + Width && Top <= y && y < Top + Length;
		}

		/// <summary>
		/// Checks if the point is inside the playfield, which means it could be used for indexing into the map
		/// </summary>
		/// <param name="point">the point to check</param>
		/// <returns>True if it is inside, False if not</returns>
		public bool IsInside(IntVector2 point) {
			return IsInside(point.X, point.Y);
		}

		/// <summary>
		/// Checks if <paramref name="point"/> is inside the map borders in the XZ plane
		/// </summary>
		/// <param name="point">position to check</param>
		/// <returns>true if inside, false if outside</returns>
		public bool IsInside(Vector2 point) {
			return IsInside(point.X, point.Y);
		}

		/// <summary>
		/// Checks if <paramref name="point"/> is inside the map borders and above terrain
		/// </summary>
		/// <param name="point">position to check</param>
		/// <returns>true if inside, false if outside</returns>
		public bool IsInside(Vector3 point) {
			return IsInside(point.X, point.Z) && GetHeightAt(point.X, point.Z) <= point.Y;
		}

		public bool IsXInside(int x) {
			return Left <= x && x <= Right;
		}

		public bool IsXInside(IntVector2 vector) {
			return IsXInside(vector.X);
		}

		public bool IsYInside(int y) {
			return Top <= y && y <= Bottom;
		}

		public bool IsYInside(IntVector2 vector) {
			return IsYInside(vector.Y);
		}

		/// <summary>
		/// Compares x with the coords of Left and Right side, returns where the x is
		/// </summary>
		/// <param name="x">x coord to copare with the map boundaries</param>
		/// <returns>-1 if X is to the left, 0 if inside, 1 if to the right of the map rectangle</returns>
		public int WhereIsX(int x) {
			if (x < Left) {
				return -1;
			}

			if (x > Right) {
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// Compares x with the coords of Left and Right side, returns where the x is
		/// </summary>
		/// <param name="vector">compares x coord of this vector</param>
		/// <returns>-1 if X is to the left, 0 if inside, 1 if to the right of the map rectangle</returns>
		public int WhereIsX(IntVector2 vector) {
			return WhereIsX(vector.X);
		}


		/// <summary>
		/// Compares y with the coords of Top and Bottom side, returns where the y is
		/// </summary>
		/// <param name="y">y coord to copare with the map boundaries</param>
		/// <returns>-1 if Y is above, 0 if inside, 1 if below the map rectangle</returns>
		public int WhereIsY(int y) {
			if (y < Top) {
				return -1;
			}

			if (y > Bottom) {
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// Compares y with the coords of Top and Bottom side, returns where the y is
		/// </summary>
		/// <param name="vector">compares y of this vector</param>
		/// <returns>-1 if Y is above, 0 if inside, 1 if below the map rectangle</returns>
		public int WhereIsY(IntVector2 vector) {
			return WhereIsY(vector.Y);
		}

		public ITile GetTileByMapLocation(int x, int y) {
			return GetTileByTopLeftCorner(x, y);
		}

		public ITile GetTileByMapLocation(IntVector2 mapLocation) {
			return GetTileByTopLeftCorner(mapLocation);
		}

		public ITile GetTileByTopLeftCorner(int x, int y) {
			return IsInside(x, y) ? tiles[GetTileIndex(x, y)] : null;
		}

		public ITile GetTileByTopLeftCorner(IntVector2 topLeftCorner) {
			return GetTileByTopLeftCorner(topLeftCorner.X, topLeftCorner.Y);
		}

		public ITile GetTileByTopRightCorner(int x, int y) {
			return GetTileByTopLeftCorner(x - 1, y);
		}

		public ITile GetTileByTopRightCorner(IntVector2 topRightCorner) {
			return GetTileByTopRightCorner(topRightCorner.X, topRightCorner.Y);
		}

		public ITile GetTileByBottomLeftCorner(int x, int y) {
			return GetTileByTopLeftCorner(x, y - 1);
		}

		public ITile GetTileByBottomLeftCorner(IntVector2 bottomLeftCorner) {
			return GetTileByBottomLeftCorner(bottomLeftCorner.X, bottomLeftCorner.Y);
		}

		public ITile GetTileByBottomRightCorner(int x, int y) {
			return GetTileByTopLeftCorner(x - 1, y - 1);
		}

		public ITile GetTileByBottomRightCorner(IntVector2 bottomRightCorner) {
			return GetTileByBottomRightCorner(bottomRightCorner.X, bottomRightCorner.Y);
		}

		/// <summary>
		/// Moves the rectangle defined by topLeft and bottomRight corners so that
		/// the whole rectangle is inside the map
		/// </summary>
		/// <param name="topLeft">top left corner of the rectangle</param>
		/// <param name="bottomRight">bottom right corner of the rectangle</param>
		/// <returns>True if it is possible to snap to map, false if it is not possible</returns>
		public bool SnapToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight) {

			if ( IsInside(topLeft) && IsInside(bottomRight)) {
				return true;
			}
			int recWidth = bottomRight.X - topLeft.X;
			int recLength = bottomRight.Y - topLeft.Y;

			bool fits = true;
			int where;

			if (recWidth > Width) {
				//Rectangle is wider than the map, center it on the map
				int diff = recWidth - Width;
				topLeft.X = diff / 2;
				bottomRight.X = topLeft.X + recWidth;

				fits = false;
			}
			else if ((where = WhereIsX(topLeft.X)) != 0) {
				int dist = (where == -1) ? this.TopLeft.X - topLeft.X : this.BottomRight.X - bottomRight.X;
				topLeft.X += dist;
				bottomRight.X += dist;
			}

			if (recLength > Length) {
				//Rectangle is wider than the map, center it on the map
				int diff = recLength - Length;
				topLeft.Y = diff / 2;
				bottomRight.Y = topLeft.Y + recLength;

				fits = false;
			}
			else if ((where = WhereIsY(topLeft.Y)) != 0) {
				int dist = (where == -1) ? this.TopLeft.Y - topLeft.Y : this.BottomRight.Y - bottomRight.Y;
				topLeft.Y += dist;
				bottomRight.Y += dist;
			}

			return fits;
		}

		public void SquishToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight) {
			if (IsInside(topLeft) && IsInside(bottomRight)) {
				return;
			}

			switch (WhereIsX(topLeft.X)) {
				case 0: // NOTHING
					break;
				case -1:
					topLeft.X = Left;
					break;
				case 1:
					topLeft.X = Right;
					break;
				default:
					//TODO: Exceptions
					throw new Exception("Switch not updated for the current implementation of WhereIsX");
			}

			switch (WhereIsX(bottomRight.X)) {
				case 0: // NOTHING
					break;
				case -1:
					bottomRight.X = Left;
					break;
				case 1:
					bottomRight.X = Right;
					break;
				default:
					//TODO: Exceptions
					throw new Exception("Switch not updated for the current implementation of WhereIsX");
			}

			switch (WhereIsY(topLeft.Y)) {
				case 0:
					break;
				case -1:
					topLeft.Y = Top;
					break;
				case 1:
					topLeft.Y = Bottom;
					break;
				default:
					//TODO: Exceptions
					throw new Exception("Switch not updated for the current implementation of WhereIsY");
			}

			switch (WhereIsY(bottomRight.Y)) {
				case 0:
					break;
				case -1:
					bottomRight.Y = Top;
					break;
				case 1:
					bottomRight.Y = Bottom;
					break;
				default:
					//TODO: Exceptions
					throw new Exception("Switch not updated for the current implementation of WhereIsY");
			}
		}

		public ITile FindClosestEmptyTile(ITile closestTo) {
			int dist = 1;
			while (true) {
				for (int dx = -dist; dx < dist + 1; dx++) {
					for (int dy = -dist; dy < dist + 1; dy++) {
						IntVector2 pos = closestTo.MapLocation;
						pos.X += dx;
						pos.Y += dy;
						if (!IsInside(pos)) {
							continue;
						}

						if (GetTileByTopLeftCorner(pos).Unit == null) {
							return GetTileByTopLeftCorner(pos);
						}
					}
				}
				dist++;
				//TODO: Cutoff
			}
		}

		public ITile RaycastToTile(List<RayQueryResult> rayQueryResults) {
			return graphics.RaycastToTile(rayQueryResults);
		}

		public ITile RaycastToTile(RayQueryResult rayQueryResult) {
			return graphics.RaycastToTile(rayQueryResult);
		}

		/// <summary>
		/// Gets the position of the closest tile corner to the point clicked
		/// </summary>
		/// <param name="rayQueryResults">result of raycast to process</param>
		/// <returns>Position of the closest tile corner to click or null if the clicked thing was not map</returns>
		public Vector3? RaycastToVertexPosition(List<RayQueryResult> rayQueryResults) {
			return graphics.RaycastToVertex(rayQueryResults);
		}

		public Vector3? RaycastToVertexPosition(RayQueryResult rayQueryResult) {
			return graphics.RaycastToVertex(rayQueryResult);
		}

		/// <summary>
		/// Gets map matrix coords of the tile corner
		/// </summary>
		/// <param name="rayQueryResults"></param>
		/// <returns></returns>
		public IntVector2? RaycastToVertex(List<RayQueryResult> rayQueryResults) {
			var cornerPosition = RaycastToVertexPosition(rayQueryResults);

			if (!cornerPosition.HasValue) {
				return null;
			}

			return new IntVector2((int)cornerPosition.Value.X, (int)cornerPosition.Value.Z);
		}

		public IntVector2? RaycastToVertex(RayQueryResult rayQueryResult) {
			var cornerPosition = RaycastToVertexPosition(rayQueryResult);

			if (!cornerPosition.HasValue) {
				return null;
			}

			return new IntVector2((int)cornerPosition.Value.X, (int)cornerPosition.Value.Z);
		}

		public void ChangeTileType(ITile tile, TileType newType) {
			if (tile.Type == newType) {
				return;
			}

			tile.ChangeType(newType);
			graphics.ChangeTileType(tile.MapLocation, newType);
		}

		public void ChangeTileType(ITile centerTile, IntVector2 rectangleSize, TileType newType) {
			IntVector2 topLeft = centerTile.TopLeft - (rectangleSize / 2);
			IntVector2 bottomRight = topLeft + (rectangleSize - new IntVector2(1,1));
			SquishToMap(ref topLeft, ref bottomRight);

			ForEachInRectangle(topLeft, bottomRight, (tile) => { tile.ChangeType(newType); });
			graphics.ChangeTileType(topLeft, bottomRight, newType);
		}

		public void ChangeTileHeight(ITile tile, float heightDelta) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// For fast relative height changing in response to every mouse movement
		/// </summary>
		/// <param name="centerTile">center tile of the rectangle</param>
		/// <param name="rectangleSize">Size of the rectangle in which the height changes</param>
		/// <param name="heightDelta">By how much should the hight change</param>
		public void ChangeTileHeight(ITile centerTile, IntVector2 rectangleSize, float heightDelta) {
			IntVector2 topLeft = centerTile.TopLeft - (rectangleSize / 2);
			IntVector2 bottomRight = topLeft + (rectangleSize - new IntVector2(1, 1));
			SquishToMap(ref topLeft, ref bottomRight);

			//Make the rectangle larger, to include the surrounding tiles
			// I need to change height of their side or corner, to make the map continuous
			topLeft -= new IntVector2(1, 1);
			bottomRight += new IntVector2(1, 1);

			for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
				for (int x = topLeft.X; x <= bottomRight.X; x++) {
					if (x == topLeft.X || x == bottomRight.X || y == topLeft.Y || y == bottomRight.Y) {
						//Its a side tile of this rectangle
						if (IsBorder(x, y)) {
							if (x == topLeft.X) {
								if (y == topLeft.Y) {
									//Top left corner tile 
									((BorderTile)GetTileWithBorders(x, y)).BottomRightHeight += heightDelta;

								}
								else if (y == bottomRight.Y) {
									//bottom left corner tile
									((BorderTile)GetTileWithBorders(x, y)).TopRightHeight += heightDelta;
								}
								else {
									//left side tile
									((BorderTile)GetTileWithBorders(x, y)).TopRightHeight += heightDelta;
									((BorderTile)GetTileWithBorders(x, y)).BottomRightHeight += heightDelta;
								}
							}
							else if (x == bottomRight.X) {
								if (y == topLeft.Y) {
									//top right corner tile
									((BorderTile)GetTileWithBorders(x, y)).BottomLeftHeight += heightDelta;
								}
								else if (y == bottomRight.Y) {
									//bottom right corner tile
									((BorderTile)GetTileWithBorders(x, y)).TopLeftHeight += heightDelta;
								}
								else {
									//right side tile
									((BorderTile)GetTileWithBorders(x, y)).TopLeftHeight += heightDelta;
									((BorderTile)GetTileWithBorders(x, y)).BottomLeftHeight += heightDelta;
								}
							}
							else if (y == topLeft.Y) {
								//top side tile
								((BorderTile)GetTileWithBorders(x, y)).BottomLeftHeight += heightDelta;
								((BorderTile)GetTileWithBorders(x, y)).BottomRightHeight += heightDelta;
							}
							else if (y == bottomRight.Y) {
								//bottom side tile
								((BorderTile)GetTileWithBorders(x, y)).TopLeftHeight += heightDelta;
								((BorderTile)GetTileWithBorders(x, y)).TopRightHeight += heightDelta;
							}
							else {
								//TODO: Exception
								throw new Exception("Implementation error, wrong if condition here");
							}
						}
						else if ((x == bottomRight.X && y != topLeft.Y) ||
								 (y == bottomRight.Y && x != topLeft.X)) {
							//Only the right side tiles need to change on the border if they are normal inner tiles
							// excluding topRight corner tile, that one doesnt need to change too if its normal inner tile
							GetTileByTopLeftCorner(x, y).ChangeTopLeftHeight(heightDelta, false);
						} 
					}
					else {
						//inner tile
						Debug.Assert(!IsBorder(x, y));
						GetTileByTopLeftCorner(x, y).ChangeTopLeftHeight(heightDelta, false);
					}
				}
			}

			ForEachInRectangle(topLeft, bottomRight, (tile) => { tile.CornerHeightChange(); });

			graphics.CorrectTileHeight(topLeft, bottomRight);

		}

		public void ChangeTileHeight(ITile centerTile, 
									 IntVector2 rectangleSize,
									 ChangeTileHeightDelegate newHeightFunction) {

			//COPYING IS FREQUENT SOURCE OF ERRORS
			IntVector2 topLeft = centerTile.TopLeft - (rectangleSize / 2);
			IntVector2 bottomRight = topLeft + (rectangleSize - new IntVector2(1, 1));
			SquishToMap(ref topLeft, ref bottomRight);

			//Make the rectangle larger, to include the surrounding tiles
			// I need to change height of their side or corner, to make the map continuous
			topLeft -= new IntVector2(1, 1);
			bottomRight += new IntVector2(1, 1);

			for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
				for (int x = topLeft.X; x <= bottomRight.X; x++) {
					if (x == topLeft.X || x == bottomRight.X || y == topLeft.Y || y == bottomRight.Y) {
						//Its a side tile of this rectangle
						if (IsBorder(x, y)) {
							if (x == topLeft.X) {
								if (y == topLeft.Y) {
									//Top left corner tile 
									((BorderTile) GetTileWithBorders(x, y)).BottomRightHeight =
										newHeightFunction(((BorderTile) GetTileWithBorders(x, y)).BottomRightHeight, 
														  x + 1,
														  y + 1);

								}
								else if (y == bottomRight.Y) {
									//bottom left corner tile
									((BorderTile)GetTileWithBorders(x, y)).TopRightHeight = 
										newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).TopRightHeight,
														  x + 1,
														  y);
								}
								else {
									//left side tile
									((BorderTile)GetTileWithBorders(x, y)).TopRightHeight =
										newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).TopRightHeight,
														  x + 1,
														  y);
									((BorderTile)GetTileWithBorders(x, y)).BottomRightHeight =
										newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).BottomRightHeight,
														  x + 1,
														  y + 1);
								}
							}
							else if (x == bottomRight.X) {
								if (y == topLeft.Y) {
									//top right corner tile
									((BorderTile)GetTileWithBorders(x, y)).BottomLeftHeight = 
										newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).BottomLeftHeight,
														  x,
														  y - 1);
								}
								else if (y == bottomRight.Y) {
									//bottom right corner tile
									((BorderTile) GetTileWithBorders(x, y)).TopLeftHeight =
										newHeightFunction(((BorderTile) GetTileWithBorders(x, y)).TopLeftHeight,
														  x,
														  y);
								}
								else {
									//right side tile
									((BorderTile)GetTileWithBorders(x, y)).TopLeftHeight =
										newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).TopLeftHeight,
														  x,
														  y);
									((BorderTile)GetTileWithBorders(x, y)).BottomLeftHeight =
										newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).BottomLeftHeight,
														  x,
														  y - 1);
								}
							}
							else if (y == topLeft.Y) {
								//top side tile
								((BorderTile)GetTileWithBorders(x, y)).BottomLeftHeight =
									newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).BottomLeftHeight,
													  x,
													  y - 1);
								((BorderTile)GetTileWithBorders(x, y)).BottomRightHeight =
									newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).BottomRightHeight,
													  x + 1,
													  y + 1);
							}
							else if (y == bottomRight.Y) {
								//bottom side tile
								((BorderTile)GetTileWithBorders(x, y)).TopLeftHeight =
									newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).TopLeftHeight,
													  x,
													  y);
								((BorderTile)GetTileWithBorders(x, y)).TopRightHeight =
									newHeightFunction(((BorderTile)GetTileWithBorders(x, y)).TopRightHeight,
													  x + 1,
													  y);
							}
							else {
								//TODO: Exception
								throw new Exception("Implementation error, wrong if condition here");
							}
						}
						else if ((x == bottomRight.X && y != topLeft.Y) ||
								 (y == bottomRight.Y && x != topLeft.X)) {
							//Only the right side tiles need to change on the border if they are normal inner tiles
							// excluding topRight corner tile, that one doesnt need to change too if its normal inner tile
							ITile tile = GetTileByTopLeftCorner(x, y);
							tile.SetTopLeftHeight(newHeightFunction(tile.TopLeftHeight, x, y), false);
						}
					}
					else {
						//inner tile
						Debug.Assert(!IsBorder(x, y));
						ITile tile = GetTileByTopLeftCorner(x, y);
						tile.SetTopLeftHeight(newHeightFunction(tile.TopLeftHeight, x, y), false);
					}
				}
			}

			ForEachInRectangle(topLeft, bottomRight, (tile) => { tile.CornerHeightChange(); });

			graphics.CorrectTileHeight(topLeft, bottomRight);
		}

		//TODO: Handle right and bottom side tiles better
		public float GetHeightAt(int x, int y) {
			ITile tile;
			if ((tile = GetTileWithBorders(x,y)) != null) {
				return tile.TopLeftHeight;
			}

			BorderTile bTile;
			if ((bTile = (BorderTile)TileByTopRightCorner(new IntVector2(x,y), true)) != null) {
				return bTile.TopRightHeight;
			}

			if ((bTile = (BorderTile)TileByBottomLeftCorner(new IntVector2(x, y), true)) != null) {
				return bTile.BottomLeftHeight;
			}

			if ((bTile = (BorderTile)TileByBottomRightCorner(new IntVector2(x, y), true)) != null) {
				return bTile.BottomRightHeight;
			}

			throw new ArgumentOutOfRangeException($"Point [{x},{y}] is not inside the map, even with borders");
		}

		public float GetHeightAt(IntVector2 position) {
			return GetHeightAt(position.X, position.Y);
		}

		public float GetHeightAt(float x, float y) {

			int topLeftX = (int) Math.Floor(x);
			int topLeftY = (int) Math.Floor(y);

			float topLeftHeight = GetHeightAt(topLeftX, topLeftY);
			float topRightHeight = GetHeightAt(topLeftX + 1, topLeftY);
			float botLeftHeight = GetHeightAt(topLeftX, topLeftY + 1);
			float botRightHeight = GetHeightAt(topLeftX + 1, topLeftY + 1);

			if (IsTileSplitFromTopLeftToBottomRight(topLeftHeight, topRightHeight, botLeftHeight, botRightHeight)) {
				//Tile is split from topleft to botRight
				Vector2 botLeftToTargetDistance = new Vector2(x - topLeftX, y - (topLeftY + 1));

				//Barycentric coordinates
				float v = botLeftToTargetDistance.X; //botRight coef
				float w = -botLeftToTargetDistance.Y; //topLeft coef, inverted because we are counting from botLeft
				float u = 1.0f - v - w; //botLeft or topRight coef

				if (u >= 0) {
					//In bottom left triangle
					return u * botLeftHeight + v * botRightHeight + w * topLeftHeight;
				}
				else {
					float tmp = v;
					v = 1.0f - w;
					w = 1.0f - tmp;
					u = 1.0f - v - w;
					return u * topRightHeight + v * botRightHeight + w * topLeftHeight;
				}

			}
			else {
				//Tile is split from topRight to botLeft
				Vector2 topLeftToTargetDistance = new Vector2(x - topLeftX, y - topLeftY);

				//Barycentric coordinates
				float v = topLeftToTargetDistance.X; //topRight coef
				float w = topLeftToTargetDistance.Y; //bottomLeft coef
				float u = 1.0f - v - w; //topLeft or bottomRight coef

				if (u >= 0) {
					//In top left triangle

					return u * topLeftHeight + v * topRightHeight + w * botLeftHeight;
				}
				else {
					//In bottom right triangle

					float tmp = v;
					v = 1.0f - w;
					w = 1.0f - tmp;
					u = 1.0f - v - w;
					return u * botRightHeight + v * topRightHeight + w * botLeftHeight;
				}
			}
		}

		public float GetHeightAt(Vector2 position) {
			return GetHeightAt(position.X, position.Y);
		}

		public Vector3 GetUpDirectionAt(float x, float y) {
			return GetUpDirectionAt(new Vector2(x, y));
		}

		public Vector3 GetUpDirectionAt(Vector2 position) {

			var topLeftX = (int)Math.Floor(position.X);
			var topLeftY = (int)Math.Floor(position.Y);

			var topLeft = new Vector3(topLeftX, GetHeightAt(topLeftX, topLeftY), topLeftY);
			var topRight = new Vector3(topLeftX + 1, GetHeightAt(topLeftX + 1, topLeftY), topLeftY);
			var botLeft = new Vector3(topLeftX, GetHeightAt(topLeftX, topLeftY + 1), topLeftY + 1);
			var botRight = new Vector3(topLeftX + 1, GetHeightAt(topLeftX + 1, topLeftY + 1), topLeftY + 1);

			if (IsTileSplitFromTopLeftToBottomRight(topLeft.Y, topRight.Y, botLeft.Y, botRight.Y)) {
				if ((topRight.XZ2() - position).LengthSquared < (botLeft.XZ2() - position).LengthSquared) {
					//point is in the topRight triangle
					return Vector3.Cross(topLeft - topRight, botRight - topRight);
				}
				else {
					//point is in the bottomLeft triangle
					return Vector3.Cross(botRight - botLeft, topLeft - botLeft);
				}
			}
			else {
				if ((topLeft.XZ2() - position).LengthSquared < (botRight.XZ2() - position).LengthSquared) {
					//point is in topLeft triangle
					return Vector3.Cross(botLeft - topLeft, topRight - topLeft);
				}
				else {
					//point is in the bottomRight triangle
					return Vector3.Cross(topRight - botRight, botLeft - botRight);
				}
			}
		} 

		public void HighlightArea(ITile center, IntVector2 size, HighlightMode mode, Color color) {
			IntVector2 topLeft = center.TopLeft - (size / 2);
			IntVector2 bottomRight = center.TopLeft + (size / 2);
			HighlightArea(topLeft, bottomRight, mode, color);
		}

		public void HighlightArea(IntVector2 topLeft, IntVector2 bottomRight, HighlightMode mode, Color color) {
			SquishToMap(ref topLeft, ref bottomRight);
			graphics.HighlightArea(new IntRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y), mode, color);
		}

		public void HighlightArea(IntRect rectangle, HighlightMode mode, Color color) {
			HighlightArea(rectangle.TopLeft(), rectangle.BottomRight(), mode, color);
		}

		public void DisableHighlight() {
			graphics.DisableHighlight();
		}

		public void ChangeHeight(List<IntVector2> tileCorners, float heightDelta) {

			foreach (var tileCorner in tileCorners) {
				if (!IsBorderCorner(tileCorner)) {
					//Just need to change one value in the tile with this corner as topLeft corner
					ITile tile = GetTileByTopLeftCorner(tileCorner);

					Debug.Assert(tile != null, "tile corner was in the list while being outside the map");
					tile?.ChangeTopLeftHeight(heightDelta);
				}
				else {
					//Change the height of all 4 tiles containing this corner
					ITile tile = TileByBottomRightCorner(tileCorner, true);
					if (tile != null && IsBorder(tile)) {
						((BorderTile) tile).BottomRightHeight += heightDelta;
					}

					tile = TileByBottomLeftCorner(tileCorner, true);
					if (tile != null && IsBorder(tile)) {
						((BorderTile) tile).BottomLeftHeight += heightDelta;
					}

					tile = TileByTopRightCorner(tileCorner, true);
					if (tile != null && IsBorder(tile)) {
						((BorderTile)tile).TopRightHeight += heightDelta;
					}

					tile = TileByTopLeftCorner(tileCorner, true);
					//Because normal tiles contain height of their top left corner, we can just use 
					// the default changeHeight method
					tile?.ChangeTopLeftHeight(heightDelta);
				}
			}

			graphics.ChangeCornerHeights(tileCorners, heightDelta);
		}

		public void ChangeHeightTo(List<IntVector2> tileCorners, float newHeight) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets tile containing <paramref name="point"/> projection into the XZ plane
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public ITile GetContainingTile(Vector3 point) {
			return GetContainingTile(point.XZ2());
		}

		/// <summary>
		/// Gets tile containing <paramref name="point"/> in the XZ plane
		/// </summary>
		/// <param name="point">The point in the XZ plane</param>
		/// <returns>The tile containing <paramref name="point"/></returns>
		public ITile GetContainingTile(Vector2 point) {
			int topLeftX = (int)Math.Floor(point.X);
			int topLeftZ = (int)Math.Floor(point.Y);
			return GetTileByTopLeftCorner(topLeftX, topLeftZ);
		}


		public void ForEachInRectangle(IntVector2 topLeft, IntVector2 bottomRight, Action<ITile> action) {
			for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
				for (int x = topLeft.X; x <= bottomRight.X; x++) {
					var tile = GetTileByTopLeftCorner(x, y);
					if (tile != null) {
						action(tile);
					}
					
				}
			}
		}

		public void ForEachInRectangle(IntRect rectangle, Action<ITile> action) {
			ForEachInRectangle(rectangle.TopLeft(), rectangle.BottomRight(), action);
		}

		public void ForEachAroundCorner(IntVector2 cornerCoords, Action<ITile> action) {
			ITile tile;
			if ((tile = GetTileWithBorders(cornerCoords)) != null) {
				action(tile);
			}

			if ((tile = GetTileWithBorders(cornerCoords + new IntVector2(-1, 0))) != null) {
				action(tile);
			}

			if ((tile = GetTileWithBorders(cornerCoords + new IntVector2(0, -1))) != null) {
				action(tile);
			}

			if ((tile = GetTileWithBorders(cornerCoords + new IntVector2(-1, -1))) != null) {
				action(tile);
			}
		}

		public IRangeTarget GetRangeTarget(Vector3 position) {
			if (!mapRangeTargets.TryGetValue(position, out MapRangeTarget mapTarget)) {
				mapTarget = MapRangeTarget.CreateNew(levelManager, position);
				mapRangeTargets.Add(position, mapTarget);
			}

			return mapTarget;
		}

		internal void RemoveRangeTarget(MapRangeTarget mapRangeTarget) {
			var removed = mapRangeTargets.Remove(mapRangeTarget.Position);
			Debug.Assert(removed);
		}

		public void Dispose() {
			((IDisposable) graphics).Dispose();
			node.Dispose();
		}

		private void BuildGeometry() {
			graphics = MapGraphics.Build(node, 
										 this, 
										 tiles, 
										 new IntVector2(WidthWithBorders, LengthWithBorders));
		}

		private int GetTileIndex(int x, int y) {
			return x + y * WidthWithBorders;
		}

		private int GetTileIndex(IntVector2 location) {
			return GetTileIndex(location.X, location.Y);
		}

		private int GetTileIndex(ITile tile) {
			return GetTileIndex(tile.MapLocation);
		}

	 
		private bool IsBorder(int x, int y) {
			return IsLeftBorder(x,y) ||
				   IsRightBorder(x,y) ||
				   IsTopBorder(x,y) ||
				   IsBottomBorder(x,y);

		}

		private bool IsBorder(IntVector2 location) {
			return IsBorder(location.X, location.Y);
		}

		private bool IsBorder(ITile tile) {
			return IsBorder(tile.MapLocation);
		}

		private BorderType GetBorderType(int x, int y) {
			if (IsLeftBorder(x,y)) {
				if (IsTopBorder(x, y)) {
					return BorderType.TopLeft;
				}
				if (IsBottomBorder(x, y)) {
					return BorderType.BottomLeft;
				}
				return BorderType.Left;
			}

			if (IsRightBorder(x, y)) {
				if (IsTopBorder(x, y)) {
					return BorderType.TopRight;
				}
				if (IsBottomBorder(x, y)) {
					return BorderType.BottomRight;
				}
				return BorderType.Right;
			}

			if (IsTopBorder(x, y)) {
				//We already know its not left or right border
				return BorderType.Top;
			}

			if (IsBottomBorder(x, y)) {
				//We already know its not left or right border
				return BorderType.Bottom;
			}

			return BorderType.None;
		}

		private BorderType GetBorderType(IntVector2 location) {
			return GetBorderType(location.X, location.Y);
		}

		private bool IsTopBorder(int x, int y) {
			return (0 <= y && y < Top);
		}

		private bool IsTopBorder(IntVector2 location) {
			return IsTopBorder(location.X, location.Y);
		}

		private bool IsBottomBorder(int x, int y) {
			return (Bottom + 1 <= y && y < WidthWithBorders);
		}

		private bool IsBottomBorder(IntVector2 location) {
			return IsBottomBorder(location.X, location.Y);
		}

		private bool IsLeftBorder(int x, int y) {
			return (0 <= x && x < Left);
		}

		private bool IsLeftBorder(IntVector2 location) {
			return IsLeftBorder(location.X, location.Y);
		}

		private bool IsRightBorder(int x, int y) {
			return (Right + 1 <= x && x < WidthWithBorders);
		}

		private bool IsRightBorder(IntVector2 location) {
			return IsRightBorder(location.X, location.Y);
		}
		/// <summary>
		/// Gets tile by topLeft corner coordinates
		/// </summary>
		/// <param name="x">top left corner X coord</param>
		/// <param name="y">top left corner Y coord</param>
		/// <returns></returns>
		private ITile GetTileWithBorders(int x, int y) {
			if (LeftWithBorders <= x &&
				x <= RightWithBorders &&
				TopWithBorders <= y &&
				y <= BottomWithBorders) {

				return tiles[GetTileIndex(x,y)];
			}
			return null;
		}

		private ITile GetTileWithBorders(IntVector2 coords) {
			return GetTileWithBorders(coords.X, coords.Y);
		}

		private ITile TileByTopLeftCorner(IntVector2 topLeftCorner, bool withBorders) {
			return withBorders ? GetTileWithBorders(topLeftCorner) : GetTileByTopLeftCorner(topLeftCorner);
		}

		private ITile TileByTopRightCorner(IntVector2 topRightCorner, bool withBorders) {
			IntVector2 topLeft = topRightCorner + new IntVector2(-1, 0);
			return withBorders ? GetTileWithBorders(topLeft) : GetTileByTopLeftCorner(topLeft);
		}

		private ITile TileByBottomLeftCorner(IntVector2 bottomLeftCorner, bool withBorders) {
			IntVector2 topLeft = bottomLeftCorner + new IntVector2(0, -1);
			return withBorders ? GetTileWithBorders(topLeft) : GetTileByTopLeftCorner(topLeft);
		}
		private ITile TileByBottomRightCorner(IntVector2 bottomRightCorner, bool withBorders) {
			IntVector2 topLeft = bottomRightCorner + new IntVector2(-1, -1);
			return withBorders ? GetTileWithBorders(topLeft) : GetTileByTopLeftCorner(topLeft);
		}

		private bool IsBorderCorner(int x, int y) {
			return (LeftWithBorders <= x && x <= Left) ||
				   (Right <= x && x <= RightWithBorders + 1) || //+1 because RightWithBorders contains coords of the topLeftCorner
				   (TopWithBorders <= y && y <= Top) ||
				   (Bottom <= y && y <= BottomWithBorders + 1);
		}

		private bool IsBorderCorner(IntVector2 corner) {
			return IsBorderCorner(corner.X, corner.Y);
		}

		private bool IsTileSplitFromTopLeftToBottomRight(float topLeftHeight,
														 float topRightHeight,
														 float bottomLeftHeight,
														 float bottomRightHeight) {
			return topLeftHeight + bottomRightHeight >= topRightHeight + bottomLeftHeight;
		}

		private bool IsTileSplitFromTopLeftToBottomRight(ITile tile) {
			return IsTileSplitFromTopLeftToBottomRight(tile.TopLeftHeight,
													   tile.TopRightHeight,
													   tile.BottomLeftHeight,
													   tile.BottomRightHeight);
		}
	}
}
