using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
using MHUrho.Packaging;
using Urho;
using MHUrho.Storage;
using Urho.Actions;

namespace MHUrho.Logic
{

    public class LevelManager
    {
        public static LevelManager CurrentLevel { get; private set; }

        public float GameSpeed { get; set; } = 1f;

        public Map Map { get; private set; }

        public Scene Scene { get; private set; }

        //TODO: Probably not public
        public Player[] Players;

        
        private CameraController cameraController;
        private IGameController inputController;

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


        public static LevelManager Load(MyGame game, StLevel storedLevel) {

            var scene = new Scene(game.Context);
            scene.CreateComponent<Octree>();

            LoadSceneParts(game, scene);
            var cameraController = LoadCamera(game, scene);

            //Load data
            Node mapNode = scene.CreateChild("MapNode");
            Map map = Map.StartLoading(mapNode, storedLevel.Map);

            LevelManager level = new LevelManager(game, map, scene, cameraController);

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
        /// <param name="levelNode">Scene node of the level</param>
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

            CurrentLevel = new LevelManager(game, map, scene, cameraController);
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
            inputController.Dispose();
            inputController = null;
            Map.Dispose();
            Scene.RemoveAllChildren();
            Scene.Dispose();
            CurrentLevel = null;
        }

        public void HandleRaycast(RayQueryResult rayQueryResult) {
            //TODO: Switch on current user action, like building, selecting units etc.
            var clickedTile = Map.Clicked(rayQueryResult);
            if (clickedTile != null && inputController.SelectedTileType != null) {
                Map.ChangeTileType(clickedTile, inputController.SelectedTileType);
            }
        }

        public void HandleRaycast(List<RayQueryResult> rayQueryResults) {

        }

        protected LevelManager(MyGame game,
                               Map map, 
                               Scene scene, 
                               CameraController cameraController)
        {
            this.Scene = scene;
            units = new List<Unit>();
            this.Map = map;
            this.pathFind = new AStar(map);
            this.Players = new Player[0];
            this.cameraController = cameraController;
            this.inputController = game.menuController.GetGameController(cameraController, this);
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
            light.Brightness = 1f;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            light.ShadowCascade = new CascadeParameters(20.0f, 0f, 0f, 0.0f, 0.8f);

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



    }
}
   

