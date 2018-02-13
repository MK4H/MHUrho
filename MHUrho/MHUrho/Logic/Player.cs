﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace MHUrho
{
    public class Player
    {
        class LineOrder
        {
            Point MovePoint, End1, End2, Delta;
            bool Finished,OneEndHit;
            Player Player;
            public bool OrderNext(ref int toOrder)
            {
                if (Finished)
                {
                    return false;
                }

                while (!Finished)
                {
                    if (MovePoint == End1 || MovePoint == End2)
                    {
                        if (OneEndHit || (MovePoint == End1 && MovePoint == End2))
                        {
                            Finished = true;
                        }
                        else
                        {
                            OneEndHit = true;
                        }
                    }

                    if (!Player.Level.IsInsideMap(MovePoint))
                    {
                        PointHelper.Add(MovePoint, Delta, out MovePoint);
                        Delta.X = -(Delta.X + 1);
                        Delta.Y = -(Delta.Y + 1);
                        continue;
                    }

                    if (Player.Selected[toOrder].Order(Player.Level.TileAt(MovePoint)))
                    {
                        toOrder++;
                    }

                    
                    PointHelper.Add(MovePoint,Delta,out MovePoint);
                    if (Delta.X != 0)
                    {
                        Delta.X = -(Delta.X + 1);
                    }
                    else if (Delta.Y != 0)
                    {
                        Delta.Y = -(Delta.Y + 1);
                    }
                    
                    return true;
                }
                return false;
            }

            public LineOrder(Point end1, Point end2, Player player)
            {
                End1 = end1;
                End2 = end2;
                Player = player;
                if (end1.X == end2.X)
                {
                    Delta = new Point(0, 1);
                }
                else if (end1.Y == end2.Y)
                {
                    Delta = new Point(1, 0);
                }
                else
                {
                    throw new ArgumentException("Directions other than horizontal and vertical not supported");
                }
                // Center of the line
                MovePoint = new Point(
                        Math.Min(end2.X, end1.X) + Math.Abs(end2.X - end1.X) / 2,
                        Math.Min(end2.Y, end1.Y) + Math.Abs(end2.Y - end1.Y) / 2);
            }

            public override string ToString()
            {
                return string.Format("End1 = \"{0}\", End2 = \"{1}\"", End1, End2);
            }
        }

        class SpawnOrder
        {

        }



        public readonly LogicManager Level;

        List<Player> Friends;

        List<Unit> Units;

        SelectedType TypeOfSelected;
        List<ISelectable> Selected;

        /// <summary>
        /// Processes a player click on a unit
        /// </summary>
        /// <param name="unit">The unit that was clicked</param>
        public void ClickUnit(Unit unit)
        {
            // My unit
            if (unit.Player == this)
            {
                MyUnitClick(unit);
            }
            // Friendly unit
            else if (Friends.Contains(unit.Player))
            {
                FriednlyUnitClick(unit);
            }
            // Enemy unit
            else
            {
                EnemyUnitClick(unit);
            }
        }


        private void MyUnitClick(Unit unit)
        {
            if (TypeOfSelected == SelectedType.Unit)
            {
                if (unit.Select())
                {
                    Selected.Add(unit);
                    TypeOfSelected = SelectedType.Unit;
                }
                // Clicked on already selected unit
                else
                {
                    Selected.Remove(unit);
                    // Deselected last unit
                    if (Selected.Count == 0)
                    {
                        TypeOfSelected = SelectedType.None;
                    }
                    unit.Deselect();
                }
            }
            else
            {
                //TODO: Deselect all currently selected
                ClearSelected();
                unit.Select();
                Selected.Add(unit);
                TypeOfSelected = SelectedType.Unit;
            }
        }

        private void FriednlyUnitClick(Unit unit)
        {

        }

        private void EnemyUnitClick(Unit unit)
        {
            if (TypeOfSelected == SelectedType.Unit)
            {
                foreach (var item in Selected)
                {
                    item.Order(unit);
                }
            }
        }

        /// <summary>
        /// Processes user click on a tile
        /// </summary>
        /// <param name="tile">The tile clicked</param>
        public void ClickTile(Tile tile)
        {
            if (TypeOfSelected == SelectedType.None)
            {
                //TODO:TEMP
                tile.SpawnUnit(this);
            }
            else if (TypeOfSelected == SelectedType.Unit)
            {
                //TODO: return value
                OrderUnits(tile);
            }
        }


        private bool OrderUnits(Tile tile)
        {
            int ToOrder = 0;

            // If i cant order to the clicked tile, imposible order
            if (!Selected[ToOrder++].Order(tile))
            {
                return false;
            }

            Point TopLeft = tile.Location;
            Point BottomRight = tile.Location;
            Point MoveBy = new Point(1, 1);
            List <LineOrder> LineOrders = new List<LineOrder>(4);
            while (ToOrder < Selected.Count)
            {
                // New Rectangle
                PointHelper.Sub(TopLeft,MoveBy,out TopLeft);
                PointHelper.Add(BottomRight,MoveBy, out BottomRight);
                // Top
                LineOrders.Add(new LineOrder(TopLeft, new Point(BottomRight.X,TopLeft.Y),this));
                // Right without the two corners 
                LineOrders.Add(new LineOrder(new Point(BottomRight.X, TopLeft.Y + 1), new Point(BottomRight.X,BottomRight.Y - 1), this));
                // Bottom
                LineOrders.Add(new LineOrder(BottomRight, new Point(TopLeft.X,BottomRight.Y), this));
                // Left without the two corners
                LineOrders.Add(new LineOrder(new Point(TopLeft.X, BottomRight.Y - 1), new Point(TopLeft.X,TopLeft.Y + 1), this));

                // Spawn all the lines
                int i = 0;
                while (ToOrder < Selected.Count)
                {
                    if (!LineOrders[i].OrderNext(ref ToOrder))
                    {
                        LineOrders.RemoveAt(i);
                        if (LineOrders.Count == 0)
                        {
                            break;
                        }
                    }
                    i = (i + 1) % LineOrders.Count; 
                }
            }
            return true;

        }


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
            while (toOrder < Selected.Count)
            {
                TargetPoint.X = TargetPoint.X + pdx - mdx;
                TargetPoint.Y = TargetPoint.Y + pdy - mdy;

                if (Selected[toOrder].Order(Level.TileAt(TargetPoint)))
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


        /// <summary>
        /// Clears list of currently selected 
        /// </summary>
        public void ClearSelected()
        {
            foreach (var item in Selected)
            {
                item.Deselect();
            }
            Selected = new List<ISelectable>();
        }


        public Player(LogicManager level)
        {
            this.Level = level;
            Units = new List<Unit>();
            Selected = new List<ISelectable>();
            TypeOfSelected = SelectedType.None;
        }
    }
}