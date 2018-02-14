using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Serialization;
using Urho;

namespace MHUrho.Logic
{

    public class LogicManager
    {

        public float GameSpeed { get; set; } = 1f;

        List<Unit> units;
        Map map;
        IPathFindAlg pathFind;



        //TODO: Probably not public
        public Player[] Players;

        /// <summary>
        /// Registers unit after it is spawned by a Tile or a Building
        /// </summary>
        /// <param name="unit">The unit to be registered</param>
        public void RegisterUnit(Unit unit)
        {
            units.Add(unit);
        }

        public Path GetPath(Unit unit, Tile target)
        {
            if (target.Unit != null)
            {
                target = FindClosestEmptyTile(target);
            }

            if (target == null)
            {
                return null;
            }

            var fullPath = pathFind.FindPath(unit, target.Location);
            if (fullPath == null)
            {
                return null;
            }
            else
            {
                return new Path(fullPath, target);
            }
        }

        public Tile TryMoveUnitThroughTileAt(Unit unit, IntVector2 tileIndex)
        {
            Tile TargetTile = map.GetTile(tileIndex);
            //TODO: Out of range Exception
            if (unit.CanPass(TargetTile))
            {
                unit.Tile.RemoveUnit(unit);
                TargetTile.AddPassingUnit(unit);
                return TargetTile;
            }
            else
            {
                return null;
            }
        }

        public Tile TileAt(IntVector2 tileIndex)
        {
            return TileAt(tileIndex.X, tileIndex.Y);
        }

        public Tile TileAt(int x, int y)
        {
            return Map[x][y];
        }

        /// <summary>
        /// Gets an enumerable that enumerates over the whole map
        /// </summary>
        /// <returns>An enumerable that enumerates over the whole map</returns>
        public IEnumerable<Tile> GetMapEnumerator()
        {
            for (int x = 0; x < Map.Length; x++)
            {
                for (int y = 0; y < Map[x].Length; y++)
                {
                    yield return Map[x][y];
                }
            }
        }

        /// <summary>
        /// Moves the rectangle defined by topLeft and bottomRight corners so that
        /// the whole rectangle is inside the map
        /// </summary>
        /// <param name="topLeft">top left corner of the rectangle</param>
        /// <param name="bottomRight">bottom right corner of the rectangle</param>
        public void SnapToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight)
        {

            if (topLeft.X < 0)
            {
                bottomRight.X -= topLeft.X;
                topLeft.X = 0;
            }
            if (topLeft.Y < 0)
            {
                bottomRight.Y -= topLeft.Y;
                topLeft.Y = 0;
            }
            if (bottomRight.X >= Map.Length)
            {
                topLeft.X -= Map.Length - 1 - bottomRight.X;
                bottomRight.X = Map.Length - 1;
            }
            if (bottomRight.Y >= Map[bottomRight.X].Length)
            {
                topLeft.Y -= Map[bottomRight.X].Length - 1 - bottomRight.Y;
                bottomRight.Y = Map[bottomRight.X].Length - 1;
            }
        }


        public void SquishToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight)
        {
            if (topLeft.X < 0)
            {
                topLeft.X = 0;
            }
            if (topLeft.Y < 0)
            {
                topLeft.Y = 0;
            }
            if (bottomRight.X >= Map.Length)
            {
                bottomRight.X = Map.Length - 1;
            }
            if (bottomRight.Y >= Map[bottomRight.X].Length)
            {
                bottomRight.Y = Map[bottomRight.X].Length - 1;
            }
        }

        public bool IsInsideMap(IntVector2 point)
        {
            return point.X >= 0 &&
                point.X < MapWidth &&
                point.Y >= 0 &&
                point.Y < MapHeight;
        }

        //TODO: From XML
        /// <summary>
        /// Loads level logic
        /// </summary>
        /// <returns></returns>
        public static LogicManager Load()
        {
            LogicManager newLevel = new LogicManager();
            //TODO: Load from XML or something
            newLevel.Map = new Tile[300][];
            for (int x = 0; x < 300; x++)
            {
                newLevel.Map[x] = new Tile[300];
                for (int y = 0; y < 300; y++)
                {
                    newLevel.Map[x][y] = new Tile(newLevel, x, y);
                }
            }
            newLevel._MapHeight = 300;
            newLevel.PathFind = new AStar(newLevel.Map);
            //TODO: Load AI players
            newLevel.Players = new Player[4];
            newLevel.Players[0] = new Player(newLevel);
            return newLevel;
        }
        public LogicManager()
        {
            Units = new List<Unit>();

        }
        private Tile FindClosestEmptyTile(Tile closestTo)
        {
            int d = 1;
            while (true)
            {
                for (int dx = -d; dx < d + 1; dx++)
                {
                    for (int dy = -d; dy < d + 1; dy++)
                    {
                        int XIndex = closestTo.XIndex + dx;
                        if (XIndex >= Map.Length || XIndex < 0)
                        {
                            break;
                        }
                        int YIndex = closestTo.YIndex + dy;
                        if (YIndex >= Map[XIndex].Length || YIndex < 0)
                        {
                            continue;
                        }
                        if (Map[XIndex][closestTo.YIndex + dy].Unit == null)
                        {
                            return Map[closestTo.XIndex + dx][closestTo.YIndex + dy];
                        }
                    }
                }
                d++;
                //TODO: Cutoff
            }
        }

        
    }
}
   

