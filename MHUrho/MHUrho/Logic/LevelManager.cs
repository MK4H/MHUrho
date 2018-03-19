using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
using MHUrho.Input;
using MHUrho.Packaging;
using Urho;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho.Actions;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{

    public class LevelManager
    {
        /// <summary>
        /// Currently running level, CANNOT BE USED DURING LOADING
        /// </summary>
        public static LevelManager CurrentLevel { get; private set; }

        public float GameSpeed { get; set; } = 1f;

        public Map Map { get; private set; }

        public Scene Scene { get; private set; }

        public DefaultComponentFactory DefaultComponentFactory { get; private set; }

        private CameraController cameraController;
        private IGameController inputController;

        

        private readonly Dictionary<int,Unit> units;
        private readonly Dictionary<int, Player> players;

        private readonly Random rng;

        public static LevelManager Load(MyGame game, StLevel storedLevel) {

            var scene = new Scene(game.Context);
            scene.CreateComponent<Octree>();

            LoadSceneParts(game, scene);
            var cameraController = LoadCamera(game, scene);

            //Load data
            Node mapNode = scene.CreateChild("MapNode");
            var map = Map.StartLoading(mapNode, storedLevel.Map);

            LevelManager level = new LevelManager(map, scene, cameraController);

            PackageManager.Instance.LoadPackages(storedLevel.Packages);

            foreach (var unit in storedLevel.Units) {
                //TODO: Group units under one node
                var loadedUnit = Unit.Load(level, PackageManager.Instance, scene.CreateChild("UnitNode"), unit);
                level.units.Add(loadedUnit.ID, loadedUnit);
            }

            //TODO: Remove this
            Player firstPlayer = null;

            foreach (var player in storedLevel.Players) {
                var loadedPlayer = Player.Load(player);
                //TODO: If player needs controller, give him
                if (firstPlayer == null) {
                    firstPlayer = loadedPlayer;
                }

                level.players.Add(loadedPlayer.ID, loadedPlayer);
            }
            //TODO: Move this inside the foreach
            level.inputController = game.menuController.GetGameController(cameraController, level, firstPlayer);
            
            //Connect references
            map.ConnectReferences(level);

            foreach (var unit in level.units.Values) {
                unit.ConnectReferences(level);
            }

            foreach (var player in level.players.Values) {
                player.ConnectReferences(level);
            }


            //Build geometry and other things

            map.FinishLoading();

            foreach (var unit in level.units.Values) {
                unit.FinishLoading();
            }

            CurrentLevel = level;
            return level;
        }

        public static LevelManager LoadFrom(MyGame game, Stream stream, bool leaveOpen = false) {
            var storedLevel = StLevel.Parser.ParseFrom(stream);
            var level = Load(game, storedLevel);
            if (!leaveOpen) {
                stream.Close();
            }
            return level;
        }

        /// <summary>
        /// Loads default level to use in level builder as basis, loads specified packages plus default package
        /// </summary>
        /// <param name="mapSize">Size of the map to create</param>
        /// <param name="packages">packages to load</param>
        /// <returns>Loaded default level</returns>
        public static LevelManager LoadDefaultLevel(MyGame game, IntVector2 mapSize, IEnumerable<string> packages) {
            PackageManager.Instance.LoadWholePackages(packages);

            var scene = new Scene(game.Context);
            scene.CreateComponent<Octree>();

            LoadSceneParts(game, scene);
            var cameraController = LoadCamera(game, scene);


            Node mapNode = scene.CreateChild("MapNode");

            Map map = Map.CreateDefaultMap(mapNode, mapSize);

            CurrentLevel = new LevelManager(map, scene, cameraController);


            //TODO: Temporary player
            var player = new Player(CurrentLevel.GetNewID(CurrentLevel.players));
            CurrentLevel.players.Add(player.ID, player);
            CurrentLevel.inputController = game.menuController.GetGameController(cameraController, CurrentLevel, player);

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
                stUnits.Add(unit.Value.Save());
            }

            var stPlayers = level.Players;
            foreach (var player in players.Values) {
                stPlayers.Add(player.Save());
            }

            return level;
        }

        public void SaveTo(Stream stream, bool leaveOpen = false) {
            var storedLevel = Save();
            storedLevel.WriteTo(new Google.Protobuf.CodedOutputStream(stream, leaveOpen));
        }

        public void End() {
            inputController.Dispose();
            inputController = null;
            Map.Dispose();
            Scene.RemoveAllChildren();
            Scene.Dispose();
            CurrentLevel = null;
        }

        protected LevelManager(Map map, 
                               Scene scene, 
                               CameraController cameraController)
        {
            this.Scene = scene;
            this.units = new Dictionary<int, Unit>();
            this.players = new Dictionary<int, Player>();
            this.rng = new Random();
            this.Map = map;

            this.cameraController = cameraController;
            this.DefaultComponentFactory = new DefaultComponentFactory();

        }

        /// <summary>
        /// Spawns new unit of given <paramref name="unitType"/> into the world map at <paramref name="tile"/>
        /// </summary>
        /// <param name="unitType">The unit to be added</param>
        /// <param name="tile">Tile to spawn the unit at</param>
        /// <param name="player">owner of the new unit</param>
        public void SpawnUnit(UnitType unitType, ITile tile, IPlayer player) {
            Node unitNode = Scene.CreateChild("Unit");

            var newUnit = unitType.CreateNewUnit(GetNewID(units),unitNode, this, tile, player);
            units.Add(newUnit.ID,newUnit);
            player.AddUnit(newUnit);

            if (!tile.TryAddOwningUnit(newUnit)) {
                var targetTile = Map.FindClosestEmptyTile(tile);
                if (targetTile == null) {
                    //TODO: There is no closest empty tile, everything is full
                }

                newUnit.Order(targetTile);
            }
        }

        public Unit GetUnit(int ID) {
            if (!units.TryGetValue(ID, out Unit value)) {
                throw new ArgumentOutOfRangeException("Unit with this ID does not exist in the current level");
            }
            return value;
        }

        public Player GetPlayer(int ID) {
            if (!players.TryGetValue(ID, out Player player)) {
                throw new ArgumentOutOfRangeException("Player with this ID does not exist in the current level");
            }

            return player;
        }

        private static async void LoadSceneParts(MyGame game, Scene scene) {
            // Box	
            Node boxNode = scene.CreateChild(name: "Box node");
            boxNode.Position = new Vector3(x: 0, y: 0, z: 5);
            boxNode.SetScale(0f);
            boxNode.Rotation = new Quaternion(x: 60, y: 0, z: 30);

            StaticModel boxModel = boxNode.CreateComponent<StaticModel>();
            boxModel.Model = game.ResourceCache.GetModel("Models/Box.mdl");
            boxModel.SetMaterial(game.ResourceCache.GetMaterial("Materials/BoxMaterial.xml"));
            boxModel.CastShadows = true;

            // Light
            Node lightNode = scene.CreateChild(name: "light");
            //lightNode.Position = new Vector3(0, 5, 0);
            lightNode.Rotation = new Quaternion(45, 0, 0);
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;
            //light.Range = 10;
            light.Brightness = 0.5f;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            light.ShadowCascade = new CascadeParameters(20.0f, 0f, 0f, 0.0f, 0.8f);

            // Ambient light
            var zoneNode = scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();

            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = new Color(0.5f, 0.5f, 0.5f);
            zone.FogColor = new Color(0.1f, 0.2f, 0.3f);
            zone.FogStart = 10;
            zone.FogEnd = 100;

            //TODO: Remove this
            await boxNode.RunActionsAsync(new EaseBounceOut(new ScaleTo(duration: 1f, scale: 1)));
            await boxNode.RunActionsAsync(new RepeatForever(
                new RotateBy(duration: 1, deltaAngleX: 90, deltaAngleY: 0, deltaAngleZ: 0)));

        }

        private static CameraController LoadCamera(MyGame game, Scene scene) {
            // Camera

            CameraController cameraController = CameraController.GetCameraController(scene);

            // Viewport
            var viewport = new Viewport(game.Context, scene, cameraController.Camera, null);
            viewport.SetClearColor(Color.White);
            game.Renderer.SetViewport(0, viewport);

            return cameraController;
        }

        private const int MaxTries = 10000000;
        private int GetNewID<T>(IDictionary<int, T> dictionary) {
            int id, i = 0;
            while (dictionary.ContainsKey(id = rng.Next())) {
                i++;
                if (i > MaxTries) {
                    //TODO: Exception
                    throw new Exception("Could not find free ID");
                }
            }

            return id;
        }

    }
}
   

