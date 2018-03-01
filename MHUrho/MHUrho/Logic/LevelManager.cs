using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Packaging;
using Urho;
using MHUrho.Storage;

namespace MHUrho.Logic
{

    public class LevelManager
    {
        public float GameSpeed { get; set; } = 1f;

        readonly List<Unit> units;

        readonly IPathFindAlg pathFind;

        public Map Map { get; private set; }

        //TODO: Probably not public
        public Player[] Players;

        public PackageManager PackageManager { get; private set; }

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


        public static LevelManager Load(StLevel storedLevel, PackageManager packageManager) {

            //Load data
            Map map = Map.Load(storedLevel.Map);

            LevelManager level = new LevelManager(packageManager, map);

            packageManager.LoadPackages(storedLevel.Packages);

            foreach (var unit in storedLevel.Units) {
                level.units.Add(Unit.Load(level, unit));
            }

            foreach (var player in storedLevel.Players) {
                //TODO: Load players
            }

            //Connect references
            Map.ConnectReferences();

            //Build geometry and other things

            return level;
        }

        public StLevel Save() {
            StLevel level = new StLevel() {
                GameSpeed = this.GameSpeed,
                Map = this.Map.Save()
            };

            var stUnits = level.Units;
            foreach (var unit in units) {
                stUnits.Add(unit.Save());
            }

            var stPlayers = level.Players;
            foreach (var player in Players) {
                stPlayers.Add(player.Save());
            }

            return level;
        }

        

        protected LevelManager(PackageManager packageManager, Map map)
        {
            units = new List<Unit>();
            this.PackageManager = packageManager;
            this.Map = map;
            this.pathFind = new AStar(map);
        }
        
    }
}
   

