using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;

namespace MHUrho.Logic
{
    class Map {
        private Tile[][] contents;

        public int Width { get; private set; }

        public int Height { get; private set; }

        /// <summary>
        /// X coordinate of the left row of the map
        /// </summary>
        public int Left => 0;
        /// <summary>
        /// X coordinate of the right row of the map
        /// </summary>
        public int Right => Width - 1;
        /// <summary>
        /// Y coordinate of the top row of the map
        /// </summary>
        public int Top => 0;
        /// <summary>
        /// Y coordinate of the bottom row of the map
        /// </summary>
        public int Bottom => Height - 1;

        /// <summary>
        /// Checks if the point is inside the map, which means it could be used for indexing into the map
        /// </summary>
        /// <param name="point">the point to check</param>
        /// <returns>True if it is inside, False if not</returns>
        public bool IsInside(Point point) {
            return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
        }

        /// <summary>
        /// Gets tile at the coordinates [x,y]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>the tile at [x,y]</returns>
        public Tile GetTile(int x, int y) {
            return contents[x][y];
        }

        /// <summary>
        /// Gets tile at the coordinates
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns>the tile at [X,Y]</returns>
        public Tile GetTile(Point coordinates) {
            return contents[coordinates.X][coordinates.Y];
        }

    }
}
