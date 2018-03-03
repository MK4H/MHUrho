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
        public static LevelManager CurrentLevel { get; private set; }

        public float GameSpeed { get; set; } = 1f;

        public Map Map { get; private set; }

        //TODO: Probably not public
        public Player[] Players;

        private readonly Scene scene;

        private readonly Node node;

        readonly List<Unit> units;

        readonly IPathFindAlg pathFind;
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


        public static LevelManager Load(Context context, Node levelNode, StLevel storedLevel) {

            Node mapNode = levelNode.CreateChild("MapNode");
            //Load data
            Map map = Map.StartLoading(mapNode, storedLevel.Map);

            LevelManager level = new LevelManager(map, levelNode, scene);

            PackageManager.Instance.LoadPackages(storedLevel.Packages);

            foreach (var unit in storedLevel.Units) {
                level.units.Add(Unit.Load(unit));
            }

            foreach (var player in storedLevel.Players) {
                //TODO: Load players
            }

            //Connect references
            map.ConnectReferences();

            //level.units.ForEach((unit) => { unit.ConnectReferences(); });



            //Build geometry and other things

            map.FinishLoading();

            //level.units.ForEach((unit) => { unit.FinishLoading(); });

            CurrentLevel = level;
            return level;
        }

        public static LevelManager LoadFrom(Context context, Stream stream, bool leaveOpen = false) {
            var storedLevel = StLevel.Parser.ParseFrom(stream);
            var levelNode = scene.CreateChild("Level Node");
            var level = Load(scene, levelNode, storedLevel);
            if (!leaveOpen) {
                stream.Close();
            }
            return level;
        }

        /// <summary>
        /// Loads default level to use in level builder as basis, loads specified packages plus default package
        /// </summary>
        /// <param name="levelNode">Scene node of the level</param>
        /// <param name="mapSize">Size of the map to create</param>
        /// <param name="packages">packages to load</param>
        /// <returns>Loaded default level</returns>
        public static LevelManager LoadDefaultLevel(Scene scene, IntVector2 mapSize, IEnumerable<string> packages) {
            PackageManager.Instance.LoadWholePackages(packages);
            var levelNode = scene.CreateChild("Level Node");
            Node mapNode = levelNode.CreateChild("MapNode");

            Map map = Map.CreateDefaultMap(mapNode, mapSize);

            CurrentLevel = new LevelManager(map, levelNode, scene);
            return CurrentLevel;
        }

        public StLevel Save() {
            StLevel level = new StLevel() {
                GameSpeed = this.GameSpeed,
                Map = this.Map.Save(),
                Packages = PackageManager.Instance.Save()
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

        public void SaveTo(Stream stream, bool leaveOpen = false) {
            var storedLevel = Save();
            storedLevel.WriteTo(new Google.Protobuf.CodedOutputStream(stream, leaveOpen));
        }

        public void End() {
            scene.Dispose();
        }

        protected LevelManager(Map map, Node node, Scene scene)
        {
            units = new List<Unit>();
            this.Map = map;
            this.pathFind = new AStar(map);
            this.node = node;
            this.Players = new Player[0];
        }
        
    }
}
   

