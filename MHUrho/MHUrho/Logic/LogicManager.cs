using System;
using System.Collections.Generic;
using System.Text;


using Urho;
using ProtoBuf;

namespace MHUrho.Logic
{

    public class LogicManager
    {

        public float GameSpeed { get; set; } = 1f;

        [ProtoMember(1)]
        List<Unit> units;

        IPathFindAlg pathFind;

        [ProtoMember(2)]
        public Map Map { get; private set; }


        //TODO: Probably not public
        [ProtoMember(3)]
        public Player[] Players;

        /// <summary>
        /// Registers unit after it is spawned by a Tile or a Building
        /// </summary>
        /// <param name="unit">The unit to be registered</param>
        public void RegisterUnit(Unit unit)
        {
            units.Add(unit);
        }

        public Path GetPath(Unit unit, ITile target)
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

        public ITile TryMoveUnitThroughTileAt(Unit unit, IntVector2 tileIndex)
        {
            ITile TargetTile = Map.GetTile(tileIndex);
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

        [ProtoAfterDeserialization]
        public void AfterDeserialization() {
            pathFind = new AStar(Map);
        }
        
    }
}
   

