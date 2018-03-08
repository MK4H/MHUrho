using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Control;
using Urho;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.Helpers;
using MHUrho.Logic;


namespace MHUrho.WorldMap
{
    public partial class Map : IMap, IDisposable {

        private class BorderTile : ITile {
            Unit ITile.Unit => throw new InvalidOperationException("Cannot add unit to Border tile");

            List<Unit> ITile.PassingUnits => throw new InvalidOperationException("Cannot add unit to Border tile");

            float ITile.MovementSpeedModifier => throw new InvalidOperationException("Cannot move through Border tile");

            public TileType Type { get; private set; }


            public IntRect MapArea { get; private set; }

            /// <summary>
            /// Location in the Map matrix
            /// </summary>
            public IntVector2 Location => new IntVector2(MapArea.Left, MapArea.Top);

            public Vector2 Center => new Vector2(Location.X + 0.5f, Location.Y + 0.5f);

            public float Height {
                get => TopLeftHeight;
                private set => TopLeftHeight = value;
            }

            public float TopLeftHeight { get; set; }
            public float TopRightHeight { get; set; }
            public float BotLeftHeight { get; set; }
            public float BotRightHeight { get; set; }

            public BorderType BorderType { get; private set; }

            private StBorderTile storage;

            public void ConnectReferences() {
                Type = PackageManager.Instance.GetTileType(storage.TileTypeID);
            }

            public void FinishLoading() {
                storage = null;
            }

            bool ITile.SpawnUnit(Player player) {
                throw new InvalidOperationException("Cannot add unit to Border tile");
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

            StTile ITile.Save() {
                throw new InvalidOperationException("Cannot save BorderTile as a tile");
            }

            public StBorderTile Save() {
                var stBorderTile = new StBorderTile();
                stBorderTile.Position = new StIntVector2 { X = Location.X, Y = Location.Y };
                stBorderTile.TileTypeID = Type.ID;
                stBorderTile.TopLeftHeight = TopLeftHeight;
                stBorderTile.TopRightHeight = TopRightHeight;
                stBorderTile.BotLeftHeight = BotLeftHeight;
                stBorderTile.BotRightHeight = BotRightHeight;


                return stBorderTile;
            }

            public void ChangeType(TileType newType) {
                Type = newType;
            }

            public BorderTile(StBorderTile stBorderTile, Map map) {
                this.storage = stBorderTile;
                this.MapArea = new IntRect(stBorderTile.Position.X, stBorderTile.Position.Y, stBorderTile.Position.X + 1, stBorderTile.Position.Y + 1);
                this.TopLeftHeight = stBorderTile.TopLeftHeight;
                this.TopRightHeight = stBorderTile.TopRightHeight;
                this.BotLeftHeight = stBorderTile.BotLeftHeight;
                this.BotRightHeight = stBorderTile.BotRightHeight;
                BorderType = map.GetBorderType(this.Location);
            }

            public BorderTile(int x, int y, TileType tileType, BorderType borderType) {
                MapArea = new IntRect(x, y, x + 1, y + 1);
                this.Type = tileType;
                this.BorderType = borderType;
                this.TopLeftHeight = 0;
                this.TopRightHeight = 0;
                this.BotLeftHeight = 0;
                this.BotRightHeight = 0;
            }
        }

        private readonly ITile[] tiles;
       
       
        /// <summary>
        /// Coordinates of the top left tile of the playing map
        /// </summary>
        public IntVector2 TopLeft { get; private set; }


        /// <summary>
        /// Coordinates of the bottom right tile of the playing map
        /// </summary>
        public IntVector2 BottomRight { get; private set; }

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

        private readonly Node node;

        private MapGraphics graphics;
        /// <summary>
        /// Width of the whole map with borders included
        /// </summary>
        private int WidthWithBorders => Width + 2;
        /// <summary>
        /// Length of the whole map with the borders included
        /// </summary>
        private int LengthWithBorders => Length + 2;

        /// <summary>
        /// Creates default map at height 0 with all tiles with the default type
        /// </summary>
        /// <param name="mapNode">Node to connect the map to</param>
        /// <param name="size">Size of the playing field, excluding the borders</param>
        /// <returns>Fully created map</returns>
        public static Map CreateDefaultMap(Node mapNode, IntVector2 size) {
            Map newMap = new Map(mapNode, size.X, size.Y);

            TileType defaultTileType = PackageManager.Instance.DefaultTileType;

            for (int i = 0; i < newMap.tiles.Length; i++) {
                IntVector2 tilePosition = new IntVector2(i % newMap.WidthWithBorders, i / newMap.LengthWithBorders);
                if (newMap.IsBorder(tilePosition)) {
                    BorderType borderType = newMap.GetBorderType(tilePosition.X, tilePosition.Y);

                    Debug.Assert(borderType != BorderType.None,
                                 "Error in implementation of IsBorder or GetBorderType");

                    newMap.tiles[i] = new BorderTile(tilePosition.X, tilePosition.Y, defaultTileType, borderType);
                }
                else {
                    newMap.tiles[i] = new Tile(tilePosition.X, tilePosition.Y, defaultTileType);
                }
            }

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
            var tiles = storedMap.Tiles.GetEnumerator();
            var borderTiles = storedMap.BorderTiles.GetEnumerator();

            try {
                
                for (int y = 0; y < newMap.LengthWithBorders; y++) {
                    for (int x = 0; x < newMap.WidthWithBorders; x++) {
                        ITile newTile;
                        if (newMap.IsBorder(x, y)) {
                            if (!borderTiles.MoveNext()) {
                                //TODO: Exception
                                throw new Exception("Corrupted save file");
                            }

                            newTile = new BorderTile(borderTiles.Current, newMap);
                        }
                        else {
                            if (!tiles.MoveNext()) {
                                //TODO: Exception
                                throw new Exception("Corrupted save file");
                            }

                            newTile = Tile.StartLoading(tiles.Current);
                        }

                        newMap.tiles[newMap.GetTileIndex(x, y)] = newTile;
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

            return newMap;
        }

        public void ConnectReferences() {
            foreach (var tile in tiles) {
                tile.ConnectReferences();
            }
        }

        /// <summary>
        /// Builds geometry and releases stored data
        /// </summary>
        public void FinishLoading() {
            foreach (var tile in tiles) {
                tile.FinishLoading();
            }

            BuildGeometry();
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
                if (IsBorder(tile.Location)) {
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

            this.tiles = new ITile[WidthWithBorders *  LengthWithBorders];
        }

        internal static Map CreateTestMap(ITile[] newTiles, IntVector2 size) {
            return new Map(newTiles, size);
        }

        private Map(ITile[] tiles, IntVector2 size) {
            this.tiles = tiles;
            this.TopLeft = new IntVector2(0, 0);
            this.BottomRight = new IntVector2(size.X - 1, size.Y - 1);
        }


        public bool IsInside(int x, int y) {
            return Left <= x && x <= Right && Top <= y && y <= Bottom;
        }

        /// <summary>
        /// Checks if the point is inside the map, which means it could be used for indexing into the map
        /// </summary>
        /// <param name="point">the point to check</param>
        /// <returns>True if it is inside, False if not</returns>
        public bool IsInside(IntVector2 point) {
            return IsInside(point.X, point.Y);
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
        /// Gets tile at the coordinates [x,y] or null if [x,y] are outside the playfield
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>the tile at [x,y] or null if [x,y] are out of the playfield</returns>
        public ITile GetTile(int x, int y) {
            if (IsInside(x, y)) {
                return tiles[GetTileIndex(x, y)];
            }
            return null;
        }

        /// <summary>
        /// Gets tile at the coordinates [x,y] or null if [x,y] are outside the playfield
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns>the tile at [x,y] or null if [x,y] are out of the playfield</returns>
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

        public ITile Raycast(RayQueryResult rayQueryResult) {
            return graphics.HandleRaycast(rayQueryResult);
        }

        public void ChangeTileType(ITile tile, TileType newType) {
            if (tile.Type == newType) {
                return;
            }

            tile.ChangeType(newType);
            graphics.ChangeTileType(tile.Location, newType);
        }

        public void ChangeTileType(ITile centerTile, TileType newType, IntVector2 rectangleSize) {
            IntVector2 topLeft = centerTile.Location - (rectangleSize / 2);
            IntVector2 bottomRight = topLeft + (rectangleSize - new IntVector2(1,1));
            SquishToMap(ref topLeft, ref bottomRight);

            ForEachInRectangle(topLeft, bottomRight, (tile) => { tile.ChangeType(newType); });
            graphics.ChangeTileType(topLeft, bottomRight, newType);
        }

        //TODO: Handle right and bottom side tiles better
        public float GetHeightAt(int x, int y) {
            ITile tile;
            if ((tile = GetTile(x,y)) != null) {
                return tile.Height;
            }


            if (x < 0) {
                x = 0;
            }
            else if (x >= Width) {
                x = Width - 1;
            }

            if (y < 0) {
                y = 0;
            }
            else if (y >= Length) {
                y = Length - 1;
            }

            return GetTile(x, y).Height;
        }

        public float GetHeightAt(IntVector2 position) {
            return GetHeightAt(position.X, position.Y);
        }

        public float GetHeightAt(float x, float y) {

            int topLeftX = (int) Math.Floor(x);
            int topLeftY = (int) Math.Floor(y);


            Vector2 topLeftToPoint = new Vector2(x - topLeftX, y -topLeftY);
            // These two heights will be needed always because of the order of indicies in the
            // geometry, which creates an edge between bottomLeft and topRight
            float botLeftHeight = GetHeightAt(topLeftX, topLeftY + 1);
            float topRightHeight = GetHeightAt(topLeftX + 1, topLeftY);
            

                         
            //Barycentric coordinates
            float v = topLeftToPoint.X; //topRight coef
            float w = topLeftToPoint.Y; //bottomLeft coef
            float u = 1.0f - v - w; //topLeft or bottomRight coef

            if (u <= 1) {
                //In top left triangle
                float topLeftHeight = GetHeightAt(topLeftX, topLeftY);
                return u * topLeftHeight + v * topRightHeight + w * botLeftHeight;
            }
            else {
                //In bottom right triangle
                float bottomRightHeight = GetHeightAt(topLeftX + 1, topLeftY + 1);
                float tmp = v;
                v = 1.0f - w;
                w = 1.0f - tmp;
                u = 1.0f - v - w;
                return u * bottomRightHeight + v * topRightHeight + w * botLeftHeight;
            }

         
        }

        public float GetHeightAt(Vector2 position) {
            return GetHeightAt(position.X, position.Y);
        }

        public void HighlightArea(ITile center, IntVector2 size) {
            IntVector2 topLeft = center.Location - (size / 2);
            IntVector2 bottomRight = center.Location + (size / 2);
            SquishToMap(ref topLeft, ref bottomRight);
            graphics.HighlightArea(new IntRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y));
        }

        public void HideHighlight() {
            graphics.HideHighlight();
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

        private void ForEachInRectangle(IntVector2 topLeft, IntVector2 bottomRight, Action<ITile> action) {
            for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
                for (int x = topLeft.X; x <= bottomRight.X; x++) {
                    action(GetTile(x, y));
                }
            }
        }

        private void ForEachInRectangle(IntRect rectangle, Action<ITile> action) {
            ForEachInRectangle(rectangle.TopLeft(), rectangle.BottomRight(), action);
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

    }
}
