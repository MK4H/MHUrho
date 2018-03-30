using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;
using MHUrho.Logic;
using MHUrho.WorldMap;


namespace MHUrho.Logic
{
    public class Player : IPlayer {

        public int ID { get; private set; }

        private readonly List<IPlayer> friends;

        //TODO: Split units and buildings by types
        private readonly List<Unit> units;

        private readonly List<Building> buildings;

        private StPlayer storedPlayer;

        public Player(int ID) {
            this.ID = ID;
            units = new List<Unit>();
            friends = new List<IPlayer>();
        }

        protected Player(StPlayer storedPlayer) 
            : this(storedPlayer.PlayerID) {
            this.storedPlayer = storedPlayer;
        }

        public static Player Load(StPlayer storedPlayer) {
            var newPlayer = new Player(storedPlayer);
            return newPlayer;
        }

        public StPlayer Save() {
            var storedPlayer = new StPlayer();

            storedPlayer.PlayerID = ID;

            var stUnitIDs = storedPlayer.UnitIDs;
            foreach (var unit in units) {
                stUnitIDs.Add(unit.ID);
            }

            //TODO: Buildings

            var stFriendIDs = storedPlayer.FriendPlayerIDs;
            foreach (var friend in friends) {
                stFriendIDs.Add(friend.ID);
            }

            return storedPlayer;
        }

        public void ConnectReferences(LevelManager level) {
            foreach (var unitID in storedPlayer.UnitIDs) {
                units.Add(level.GetUnit(unitID));
            }
        }

        public void FinishLoading() {
            storedPlayer = null;
        }

        /// <summary>
        /// Adds unit to players units
        /// </summary>
        /// <param name="unit">unit to add</param>
        public void AddUnit(Unit unit) {
            units.Add(unit);
        }

        public void AddBuilding(Building building) {
            buildings.Add(building);
        }

        public void RemoveUnit(Unit unit) {
            units.Remove(unit);
        }


        //private bool OrderUnits(ITile tile)
        //{
        //    int ToOrder = 0;

        //    // If i cant order to the clicked tile, imposible order
        //    if (!selected[ToOrder++].Order(tile))
        //    {
        //        return false;
        //    }

        //    IntVector2 TopLeft = tile.Location;
        //    IntVector2 BottomRight = tile.Location;
        //    IntVector2 MoveBy = new IntVector2(1, 1);
        //    List <LineOrder> LineOrders = new List<LineOrder>(4);
        //    while (ToOrder < selected.Count)
        //    {
        //        // New Rectangle
        //        IntVector2.Subtract(ref TopLeft,ref MoveBy,out TopLeft);
        //        IntVector2.Add(ref BottomRight,ref MoveBy, out BottomRight);
        //        // Top
        //        LineOrders.Add(new LineOrder(TopLeft, new IntVector2(BottomRight.X,TopLeft.Y),this));
        //        // Right without the two corners 
        //        LineOrders.Add(new LineOrder(new IntVector2(BottomRight.X, TopLeft.Y + 1), new IntVector2(BottomRight.X,BottomRight.Y - 1), this));
        //        // Bottom
        //        LineOrders.Add(new LineOrder(BottomRight, new IntVector2(TopLeft.X,BottomRight.Y), this));
        //        // Left without the two corners
        //        LineOrders.Add(new LineOrder(new IntVector2(TopLeft.X, BottomRight.Y - 1), new IntVector2(TopLeft.X,TopLeft.Y + 1), this));

        //        // Spawn all the lines
        //        int i = 0;
        //        while (ToOrder < selected.Count)
        //        {
        //            if (!LineOrders[i].OrderNext(ref ToOrder))
        //            {
        //                LineOrders.RemoveAt(i);
        //                if (LineOrders.Count == 0)
        //                {
        //                    break;
        //                }
        //            }
        //            i = (i + 1) % LineOrders.Count; 
        //        }
        //    }
        //    return true;

        //}


        /*
        /// <summary>
        /// Spawns unit on the circumference of the square defined by the two points
        /// </summary>
        /// <param name="toOrder"></param>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        private void OrderSquare(ref int toOrder, Point topLeft, Point bottomRight )
        {
            Point TargetPoint = topLeft;
            // Plus delta x, minus delta x;
            int pdx = 1, mdx = 0, pdy = 0, mdy = 0; 
            while (toOrder < selected.Count)
            {
                TargetPoint.X = TargetPoint.X + pdx - mdx;
                TargetPoint.Y = TargetPoint.Y + pdy - mdy;

                if (selected[toOrder].Order(Level.TileAt(TargetPoint)))
                {
                    toOrder++;
                }

                if (TargetPoint == topLeft)
                {
                    break;
                }

                if (TargetPoint == new Point(bottomRight.X,topLeft.Y) ||
                    TargetPoint == bottomRight ||
                    TargetPoint == new Point (topLeft.X, bottomRight.Y))
                {
                    // Rotate them in order to go around the circumference
                    int tmp = pdx;
                    pdx = mdy;
                    mdy = mdx;
                    mdx = pdy;
                    pdy = tmp;
                }
            }
        }
        */

    }
}