using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Urho;
using Urho.Gui;
using Urho.Actions;
using Urho.Shapes;
using Urho.IO;
using System.IO;
using System.Reflection;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Packaging;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NUnit.Tests")]

namespace MHUrho
{
    public class MyGame : Application
    {
        public static ConfigManager Config;

        [Preserve]
        public MyGame(ApplicationOptions opts) : base(opts) { }

        private CameraController cameraController;

        private TouchControler touchControler;
        private MouseController mouseController;

        static MyGame()
        {
            UnhandledException += (s, e) =>
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                e.Handled = true;
            };
        }

        protected override void Start() {
            Log.Open(Config.LogPath);

            Log.LogLevel = Debugger.IsAttached ? LogLevel.Debug : LogLevel.Info;

            PackageManager.ResourceCache = ResourceCache;

            CreateScene();

        }

        async void CreateScene() {

            // UI text 
            var helloText = new Text(Context);
            helloText.Value = "Hello World from UrhoSharp";
            helloText.HorizontalAlignment = HorizontalAlignment.Center;
            helloText.VerticalAlignment = VerticalAlignment.Top;
            helloText.SetColor(new Color(r: 0f, g: 1f, b: 1f));
            helloText.SetFont(font: ResourceCache.GetFont("Fonts/Font.ttf"), size: 30);
            UI.Root.AddChild(helloText);





            var assetManager = new PackageManager();

            

            // 3D scene with Octree
            var scene = new Scene(Context);
            scene.CreateComponent<Octree>();

            Node mapNode = scene.CreateChild("Map");
            mapNode.Position = new Vector3(-5 , 0, -5);
            //mapNode.SetScale(1000f);
            mapNode.Rotation = new Quaternion(0, 0, 0);

            Map map = Map.CreateDefaultMap(10, 10, Context);
            StaticModel model = mapNode.CreateComponent<StaticModel>();
            model.Model = map.Model;
            model.SetMaterial(map.Material);

            AStar pathfind = new AStar(map);

            // Box	
            Node boxNode = scene.CreateChild(name: "Box node");
            boxNode.Position = new Vector3(x: 0, y: 0, z: 5);
            boxNode.SetScale(0f);
            boxNode.Rotation = new Quaternion(x: 60, y: 0, z: 30);

            StaticModel boxModel = boxNode.CreateComponent<StaticModel>();
            boxModel.Model = ResourceCache.GetModel("Models/Box.mdl");
            boxModel.SetMaterial(ResourceCache.GetMaterial("Materials/BoxMaterial.xml"));
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

            // Camera

            //TODO: Rebase all operations on camera to cameraHolder, set constant offset of camera from holder


            cameraController = CameraController.GetCameraController(scene);


            touchControler = new TouchControler(cameraController, Input);
            mouseController = new MouseController(cameraController, Input, UI, Context, ResourceCache);

            // Viewport
            var viewport = new Viewport(Context, scene, cameraController.Camera, null);
            viewport.SetClearColor(Color.White);
            Renderer.SetViewport(0, viewport );

            // Do actions
            //cameraNode.RunActionsAsync(new RepeatForever(new RotateAroundBy(5, new Vector3(0, 0, 0), 45, 0, 45)));
            await boxNode.RunActionsAsync(new EaseBounceOut(new ScaleTo(duration: 1f, scale: 1)));
            await boxNode.RunActionsAsync(new RepeatForever(
                new RotateBy(duration: 1, deltaAngleX: 90, deltaAngleY: 0, deltaAngleZ: 0)));
            
        }
    }
}
