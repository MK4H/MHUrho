using Urho;

namespace MHUrho.Logic {
    public interface IMap {
        Model Model { get; }

        Material Material { get; }

        /// <summary>
        /// Coordinates of the top left corner of the map
        /// </summary>
        IntVector2 TopLeft { get; }

        /// <summary>
        /// Coordinates of the bottom right corner of the map
        /// </summary>
        IntVector2 BottomRight { get; }

        int Width { get; }
        int Height { get; }

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

        /// <summary>
        /// Checks if the point is inside the map, which means it could be used for indexing into the map
        /// </summary>
        /// <param name="point">the point to check</param>
        /// <returns>True if it is inside, False if not</returns>
        bool IsInside(IntVector2 point);

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

        /// <summary>
        /// Gets tile at the coordinates [x,y]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>the tile at [x,y]</returns>
        ITile GetTile(int x, int y);

        /// <summary>
        /// Gets tile at the coordinates
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns>the tile at [X,Y]</returns>
        ITile GetTile(IntVector2 coordinates);

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
    }
}