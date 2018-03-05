using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Graphics;
using Urho;
using MHUrho.Packaging;
using MHUrho.Storage;


namespace MHUrho.Logic
{
    public class Map : IMap, IDisposable {

        private readonly Tile[] contents;
       
        public MapGraphics Graphics { get; private set; }

        /// <summary>
        /// Coordinates of the top left corner of the map
        /// </summary>
        public IntVector2 TopLeft { get; private set; }


        /// <summary>
        /// Coordinates of the bottom right corner of the map
        /// </summary>
        public IntVector2 BottomRight { get; private set; }

        public int Width => Right + 1;

        public int Height => Bottom + 1;

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

        private readonly Node node;

        //TODO: Split map into chunks, that will be separately in memory

        public static Map CreateDefaultMap(Node mapNode, IntVector2 size) {

            int tileCount = size.X * size.Y;
            Tile[] tiles = new Tile[tileCount];

            TileType defaultTileType = PackageManager.Instance.DefaultTileType;

            for (int i = 0; i < tileCount; i++) {
                tiles[i] = new Tile(i % size.X, i / size.X, defaultTileType);
            }

            Map newMap = new Map(mapNode, size.X, size.Y, tiles);
            newMap.BuildGeometry();
            return newMap;
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
        public static Map StartLoading(Node mapNode, StMap storedMap) {
            var newMap = new Map(mapNode, storedMap);
            try {
                int i = 0;
                foreach (var tile in storedMap.Tiles) {
                    newMap.contents[i++] = Tile.StartLoading(tile);
                }

                if (i < newMap.contents.Length) {
                    throw new ArgumentException();
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

            return newMap;
        }

        public void ConnectReferences() {
            foreach (var tile in contents) {
                tile.ConnectReferences();
            }
        }

        /// <summary>
        /// Builds geometry and releases stored data
        /// </summary>
        public void FinishLoading() {
            foreach (var tile in contents) {
                tile.FinishLoading();
            }

            BuildGeometry();
        }

        public StMap Save() {
            var storedMap = new StMap();
            var StSize = new StIntVector2();
            StSize.X = Width;
            StSize.Y = Height;
            storedMap.Size = StSize;

            var storedTiles = storedMap.Tiles;

            foreach (var tile in contents) {
                storedTiles.Add(tile.Save());
            }

            return storedMap;
        }

        protected Map(Node mapNode, StMap storedMap) {
            this.node = mapNode;
            this.TopLeft = new IntVector2(0, 0);
            this.BottomRight = new IntVector2(storedMap.Size.X - 1, storedMap.Size.Y - 1);
            this.contents = new Tile[Width * Height];
        }

        protected Map(Node mapNode, int width, int height, Tile[] contents) {
            this.node = mapNode;
            TopLeft = new IntVector2(0, 0);
            BottomRight = new IntVector2(width - 1, height - 1);
            this.contents = contents;
        }


       

        /// <summary>
        /// Checks if the point is inside the map, which means it could be used for indexing into the map
        /// </summary>
        /// <param name="point">the point to check</param>
        /// <returns>True if it is inside, False if not</returns>
        public bool IsInside(IntVector2 point) {
            return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
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

        /// <summary>
        /// Gets tile at the coordinates [x,y]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>the tile at [x,y]</returns>
        public ITile GetTile(int x, int y) {
            return contents[x + y * Width];
        }

        /// <summary>
        /// Gets tile at the coordinates
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns>the tile at [X,Y]</returns>
        public ITile GetTile(IntVector2 coordinates) {
            return GetTile(coordinates.X, coordinates.Y);
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
            int recHeight = bottomRight.Y - topLeft.Y;

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

            if (recHeight > Height) {
                //Rectangle is wider than the map, center it on the map
                int diff = recHeight - Height;
                topLeft.Y = diff / 2;
                bottomRight.Y = topLeft.Y + recHeight;

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
                        IntVector2 pos = closestTo.Location;
                        pos.X += dx;
                        pos.Y += dy;
                        if (!IsInside(pos)) {
                            continue;
                        }

                        if (GetTile(pos).Unit == null) {
                            return GetTile(pos);
                        }
                    }
                }
                dist++;
                //TODO: Cutoff
            }
        }


        private void BuildGeometry() {
            Graphics = MapGraphics.Build(node, contents, new IntVector2(Width, Height));
        }

        public void Dispose() {
            ((IDisposable) Graphics).Dispose();
            node.Dispose();
        }
    }
}
