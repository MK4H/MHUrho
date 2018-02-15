﻿using System;
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

        IPathFindAlg pathFind;

        public Map Map { get; private set; }

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
                target = Map.FindClosestEmptyTile(target);
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
            Tile TargetTile = Map.GetTile(tileIndex);
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





        //TODO: From XML
        /// <summary>
        /// Loads level logic
        /// </summary>
        /// <returns></returns>
        public static LogicManager Load()
        {
            LogicManager newLevel = new LogicManager();
            //TODO: Load from XML or something

            newLevel.pathFind = new AStar(newLevel.Map);
            //TODO: Load AI players
            newLevel.Players = new Player[4];
            newLevel.Players[0] = new Player(newLevel);
            return newLevel;
        }
        public LogicManager()
        {
            units = new List<Unit>();

        }
        

        
    }
}
   

