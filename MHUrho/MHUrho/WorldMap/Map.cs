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
using MHUrho.PathFinding;
using Urho.IO;


namespace MHUrho.WorldMap
{
	public partial class Map : IMap, IDisposable {

		class Loader : IMapLoader {
			
			public Map Map { get; private set; }

			readonly LevelManager level;
			readonly Node mapNode;
			readonly Octree octree;
			readonly StMap storedMap;
			readonly LoadingWatcher loadingProgress;

			readonly List<ILoader> tileLoaders;

			public Loader(LevelManager level, Node mapNode, Octree octree, StMap storedMap, LoadingWatcher loadingProgress)
			{
				this.level = level;
				this.mapNode = mapNode;
				this.octree = octree;
				this.storedMap = storedMap;
				this.loadingProgress = loadingProgress;
				tileLoaders = new List<ILoader>();
			}

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
			public void StartLoading() {
				Map = new Map(mapNode, octree, storedMap) {levelManager = level};

				foreach (var storedMapTarget in storedMap.MapRangeTargets) {
					var newTarget = MapRangeTarget.Load(level, storedMapTarget);
					Map.mapRangeTargets.Add(newTarget.CurrentPosition, newTarget);
				}

				var tiles = storedMap.Tiles.GetEnumerator();
				var borderTiles = storedMap.BorderTiles.GetEnumerator();
				try {

					for (int y = 0; y < Map.LengthWithBorders; y++) {
						for (int x = 0; x < Map.WidthWithBorders; x++) {
							ITileLoader newTileLoader;
							if (Map.IsBorderTileMapLocation(x, y)) {
								if (!borderTiles.MoveNext()) {
									//TODO: Exception
									throw new Exception("Corrupted save file");
								}

								newTileLoader = BorderTile.GetLoader(Map, borderTiles.Current);
								
							}
							else {
								if (!tiles.MoveNext()) {
									//TODO: Exception
									throw new Exception("Corrupted save file");
								}

								newTileLoader = Tile.GetLoader(level, Map, tiles.Current);
							}

							newTileLoader.StartLoading();
							tileLoaders.Add(newTileLoader);
							Map.tiles[Map.GetTileIndex(x, y)] = newTileLoader.Tile;
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


				Map.PathFinding = new AStar(Map);
			}

			

			public void ConnectReferences() {
				foreach (var loader in tileLoaders) {
					loader.ConnectReferences();
				}
			}

			/// <summary>
			/// Builds geometry and releases stored data
			/// </summary>
			public void FinishLoading() {
				foreach (var loader in tileLoaders) {
					loader.FinishLoading();
				}

				Map.BuildGeometry(loadingProgress);
			}

		}

		class BorderTile : ITile {

			class Loader : ITileLoader {

				ITile ITileLoader.Tile => Tile;

				public BorderTile Tile { get; private set; }

				readonly Map map;
				readonly StBorderTile storedTile;

				public Loader(Map map, StBorderTile storedTile)
				{
					this.map = map;
					this.storedTile = storedTile;
				}

				public static StBorderTile Save(BorderTile borderTile)
				{
					var stBorderTile = new StBorderTile
										{
											TopLeftPosition = borderTile.TopLeft.ToStIntVector2(),
											TopLeftHeight = borderTile.TopLeftHeight,
											TopRightHeight = borderTile.TopRightHeight,
											BotLeftHeight = borderTile.BottomLeftHeight,
											BotRightHeight = borderTile.BottomRightHeight
										};
					return stBorderTile;
				}

				/// <summary>
				/// Loads everything apart from thigs referenced by ID
				/// 
				/// After everything had it StartLoading called, call ConnectReferences on everything
				/// </summary>
				/// <param name="storedTile">Image of the tile</param>
				/// <param name="map">Map this tile is in</param>
				/// <returns>Partially initialized tile</returns>
				public void StartLoading()
				{
					Tile = new BorderTile(storedTile, map);
				}

				public void ConnectReferences()
				{

				}

				public void FinishLoading()
				{

				}
			}

			IBuilding ITile.Building => throw new InvalidOperationException("Cannot add building to Border tile");

			IReadOnlyList<IUnit> ITile.Units => throw new InvalidOperationException("Cannot add unit to Border tile");

			public TileType Type => throw new InvalidOperationException("Cnnot get type of a BorderTile");


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

			public Vector3 Center3 => new Vector3(Center.X, Map.GetTerrainHeightAt(Center), Center.Y);

			public Vector3 TopLeft3 => new Vector3(MapArea.Left, Map.GetTerrainHeightAt(MapArea.Left, MapArea.Top), MapArea.Top);

			public Vector3 TopRight3 => new Vector3(MapArea.Right, Map.GetTerrainHeightAt(MapArea.Right, MapArea.Top), MapArea.Top);

			public Vector3 BottomLeft3 => new Vector3(MapArea.Left, Map.GetTerrainHeightAt(MapArea.Left, MapArea.Bottom), MapArea.Bottom);

			public Vector3 BottomRight3 => new Vector3(MapArea.Right, Map.GetTerrainHeightAt(MapArea.Right, MapArea.Bottom), MapArea.Bottom);

			IMap ITile.Map => Map;

			Map Map;

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


			public static ITileLoader GetLoader(Map map, StBorderTile storedBorderTile)
			{
				return new Loader(map, storedBorderTile);
			}

			void ITile.AddUnit(IUnit unit) {
				throw new InvalidOperationException("Cannot add unit to Border tile");
			}

			void ITile.RemoveUnit(IUnit unit) {
				throw new InvalidOperationException("Cannot remove unit from Border tile");
			}

			public void AddBuilding(IBuilding building) {
				throw new InvalidOperationException("Cannot add building to Border tile");
			}

			public void RemoveBuilding(IBuilding building) {
				throw new InvalidOperationException("Cannot remove building from Border tile");
			}
			StTile ITile.Save() {
				throw new InvalidOperationException("Cannot save BorderTile as a tile");
			}

			public StBorderTile Save()
			{
				return Loader.Save(this);
			}

			void ITile.ChangeType(TileType newType)
			{
				throw new InvalidOperationException("Cannot change the type of a BorderTile");
			}

			public void ChangeTopLeftHeight(float heightDelta) {
				Height += heightDelta;
			}

			public void SetTopLeftHeight(float newHeight) {
				Height = newHeight;
			}

			public void CornerHeightChange() {
				//Nothing
			}

			public float GetHeightAt(float x, float y)
			{
				return Map.GetTerrainHeightAt(x, y);
			}

			public float GetHeightAt(Vector2 position)
			{
				return GetHeightAt(position.X, position.Y);
			}

			public IEnumerable<ITile> GetNeighbours()
			{
				yield return Map.GetTileWithBorders(TopLeft + new IntVector2(-1, -1));
				yield return Map.GetTileWithBorders(TopLeft + new IntVector2(0, -1));
				yield return Map.GetTileWithBorders(TopLeft + new IntVector2(1, -1));
				yield return Map.GetTileWithBorders(TopLeft + new IntVector2(-1, 0));
				yield return Map.GetTileWithBorders(TopLeft + new IntVector2(1, 0));
				yield return Map.GetTileWithBorders(TopLeft + new IntVector2(-1, 1));
				yield return Map.GetTileWithBorders(TopLeft + new IntVector2(0, 1));
				yield return Map.GetTileWithBorders(TopLeft + new IntVector2(1, 1));
			}

			public BorderTile(StBorderTile stBorderTile, Map map) {
				this.MapArea = new IntRect(stBorderTile.TopLeftPosition.X, 
										   stBorderTile.TopLeftPosition.Y, 
										   stBorderTile.TopLeftPosition.X + 1, 
										   stBorderTile.TopLeftPosition.Y + 1);
				this.TopLeftHeight = stBorderTile.TopLeftHeight;
				this.TopRightHeight = stBorderTile.TopRightHeight;
				this.BottomLeftHeight = stBorderTile.BotLeftHeight;
				this.BottomRightHeight = stBorderTile.BotRightHeight;
				this.Map = map;
				BorderType = map.GetBorderTileType(this.MapLocation);
			}

			public BorderTile(int x, int y, BorderType borderType, Map map) {
				MapArea = new IntRect(x, y, x + 1, y + 1);
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

		public ILevelManager LevelManager => levelManager;

		public event Action<ITile> TileHeightChanged;

		public static IntVector2 ChunkSize => new IntVector2(50, 50);
		public static IntVector2 MinSize => ChunkSize;
		public static IntVector2 MaxSize => ChunkSize * 10;


		readonly ITile[] tiles;

		readonly Node node;

		readonly Octree octree;

		MapGraphics graphics;

		LevelManager levelManager;

		Dictionary<Vector3, MapRangeTarget> mapRangeTargets;

		/// <summary>
		/// Width of the whole map with borders included
		/// </summary>
		int WidthWithBorders => Width + 2;
		/// <summary>
		/// Length of the whole map with the borders included
		/// </summary>
		int LengthWithBorders => Length + 2;

		int LeftWithBorders => Left - 1;

		int RightWithBorders => Right + 1;

		int TopWithBorders => Top - 1;

		int BottomWithBorders => Bottom + 1;

		

		/// <summary>
		/// Creates default map at height 0 with all tiles with the default type
		/// </summary>
		/// <param name="mapNode">Node to connect the map to</param>
		/// <param name="size">Size of the playing field, excluding the borders</param>
		/// <returns>Fully created map</returns>
		internal static Map CreateDefaultMap(LevelManager level, Node mapNode, Octree octree, IntVector2 size, LoadingWatcher loadingProgress) 
		{
			Map newMap = new Map(mapNode, octree, size.X, size.Y) {levelManager = level};

			TileType defaultTileType = PackageManager.Instance.ActivePackage.DefaultTileType;

			for (int i = 0; i < newMap.tiles.Length; i++) {
				IntVector2 tilePosition = new IntVector2(i % newMap.WidthWithBorders, i / newMap.WidthWithBorders);
				if (newMap.IsBorderTileMapLocation(tilePosition)) {
					BorderType borderType = newMap.GetBorderTileType(tilePosition.X, tilePosition.Y);

					Debug.Assert(borderType != BorderType.None,
								 "Error in implementation of IsBorder or GetBorderType");

					newMap.tiles[i] = new BorderTile(tilePosition.X, tilePosition.Y, borderType, newMap);
				}
				else {
					newMap.tiles[i] = new Tile(tilePosition.X, tilePosition.Y, defaultTileType, newMap);
				}

			}

			loadingProgress.TextUpdate("Creating pathfinding graph");
			newMap.PathFinding = new AStar(newMap);

			newMap.BuildGeometry(loadingProgress);
			return newMap;
		}

		internal static IMapLoader GetLoader(LevelManager level, Node mapNode, Octree octree, StMap storedMap, LoadingWatcher loadingProgress)
		{
			return new Loader(level, mapNode, octree, storedMap, loadingProgress);
		}

		public StMap Save() 
		{
			var storedMap = new StMap();
			var stSize = new StIntVector2
						{
							X = Width,
							Y = Length
						};
			storedMap.Size = stSize;

			foreach (var tile in tiles) {
				if (IsBorderTile(tile)) {
					storedMap.BorderTiles.Add(((BorderTile)tile).Save());
				}
				else {
					storedMap.Tiles.Add(tile.Save());
				} 
			}

			foreach (var target in mapRangeTargets.Values) {
				storedMap.MapRangeTargets.Add(target.Save());
			}

			return storedMap;
		}

	

		protected Map(Node mapNode, Octree octree, StMap storedMap)
			:this(mapNode, octree, storedMap.Size.X, storedMap.Size.Y) {

		}

		/// <summary>
		/// Creates map connected to mapNode with the PLAYING FIELD of width <paramref name="width"/> and length <paramref name="length"/>
		/// </summary>
		/// <param name="mapNode"></param>
		/// <param name="width">Width of the playing field without borders</param>
		/// <param name="length">Length of the playing field without borders</param>
		protected Map(Node mapNode, Octree octree, int width, int length) 
		{
			this.node = mapNode;
			this.octree = octree;
			this.TopLeft = new IntVector2(1, 1);
			this.BottomRight = TopLeft + new IntVector2(width - 1, length - 1);
			this.mapRangeTargets = new Dictionary<Vector3, MapRangeTarget>();

			this.tiles = new ITile[WidthWithBorders *  LengthWithBorders];
		}

		public bool IsInside(int x, int z) 
		{
			return Left <= x && x <= Right && Top <= z && z <= Bottom;
		}

		public bool IsInside(float x, float z) 
		{
			return Left <= x && x < Left + Width && Top <= z && z < Top + Length;
		}

		/// <summary>
		/// Checks if the point is inside the playfield, which means it could be used for indexing into the map
		/// </summary>
		/// <param name="point">the point to check</param>
		/// <returns>True if it is inside, False if not</returns>
		public bool IsInside(IntVector2 point) 
		{
			return IsInside(point.X, point.Y);
		}

		/// <summary>
		/// Checks if <paramref name="point"/> is inside the map borders in the XZ plane
		/// </summary>
		/// <param name="point">position to check</param>
		/// <returns>true if inside, false if outside</returns>
		public bool IsInside(Vector2 point) 
		{
			return IsInside(point.X, point.Y);
		}

		/// <summary>
		/// Checks if <paramref name="point"/> is inside the map borders and above terrain
		/// </summary>
		/// <param name="point">position to check</param>
		/// <returns>true if inside, false if outside</returns>
		public bool IsInside(Vector3 point) 
		{
			return IsInside(point.X, point.Z) && GetTerrainHeightAt(point.X, point.Z) <= point.Y;
		}

		public bool IsXInside(int x) 
		{
			return Left <= x && x <= Right;
		}

		public bool IsXInside(IntVector2 point) 
		{
			return IsXInside(point.X);
		}

		public bool IsZInside(int z) 
		{
			return Top <= z && z <= Bottom;
		}

		public bool IsZInside(IntVector2 point) 
		{
			return IsZInside(point.Y);
		}

		/// <summary>
		/// Compares x with the coords of Left and Right side, returns where the x is
		/// </summary>
		/// <param name="x">x coord to copare with the map boundaries</param>
		/// <returns>-1 if X is to the left, 0 if inside, 1 if to the right of the map rectangle</returns>
		public int WhereIsX(int x) 
		{
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
		/// <param name="point">compares x coord of this vector</param>
		/// <returns>-1 if X is to the left, 0 if inside, 1 if to the right of the map rectangle</returns>
		public int WhereIsX(IntVector2 point) 
		{
			return WhereIsX(point.X);
		}


		/// <summary>
		/// Compares y with the coords of Top and Bottom side, returns where the y is
		/// </summary>
		/// <param name="z">y coord to copare with the map boundaries</param>
		/// <returns>-1 if Y is above, 0 if inside, 1 if below the map rectangle</returns>
		public int WhereIsZ(int z) 
		{
			if (z < Top) {
				return -1;
			}

			if (z > Bottom) {
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// Compares y with the coords of Top and Bottom side, returns where the y is
		/// </summary>
		/// <param name="point">compares y of this vector</param>
		/// <returns>-1 if Y is above, 0 if inside, 1 if below the map rectangle</returns>
		public int WhereIsZ(IntVector2 point) 
		{
			return WhereIsZ(point.Y);
		}

		public ITile GetTileByMapLocation(int x, int z) 
		{
			return GetTileByTopLeftCorner(x, z);
		}

		public ITile GetTileByMapLocation(IntVector2 mapLocation) 
		{
			return GetTileByTopLeftCorner(mapLocation);
		}

		public ITile GetTileByTopLeftCorner(int x, int z) 
		{
			return IsInside(x, z) ? tiles[GetTileIndex(x, z)] : null;
		}

		public ITile GetTileByTopLeftCorner(IntVector2 topLeftCorner) 
		{
			return GetTileByTopLeftCorner(topLeftCorner.X, topLeftCorner.Y);
		}

		public ITile GetTileByTopRightCorner(int x, int z) 
		{
			return GetTileByTopLeftCorner(x - 1, z);
		}

		public ITile GetTileByTopRightCorner(IntVector2 topRightCorner) 
		{
			return GetTileByTopRightCorner(topRightCorner.X, topRightCorner.Y);
		}

		public ITile GetTileByBottomLeftCorner(int x, int z) 
		{
			return GetTileByTopLeftCorner(x, z - 1);
		}

		public ITile GetTileByBottomLeftCorner(IntVector2 bottomLeftCorner) 
		{
			return GetTileByBottomLeftCorner(bottomLeftCorner.X, bottomLeftCorner.Y);
		}

		public ITile GetTileByBottomRightCorner(int x, int z) 
		{
			return GetTileByTopLeftCorner(x - 1, z - 1);
		}

		public ITile GetTileByBottomRightCorner(IntVector2 bottomRightCorner) 
		{
			return GetTileByBottomRightCorner(bottomRightCorner.X, bottomRightCorner.Y);
		}

		/// <summary>
		/// Moves the rectangle defined by topLeft and bottomRight corners so that
		/// the whole rectangle is inside the map
		/// </summary>
		/// <param name="topLeft">top left corner of the rectangle</param>
		/// <param name="bottomRight">bottom right corner of the rectangle</param>
		/// <returns>True if it is possible to snap to map, false if it is not possible</returns>
		public bool SnapToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight) 
		{

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
			else if ((where = WhereIsZ(topLeft.Y)) != 0) {
				int dist = (where == -1) ? this.TopLeft.Y - topLeft.Y : this.BottomRight.Y - bottomRight.Y;
				topLeft.Y += dist;
				bottomRight.Y += dist;
			}

			return fits;
		}

		public void SquishToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight) 
		{
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

			switch (WhereIsZ(topLeft.Y)) {
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

			switch (WhereIsZ(bottomRight.Y)) {
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

		public ITile FindClosestTile(ITile source, Predicate<ITile> predicate)
		{
			return FindClosestTile(source, int.MaxValue, predicate);
		}

		public ITile FindClosestTile(ITile source, int squareSize, Predicate<ITile> predicate) 
		{
			IntVector2 topLeft = source.MapLocation;
			IntVector2 bottomRight = topLeft;
			IntVector2 oneOne = new IntVector2(1, 1);

			bool moveTopLeft = false;
			ITile result = null;

			for (int size = 0; size < squareSize; size++) {


				//Top row
				if ((result = SearchLineInX(topLeft, size, predicate)) != null) {
					break;
				}

				//Right column
				if ((result = SearchLineInY(new IntVector2(bottomRight.X, topLeft.Y), size, predicate)) != null) {
					break;
				}


				//Bottom row
				if ((result = SearchLineInX(new IntVector2(topLeft.X, bottomRight.Y), size, predicate)) != null) {
					break;
				}

				//Left column
				if ((result = SearchLineInY(topLeft, size, predicate)) != null) {
					break;
				}

				if (moveTopLeft) {
					topLeft += oneOne;
				}
				else {
					bottomRight -= oneOne;
				}
				moveTopLeft = !moveTopLeft;

				if (!IsInside(topLeft) && !IsInside(bottomRight)) {
					return result;
				}
			}
			return result;
		}

		public IEnumerable<ITile> GetTilesInSpiral(ITile center)
		{
			var spiralPoint = new Spiral(center.MapLocation).GetSpiralEnumerator();
			spiralPoint.MoveNext();
			while (true) {
				yield return GetTileByMapLocation(spiralPoint.Current);

				spiralPoint.MoveNext();
				//While the points are outside of the map
				while (!IsInside(spiralPoint.Current)) {
					//Check if there is a part of the spiral still intersecting with the map
					IntRect square = spiralPoint.GetContainingSquare();
					if (!IsInside(square.TopLeft()) && !IsInside(square.BottomRight())) {
						//The spiral is all outside the map
						yield break;
					}

					spiralPoint.MoveNext();
				}
			}
		}

		public IFormationController GetFormationController(ITile center)
		{
			return new MapFormationController(this, center);
		}

		/// <inheritdoc />
		public IEnumerable<ITile> GetTilesInRectangle(IntVector2 topLeft, IntVector2 bottomRight)
		{
			for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
				for (int x = topLeft.X; x <= bottomRight.X; x++) {
					ITile tile = GetTileByMapLocation(x, y);
					if (tile == null) continue;
					yield return tile;
				}
			}
		}

		/// <inheritdoc />
		public IEnumerable<ITile> GetTilesInRectangle(IntRect rectangle)
		{
			return GetTilesInRectangle(rectangle.TopLeft(), rectangle.BottomRight());
		}

		public IEnumerable<ITile> GetTilesAroundCorner(int x, int y)
		{
			return GetTilesAroundCorner(new IntVector2(x,y));
		}

		public IEnumerable<ITile> GetTilesAroundCorner(IntVector2 cornerCoords)
		{
			return GetTilesAroundCorner(cornerCoords, false);
		}

		/// <summary>
		/// Returns all results where the ray intersects the map
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
		public IEnumerable<RayQueryResult> RaycastToMap(Ray ray, float maxDistance = 10000)
		{
			var results = octree.Raycast(ray: ray, maxDistance: maxDistance);

			//TODO: Check it intersects from the correct side
			return from result in results
					where IsRaycastToMap(result)
					select result;
		}

		public bool IsRaycastToMap(RayQueryResult rayQueryResult)
		{
			return graphics.IsRaycastToMap(rayQueryResult);
		}

		public ITile RaycastToTile(List<RayQueryResult> rayQueryResults) 
		{
			return graphics.RaycastToTile(rayQueryResults);
		}

		public ITile RaycastToTile(RayQueryResult rayQueryResult) 
		{
			return graphics.RaycastToTile(rayQueryResult);
		}

		/// <summary>
		/// Gets the position of the closest tile corner to the point clicked
		/// </summary>
		/// <param name="rayQueryResults">result of raycast to process</param>
		/// <returns>Position of the closest tile corner to click or null if the clicked thing was not map</returns>
		public Vector3? RaycastToVertexPosition(List<RayQueryResult> rayQueryResults) 
		{
			return graphics.RaycastToVertex(rayQueryResults);
		}

		public Vector3? RaycastToVertexPosition(RayQueryResult rayQueryResult) 
		{
			return graphics.RaycastToVertex(rayQueryResult);
		}

		/// <summary>
		/// Gets map matrix coords of the tile corner
		/// </summary>
		/// <param name="rayQueryResults"></param>
		/// <returns></returns>
		public IntVector2? RaycastToVertex(List<RayQueryResult> rayQueryResults) 
		{
			var cornerPosition = RaycastToVertexPosition(rayQueryResults);

			if (!cornerPosition.HasValue) {
				return null;
			}

			return new IntVector2((int)cornerPosition.Value.X, (int)cornerPosition.Value.Z);
		}

		public IntVector2? RaycastToVertex(RayQueryResult rayQueryResult) 
		{
			var cornerPosition = RaycastToVertexPosition(rayQueryResult);

			if (!cornerPosition.HasValue) {
				return null;
			}

			return new IntVector2((int)cornerPosition.Value.X, (int)cornerPosition.Value.Z);
		}

		public Vector3? RaycastToWorldPosition(List<RayQueryResult> rayQueryResults)
		{
			return graphics.RaycastToWorldPosition(rayQueryResults);
		}

		public Vector3? RaycastToWorldPosition(RayQueryResult rayQueryResult)
		{
			return graphics.RaycastToWorldPosition(rayQueryResult);
		}

		public void ChangeTileType(ITile tile, TileType newType) 
		{
			if (tile.Type == newType) {
				return;
			}
		
			tile.ChangeType(newType);
			graphics.ChangeTileType(tile.MapLocation, newType);
		}

		public void ChangeTileType(ITile centerTile, IntVector2 rectangleSize, TileType newType) 
		{
			IntVector2 topLeft = centerTile.TopLeft - (rectangleSize / 2);
			IntVector2 bottomRight = topLeft + (rectangleSize - new IntVector2(1, 1));
			SquishToMap(ref topLeft, ref bottomRight);

			ForEachInRectangle(topLeft, bottomRight, (tile) => { tile.ChangeType(newType); });
			graphics.ChangeTileType(topLeft, bottomRight, newType);
		}

		public void ChangeTileHeight(ITile tile, float heightDelta)
		{
			ChangeTileHeight(tile, new IntVector2(1, 1), heightDelta);
		}

		/// <summary>
		/// For fast relative height changing in response to every mouse movement
		/// </summary>
		/// <param name="centerTile">center tile of the rectangle</param>
		/// <param name="rectangleSize">Size of the rectangle in which the height changes</param>
		/// <param name="heightDelta">By how much should the height change</param>
		public void ChangeTileHeight(ITile centerTile, IntVector2 rectangleSize, float heightDelta) 
		{
			IntVector2 topLeft = centerTile.TopLeft - (rectangleSize / 2);
			IntVector2 bottomRight = topLeft + (rectangleSize - new IntVector2(1, 1));
			SquishToMap(ref topLeft, ref bottomRight);

			//Include the bottom and right sides of the edge tiles
			bottomRight += new IntVector2(1, 1);

			for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
				for (int x = topLeft.X; x <= bottomRight.X; x++) {
					ChangeCornerHeight(x, y, heightDelta);
				}
			}

			//Include the tiles (indexed by top left corner), whose bottom and right edges changed
			//They are to the top and to the left of the current rectangle
			//We did not want them before because the their top left corners (by which they are indexed) did not change
			topLeft -= new IntVector2(1, 1);

			ForEachInRectangle(topLeft, bottomRight, (tile) => {
														tile.CornerHeightChange();
														TileHeightChanged?.Invoke(tile);
													});


			//Squish to map again, because we needed to enlarge the rectangle to signal the tiles
			//around the rectangle, but those may be BorderTiles, which are not represented in graphics
			SquishToMap(ref topLeft, ref bottomRight);
			graphics.CorrectTileHeight(topLeft, bottomRight);

		}

		public void ChangeTileHeight(ITile centerTile, 
									 IntVector2 rectangleSize,
									 ChangeCornerHeightDelegate newHeightFunction) 
		{

			//COPYING IS FREQUENT SOURCE OF ERRORS
			IntVector2 topLeft = centerTile.TopLeft - (rectangleSize / 2);
			IntVector2 bottomRight = topLeft + (rectangleSize - new IntVector2(1,1));
			SquishToMap(ref topLeft, ref bottomRight);

			//Include the bottom and right sides of the edge tiles
			bottomRight += new IntVector2(1, 1);

			for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
				for (int x = topLeft.X; x <= bottomRight.X; x++) {
					SetCornerHeight(x, y, newHeightFunction(GetTerrainHeightAt(x, y), x, y));
				}
			}

			//Include the tiles (indexed by top left corner), whose bottom and right edges changed
			//They are to the top and to the left of the current rectangle
			topLeft -= new IntVector2(1, 1);

			ForEachInRectangle(topLeft, bottomRight,
								(tile) => {
									tile.CornerHeightChange();
									TileHeightChanged?.Invoke(tile);
								});

			SquishToMap(ref topLeft, ref bottomRight);
			graphics.CorrectTileHeight(topLeft, bottomRight);
		}


		public float GetTerrainHeightAt(int x, int y) 
		{
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

		public float GetTerrainHeightAt(IntVector2 position) 
		{
			return GetTerrainHeightAt(position.X, position.Y);
		}

		public float GetTerrainHeightAt(float x, float y) 
		{

			int topLeftX = (int) Math.Floor(x);
			int topLeftY = (int) Math.Floor(y);

			float topLeftHeight = GetTerrainHeightAt(topLeftX, topLeftY);
			float topRightHeight = GetTerrainHeightAt(topLeftX + 1, topLeftY);
			float botLeftHeight = GetTerrainHeightAt(topLeftX, topLeftY + 1);
			float botRightHeight = GetTerrainHeightAt(topLeftX + 1, topLeftY + 1);

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

		public float GetTerrainHeightAt(Vector2 position) 
		{
			return GetTerrainHeightAt(position.X, position.Y);
		}

		public float GetHeightAt(float x, float y)
		{
			ITile tile = GetContainingTile(x, y);

			return tile.GetHeightAt(x, y);
		}

		public float GetHeightAt(Vector2 position)
		{
			return GetHeightAt(position.X, position.Y);
		}

		public Vector3 GetUpDirectionAt(float x, float y) 
		{
			return GetUpDirectionAt(new Vector2(x, y));
		}

		public Vector3 GetUpDirectionAt(Vector2 position) 
		{

			var topLeftX = (int)Math.Floor(position.X);
			var topLeftY = (int)Math.Floor(position.Y);

			var topLeft = new Vector3(topLeftX, GetTerrainHeightAt(topLeftX, topLeftY), topLeftY);
			var topRight = new Vector3(topLeftX + 1, GetTerrainHeightAt(topLeftX + 1, topLeftY), topLeftY);
			var botLeft = new Vector3(topLeftX, GetTerrainHeightAt(topLeftX, topLeftY + 1), topLeftY + 1);
			var botRight = new Vector3(topLeftX + 1, GetTerrainHeightAt(topLeftX + 1, topLeftY + 1), topLeftY + 1);

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

		public void HighlightCornerList(IEnumerable<IntVector2> corners, Color color)
		{
			HighlightCornerList(corners, (_) => color);
		}

		public void HighlightCornerList(IEnumerable<IntVector2> corners, Func<IntVector2, Color> getColor)
		{
			graphics.HighlightCornerList(corners, getColor);
		}

		public void HighlightTileList(IEnumerable<ITile> tiles, Func<ITile, Color> getColor)
		{
			graphics.HighlightTileList(tiles, getColor);
		}

		public void HighlightTileList(IEnumerable<ITile> tiles, Color color)
		{
			HighlightTileList(tiles, (tile) => color);
		}

		public void HighlightRectangle(IntVector2 topLeft, IntVector2 bottomRight, Func<ITile, Color> getColor)
		{
			SquishToMap(ref topLeft, ref bottomRight);
			graphics.HighlightRectangle(new IntRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y), getColor);
		}

		public void HighlightRectangle(ITile center, IntVector2 size, Func<ITile, Color> getColor) {
			IntVector2 topLeft = center.TopLeft - (size / 2);
			IntVector2 bottomRight = topLeft + (size - new IntVector2(1, 1));
			HighlightRectangle(topLeft, bottomRight, getColor);
		}

		public void HighlightRectangle(IntRect rectangle, Func<ITile, Color> getColor) {
			HighlightRectangle(rectangle.TopLeft(), rectangle.BottomRight(), getColor);
		}

		public void HighlightRectangle(IntVector2 topLeft, IntVector2 bottomRight, Color color)
		{
			HighlightRectangle(topLeft, bottomRight, (tile) => color);
		}

		public void HighlightRectangle(ITile center, IntVector2 size, Color color)
		{
			HighlightRectangle(center, size, (tile) => color);
		}
		
		public void HighlightRectangle(IntRect rectangle, Color color)
		{
			HighlightRectangle(rectangle, (tile) => color);
		}

		public void HighlightRectangleBorder(IntVector2 topLeft, IntVector2 bottomRight, Color color) {
			SquishToMap(ref topLeft, ref bottomRight);
			graphics.HighlightBorder(new IntRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y), color);
		}

		public void HighlightRectangleBorder(ITile center, IntVector2 size, Color color) {
			IntVector2 topLeft = center.TopLeft - (size / 2);
			IntVector2 bottomRight = topLeft + (size - new IntVector2(1, 1));
			HighlightRectangleBorder(topLeft, bottomRight, color);
		}

		public void HighlightRectangleBorder(IntRect rectangle, Color color)
		{
			HighlightRectangleBorder(rectangle.TopLeft(), rectangle.BottomRight(), color);
		}


		public void DisableHighlight() 
		{
			graphics.DisableHighlight();
		}

		public void ChangeHeight(IEnumerable<IntVector2> tileCorners, float heightDelta) 
		{

			foreach (var tileCorner in tileCorners) {
				ChangeCornerHeight(tileCorner, heightDelta);

				//With border tiles
				ForEachAroundCorner(tileCorner, 
									(changedTile) => {
										changedTile.CornerHeightChange();
									},
									 true);

				//Without border tiles, so they dont leak out of the implementation
				ForEachAroundCorner(tileCorner,
									(changedTile) => {
										TileHeightChanged?.Invoke(changedTile);
									},
									false);
				
			}

			graphics.ChangeCornerHeights(tileCorners);
		}

		public void ChangeHeightTo(IEnumerable<IntVector2> tileCorners, float newHeight) 
		{
			foreach (var tileCorner in tileCorners) {
				SetCornerHeight(tileCorner, newHeight);

				//With border tiles
				ForEachAroundCorner(tileCorner,
									(changedTile) => {
										changedTile.CornerHeightChange();
									},
									true);

				//Without border tiles, so they dont leak out of the implementation
				ForEachAroundCorner(tileCorner,
									(changedTile) => {
										TileHeightChanged?.Invoke(changedTile);
									},
									false);

			}

			graphics.ChangeCornerHeights(tileCorners);
		}

		/// <summary>
		/// Gets tile containing <paramref name="point"/> projection into the XZ plane
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public ITile GetContainingTile(Vector3 point) 
		{
			return GetContainingTile(point.XZ2());
		}

		/// <summary>
		/// Gets tile containing <paramref name="point"/> in the XZ plane
		/// </summary>
		/// <param name="point">The point in the XZ plane</param>
		/// <returns>The tile containing <paramref name="point"/></returns>
		public ITile GetContainingTile(Vector2 point)
		{
			return GetContainingTile(point.X, point.Y);
		}

		public ITile GetContainingTile(float x, float z)
		{
			int topLeftX = (int)Math.Floor(x);
			int topLeftZ = (int)Math.Floor(z);
			return GetTileByTopLeftCorner(topLeftX, topLeftZ);
		}

		public void ForEachInRectangle(IntVector2 topLeft, IntVector2 bottomRight, Action<ITile> action) 
		{
			for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
				for (int x = topLeft.X; x <= bottomRight.X; x++) {
					var tile = GetTileByTopLeftCorner(x, y);
					if (tile != null) {
						action(tile);
					}
					
				}
			}
		}

		public void ForEachInRectangle(IntRect rectangle, Action<ITile> action) 
		{
			ForEachInRectangle(rectangle.TopLeft(), rectangle.BottomRight(), action);
		}

		public void ForEachAroundCorner(IntVector2 cornerCoords, Action<ITile> action)
		{
			ForEachAroundCorner(cornerCoords, action, false);
		}

		public IRangeTarget GetRangeTarget(Vector3 position) 
		{
			if (!mapRangeTargets.TryGetValue(position, out MapRangeTarget mapTarget)) {
				mapTarget = MapRangeTarget.CreateNew(levelManager, position);
				mapRangeTargets.Add(position, mapTarget);
			}

			return mapTarget;
		}

		public Vector3 GetBorderBetweenTiles(ITile tile1, ITile tile2)
		{
			Vector2 borderPosition = (tile1.Center + tile2.Center) / 2;
			return new Vector3(borderPosition.X, GetTerrainHeightAt(borderPosition), borderPosition.Y);
		}

		internal void RemoveRangeTarget(MapRangeTarget mapRangeTarget) 
		{
			var removed = mapRangeTargets.Remove(mapRangeTarget.CurrentPosition);
			Debug.Assert(removed);
		}

		public void Dispose() 
		{
			((IDisposable) graphics).Dispose();
			node.Dispose();
		}

		void BuildGeometry(LoadingWatcher loadingProgress)
		{
			graphics = MapGraphics.Build(this, 
										 ChunkSize,
										 loadingProgress);
		}

		int GetTileIndex(int x, int y) 
		{
			return x + y * WidthWithBorders;
		}

		int GetTileIndex(IntVector2 location) 
		{
			return GetTileIndex(location.X, location.Y);
		}

		int GetTileIndex(ITile tile) 
		{
			return GetTileIndex(tile.MapLocation);
		}

	 
		bool IsOuterBorderPoint(int x, int y) 
		{
			return IsLeftOuterBorderPoint(x,y) ||
				   IsRightOuterBorderPoint(x,y) ||
				   IsTopOuterBorderPoint(x,y) ||
				   IsBottomOuterBorderPoint(x,y);

		}

		bool IsOuterBorderPoint(IntVector2 location) 
		{
			return IsOuterBorderPoint(location.X, location.Y);
		}

		bool IsBorderTileMapLocation(int x, int y)
		{
			return IsTopOuterBorderPoint(x, y) ||
					IsLeftOuterBorderPoint(x, y) ||
					IsBottomInnerBorderPoint(x, y) ||
					IsRightInnerBorderPoint(x, y);
		}

		bool IsBorderTileMapLocation(IntVector2 point)
		{
			return IsBorderTileMapLocation(point.X, point.Y);
		}

		bool IsBorderTile(ITile tile) 
		{
			return IsBorderTileMapLocation(tile.MapLocation);
		}

		BorderType GetBorderTileType(int tileMapLocationX, int tileMapLocationY) 
		{
			/*Top and Left are Outer borders and Bottom and Rigth inner because
			 tile mapLocation are the coordinates of its top left corner
			 */ 

			if (IsLeftOuterBorderPoint(tileMapLocationX,tileMapLocationY)) {
				//Left && Top
				if (IsTopOuterBorderPoint(tileMapLocationX, tileMapLocationY)) {
					return BorderType.TopLeft;
				}
				//Left && Bottom
				else if (IsBottomInnerBorderPoint(tileMapLocationX, tileMapLocationY)) {
					return BorderType.BottomLeft;
				}
				//Left && (!Bottom && !Top)
				else {
					return BorderType.Left;
				}
			}

			else if (IsRightInnerBorderPoint(tileMapLocationX, tileMapLocationY)) {
				if (IsTopOuterBorderPoint(tileMapLocationX, tileMapLocationY)) {
					return BorderType.TopRight;
				}
				else if (IsBottomInnerBorderPoint(tileMapLocationX, tileMapLocationY)) {
					return BorderType.BottomRight;
				}
				else {
					return BorderType.Right;
				}
				
			}
			else if (IsTopOuterBorderPoint(tileMapLocationX, tileMapLocationY)) {
				//We already know its not left or right border
				return BorderType.Top;
			}
			else if (IsBottomInnerBorderPoint(tileMapLocationX, tileMapLocationY)) {
				//We already know its not left or right border
				return BorderType.Bottom;
			}
			else {
				return BorderType.None;
			}
			
		}

		BorderType GetBorderTileType(IntVector2 tileMapLocation) 
		{
			return GetBorderTileType(tileMapLocation.X, tileMapLocation.Y);
		}

		bool IsTopOuterBorderPoint(int x, int y)
		{
			return y == TopWithBorders;
		}

		bool IsTopOuterBorderPoint(IntVector2 location) 
		{
			return IsTopOuterBorderPoint(location.X, location.Y);
		}

		bool IsBottomOuterBorderPoint(int x, int y)
		{
			return y == BottomWithBorders;
		}

		bool IsBottomOuterBorderPoint(IntVector2 location) 
		{
			return IsBottomOuterBorderPoint(location.X, location.Y);
		}

		bool IsLeftOuterBorderPoint(int x, int y)
		{
			return x == LeftWithBorders;
		}

		bool IsLeftOuterBorderPoint(IntVector2 location) 
		{
			return IsLeftOuterBorderPoint(location.X, location.Y);
		}

		bool IsRightOuterBorderPoint(int x, int y)
		{
			return x == RightWithBorders;
		}

		bool IsRightOuterBorderPoint(IntVector2 location) 
		{
			return IsRightOuterBorderPoint(location.X, location.Y);
		}

		bool IsTopInnerBorderPoint(int x, int y)
		{
			return y == Top;
		}

		bool IsTopInnerBorderPoint(IntVector2 point)
		{
			return IsTopInnerBorderPoint(point.X, point.Y);
		}

		bool IsBottomInnerBorderPoint(int x, int y)
		{
			return y == Top + Length;
		}

		bool IsBottomInnerBorderPoint(IntVector2 point)
		{
			return IsBottomInnerBorderPoint(point.X, point.Y);
		}

		bool IsLeftInnerBorderPoint(int x, int y)
		{
			return x == Left;
		}

		bool IsLeftInnerBorderPoint(IntVector2 point)
		{
			return IsLeftInnerBorderPoint(point.X, point.Y);
		}

		bool IsRightInnerBorderPoint(int x, int y)
		{
			return x == Left + Width;
		}

		bool IsRightInnerBorderPoint(IntVector2 point)
		{
			return IsRightInnerBorderPoint(point.X, point.Y);
		}

		/// <summary>
		/// Gets tile by topLeft corner coordinates
		/// </summary>
		/// <param name="x">top left corner X coord</param>
		/// <param name="y">top left corner Y coord</param>
		/// <returns></returns>
		ITile GetTileWithBorders(int x, int y)
		{
			if (LeftWithBorders <= x &&
				x <= RightWithBorders &&
				TopWithBorders <= y &&
				y <= BottomWithBorders) {

				return tiles[GetTileIndex(x,y)];
			}
			return null;
		}

		ITile GetTileWithBorders(IntVector2 coords) 
		{
			return GetTileWithBorders(coords.X, coords.Y);
		}

		ITile TileByTopLeftCorner(IntVector2 topLeftCorner, bool withBorders) 
		{
			return withBorders ? GetTileWithBorders(topLeftCorner) : GetTileByTopLeftCorner(topLeftCorner);
		}

		ITile TileByTopRightCorner(IntVector2 topRightCorner, bool withBorders) 
		{
			IntVector2 topLeft = topRightCorner + new IntVector2(-1, 0);
			return withBorders ? GetTileWithBorders(topLeft) : GetTileByTopLeftCorner(topLeft);
		}

		ITile TileByBottomLeftCorner(IntVector2 bottomLeftCorner, bool withBorders) 
		{
			IntVector2 topLeft = bottomLeftCorner + new IntVector2(0, -1);
			return withBorders ? GetTileWithBorders(topLeft) : GetTileByTopLeftCorner(topLeft);
		}
		ITile TileByBottomRightCorner(IntVector2 bottomRightCorner, bool withBorders) 
		{
			IntVector2 topLeft = bottomRightCorner + new IntVector2(-1, -1);
			return withBorders ? GetTileWithBorders(topLeft) : GetTileByTopLeftCorner(topLeft);
		}

		bool IsBorderCorner(int x, int y) 
		{
			return (LeftWithBorders <= x && x <= Left) ||
				   (Right <= x && x <= RightWithBorders + 1) || //+1 because RightWithBorders contains coords of the topLeftCorner
				   (TopWithBorders <= y && y <= Top) ||
				   (Bottom <= y && y <= BottomWithBorders + 1);
		}

		bool IsBorderCorner(IntVector2 corner) 
		{
			return IsBorderCorner(corner.X, corner.Y);
		}

		void ForEachAroundCorner(IntVector2 cornerCoords, Action<ITile> action, bool withBorders)
		{
			var tilesAroundCorner = GetTilesAroundCorner(cornerCoords, withBorders);

			foreach (var tile in tilesAroundCorner) {
				action(tile);
			}
		}

		IEnumerable<ITile> GetTilesAroundCorner(IntVector2 cornerCoords, bool withBorders)
		{
			ITile tile;

			if ((tile = TileByTopLeftCorner(cornerCoords, withBorders)) != null) {
				yield return tile;
			}

			if ((tile = TileByTopRightCorner(cornerCoords, withBorders)) != null) {
				yield return tile;
			}

			if ((tile = TileByBottomRightCorner(cornerCoords, withBorders)) != null) {
				yield return tile;
			}

			if ((tile = TileByBottomLeftCorner(cornerCoords, withBorders)) != null) {
				yield return tile;
			}
		}

		bool IsTileSplitFromTopLeftToBottomRight(float topLeftHeight,
														 float topRightHeight,
														 float bottomLeftHeight,
														 float bottomRightHeight) 
		{
			return topLeftHeight + bottomRightHeight >= topRightHeight + bottomLeftHeight;
		}

		bool IsTileSplitFromTopLeftToBottomRight(ITile tile) 
		{
			return IsTileSplitFromTopLeftToBottomRight(tile.TopLeftHeight,
													   tile.TopRightHeight,
													   tile.BottomLeftHeight,
													   tile.BottomRightHeight);
		}

		ITile SearchLineInX(IntVector2 source, int length, Predicate<ITile> predicate)
		{
			if (!IsZInside(source.Y)) return null;

			source.X = Math.Max(source.X, Left);
			length = Math.Min(Right - source.X, length);
			for (int i = 0; i < length; i++) {
				ITile tile = GetTileByMapLocation(new IntVector2(source.X + i, source.Y));
				if (predicate(tile)) {
					return tile;
				}
			}
			return null;
		}

		ITile SearchLineInY(IntVector2 source, int length, Predicate<ITile> predicate)
		{
			if (!IsXInside(source.X)) return null;

			source.Y = Math.Max(source.Y, Top);
			length = Math.Min(Bottom - source.Y, length);
			for (int i = 0; i < length; i++) {
				ITile tile = GetTileByMapLocation(new IntVector2(source.X, source.Y + i));
				if (predicate(tile)) {
					return tile;
				}
			}
			return null;
		}

		/// <summary>
		/// Changes heights in the logic tiles, does not change the height in the map model
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="height"></param>
		void SetCornerHeight(int x, int y, float height)
		{
			if (IsBorderCorner(x, y)) {
				foreach (var tile in GetTilesAroundCorner(new IntVector2(x, y), true)) {
					if (IsBorderTile(tile)) {
						bool left = tile.TopLeft.X == x;
						bool top = tile.TopLeft.Y == y;
						if (top && left) {
							((BorderTile)tile).TopLeftHeight = height;
						}
						else if (top /* && !left*/) {
							((BorderTile)tile).TopRightHeight = height;
						}
						else if ( /* !top && */ left) {
							((BorderTile)tile).BottomLeftHeight = height;
						}
						else /*!top && !left*/{
							((BorderTile)tile).BottomRightHeight = height;
						}
					}
					else if (tile.TopLeft == new IntVector2(x, y)) {
						tile.SetTopLeftHeight(height);
					}
				}
			}
			else {
				GetTileByTopLeftCorner(x, y).SetTopLeftHeight(height);
			}
		
		}

		void SetCornerHeight(IntVector2 cornerPosition, float height)
		{
			SetCornerHeight(cornerPosition.X, cornerPosition.Y, height);
		}

		

		/// <summary>
		/// Changes heights in the logic tiles, does not change the height in the map model
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="height"></param>
		void ChangeCornerHeight(int x, int y, float heightDelta)
		{
			if (IsBorderCorner(x, y)) {
				foreach (var tile in GetTilesAroundCorner(new IntVector2(x, y), true)) {
					if (IsBorderTile(tile)) {
						bool left = tile.TopLeft.X == x;
						bool top = tile.TopLeft.Y == y;
						if (top && left) {
							((BorderTile)tile).TopLeftHeight += heightDelta;
						}
						else if (top /* && !left*/) {
							((BorderTile)tile).TopRightHeight += heightDelta;
						}
						else if ( /* !top && */ left) {
							((BorderTile)tile).BottomLeftHeight += heightDelta;
						}
						else /*!top && !left*/{
							((BorderTile)tile).BottomRightHeight += heightDelta;
						}
					}
					else if (tile.TopLeft == new IntVector2(x, y)) {
						tile.ChangeTopLeftHeight(heightDelta);
					}
				}
			}
			else {
				GetTileByTopLeftCorner(x, y).ChangeTopLeftHeight(heightDelta);
			}

		}

		void ChangeCornerHeight(IntVector2 cornerPosition, float heightDelta)
		{
			ChangeCornerHeight(cornerPosition.X, cornerPosition.Y, heightDelta);
		}
	}
}
