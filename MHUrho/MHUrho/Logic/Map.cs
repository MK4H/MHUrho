﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Graphics;
using Urho;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.Helpers;


namespace MHUrho.Logic
{
    public class Map : IMap, IDisposable {

        private class BorderTile : ITile {
            public Unit Unit => throw new NotImplementedException();

            public List<Unit> PassingUnits => throw new NotImplementedException();

            public float MovementSpeedModifier => throw new NotImplementedException();

            public TileType Type { get; private set; }


            public IntRect MapArea { get; private set; }

            /// <summary>
            /// Location in the Map matrix
            /// </summary>
            public IntVector2 Location => new IntVector2(MapArea.Left, MapArea.Top);

            public Vector2 Center => new Vector2(Location.X + 0.5f, Location.Y + 0.5f);

            public float Height { get; private set; }

            private StTile storage;

            public static ITile LoadTileOrBorder(StTile storedTile, Map map) {
                if (map.IsBorder(storedTile.Position.X, storedTile.Position.Y)) {
                    return new BorderTile(storedTile);
                }
                else {
                    return Tile.StartLoading(storedTile);
                }
            }

            public void ConnectReferences() {
                Type = PackageManager.Instance.GetTileType(storage.TileTypeID);
            }

            public void FinishLoading() {
                storage = null;
            }

            public bool SpawnUnit(Player player) {
                throw new NotImplementedException();
            }

            public void AddPassingUnit(Unit unit) {
                throw new NotImplementedException();
            }

            public bool TryAddOwningUnit(Unit unit) {
                throw new NotImplementedException();
            }

            public void RemoveUnit(Unit unit) {
                throw new NotImplementedException();
            }

            public StTile Save() {
                var storedTile = new StTile();
                storedTile.UnitID = 0;
                storedTile.Position = new StIntVector2 { X = Location.X, Y = Location.Y };
                storedTile.Height = Height;
                storedTile.TileTypeID = Type.ID;

                return storedTile;
            }

            public void ChangeType(TileType newType) {
                Type = newType;
            }

            public BorderTile(StTile stTile) {
                this.storage = stTile;
                this.MapArea = new IntRect(stTile.Position.X, stTile.Position.Y, stTile.Position.X + 1, stTile.Position.Y + 1);
                this.Height = stTile.Height;
            }

            public BorderTile(int x, int y, TileType tileType) {
                MapArea = new IntRect(x, y, x + 1, y + 1);
                this.Type = tileType;
                this.Height = 0;
            }
        }

        private readonly ITile[] tiles;
       
       
        /// <summary>
        /// Coordinates of the top left corner of the map
        /// </summary>
        public IntVector2 TopLeft { get; private set; }


        /// <summary>
        /// Coordinates of the bottom right corner of the map
        /// </summary>
        public IntVector2 BottomRight { get; private set; }

        public int Width => Right - Left;

        public int Height => Bottom - Top;

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

        private float borderHeight;

        private int WidthWithBorders => Width + 2;

        private int HeightWithBorders => Height + 2;

        public static Map CreateDefaultMap(Node mapNode, IntVector2 size, float borderHeight) {
            Map newMap = new Map(mapNode, size.X, size.Y, borderHeight);

            TileType defaultTileType = PackageManager.Instance.DefaultTileType;

            for (int i = 0; i < newMap.tiles.Length; i++) {
                IntVector2 tilePosition = new IntVector2(i % newMap.WidthWithBorders, i / newMap.HeightWithBorders);
                if (newMap.IsBorder(tilePosition)) {
                    newMap.tiles[i] = new BorderTile(tilePosition.X, tilePosition.Y, defaultTileType);
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
            try {
                int i = 0;
                foreach (var tile in storedMap.Tiles) {
                    newMap.tiles[i++] = BorderTile.LoadTileOrBorder(tile, newMap);
                }

                if (i < newMap.tiles.Length) {
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
            stSize.Y = Height;
            storedMap.Size = stSize;

            var storedTiles = storedMap.Tiles;

            foreach (var tile in tiles) {
                storedTiles.Add(tile.Save());
            }

            return storedMap;
        }

        protected Map(Node mapNode, StMap storedMap)
            :this(mapNode, storedMap.Size.X, storedMap.Size.Y, storedMap.BorderHeight) {

        }

        protected Map(Node mapNode, int width, int length, float borderHeight) {
            this.node = mapNode;
            this.TopLeft = new IntVector2(1, 1);
            this.BottomRight = new IntVector2(width, length);
            //To make room for the borderTiles
            this.tiles = new ITile[(Width + 2) * (Height + 2)];
            this.borderHeight = borderHeight;
        }

        internal static Map CreateTestMap(ITile[] newTiles, IntVector2 size) {
            return new Map(newTiles, size);
        }

        private Map(ITile[] tiles, IntVector2 size) {
            this.tiles = tiles;
            this.TopLeft = new IntVector2(0, 0);
            this.BottomRight = new IntVector2(size.X - 1, size.Y - 1);
        }

        /// <summary>
        /// Checks if the point is inside the map, which means it could be used for indexing into the map
        /// </summary>
        /// <param name="point">the point to check</param>
        /// <returns>True if it is inside, False if not</returns>
        public bool IsInside(IntVector2 point) {
            return IsInside(point.X, point.Y);
        }

        public bool IsInside(int x, int y) {
            return Left <= x && x <= Right && Top <= y && y <= Bottom;
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
            return tiles[x + y * Width];
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
            IntVector2 bottomRight = topLeft + rectangleSize;
            SquishToMap(ref topLeft, ref bottomRight);
            rectangleSize = bottomRight - topLeft;

            ForEachInRectangle(topLeft, bottomRight, (tile) => { tile.ChangeType(newType); });
            graphics.ChangeTileType(topLeft, newType, rectangleSize);
        }

        //TODO: Handle right and bottom side tiles better
        public float GetHeightAt(int x, int y) {
            ITile tile;
            if ((tile = SafeGetTile(x,y)) != null) {
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
            else if (y >= Height) {
                y = Height - 1;
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
                                         new IntVector2(WidthWithBorders, HeightWithBorders),
                                         IsTopBorder,
                                         IsBottomBorder,
                                         IsLeftBorder,
                                         IsRightBorder);
        }

        private ITile SafeGetTile(int x, int y) {
            if (0 <= x && x < Width && 0 <= y && y < Height) {
                return GetTile(x, y);
            }
            return null;
        }

        private ITile SafeGetTile(IntVector2 position) {
            return SafeGetTile(position.X, position.Y);
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
            return (0 <= x && x < Left) ||
                   (Right < x && x < WidthWithBorders) ||
                   (0 <= y && y < Top) ||
                   (Bottom < y && y < WidthWithBorders);

        }

        private bool IsBorder(IntVector2 location) {
            return IsBorder(location.X, location.Y);
        }

        private bool IsTopBorder(int x, int y) {
            return (0 <= y && y < Top);
        }

        private bool IsTopBorder(IntVector2 location) {
            return IsTopBorder(location.X, location.Y);
        }

        private bool IsBottomBorder(int x, int y) {
            return (Bottom < y && y < WidthWithBorders);
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
            return (Right < x && x < WidthWithBorders);
        }

        private bool IsRightBorder(IntVector2 location) {
            return IsRightBorder(location.X, location.Y);
        }
    }
}
