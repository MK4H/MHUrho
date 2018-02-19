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
using MHUrho.Logic;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NUnit.Tests")]

namespace MHUrho
{
    public class MyGame : Application
    {
        public static ConfigManager Config;

        [Preserve]
        public MyGame(ApplicationOptions opts) : base(opts) { }
        

        static MyGame()
        {
            UnhandledException += (s, e) =>
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                e.Handled = true;
            };
        }

        protected override void Start()
        {
            CreateScene();

            // Subscribe to Esc key:
            Input.SubscribeToKeyDown(args => { if (args.Key == Key.Esc) Exit(); });
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

            var assetManager = new AssetManager(ResourceCache, Config);


            // 3D scene with Octree
            var scene = new Scene(Context);
            scene.CreateComponent<Octree>();

            Node mapNode = scene.CreateChild("Map");
            mapNode.Position = new Vector3(0, 5, 0);
            //mapNode.SetScale(1000f);
            mapNode.Rotation = new Quaternion(0, 0, 0);

            Map map = Map.CreateDefaultMap(10, 10, Context);
            StaticModel model = mapNode.CreateComponent<StaticModel>();
            model.Model = map.Model;

            AStar pathfind = new AStar(map);

            // Box	
            Node boxNode = scene.CreateChild(name: "Box node");
            boxNode.Position = new Vector3(x: 0, y: 1, z: -3);
            boxNode.SetScale(0f);
            boxNode.Rotation = new Quaternion(x: 60, y: 0, z: 30);

            StaticModel boxModel = boxNode.CreateComponent<StaticModel>();
            boxModel.Model = ResourceCache.GetModel("Models/Box.mdl");
            boxModel.SetMaterial(ResourceCache.GetMaterial("Materials/BoxMaterial.xml"));
            boxModel.CastShadows = true;

            //Plane
            Node planeNode = scene.CreateChild(name: "Plane node");
            planeNode.Position = new Vector3(3, 1, 0);

            StaticModel planeModel = planeNode.CreateComponent<StaticModel>();
            planeModel.Model = CoreAssets.Models.Plane;
            planeModel.Material = CoreAssets.Materials.DefaultGrey;
            planeModel.CastShadows = true;

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
            Node cameraNode = scene.CreateChild(name: "camera");
            cameraNode.Position = new Vector3(0, 10, 0);
            cameraNode.LookAt(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            Camera camera = cameraNode.CreateComponent<Camera>();

            // Viewport
            var viewport = new Viewport(Context, scene, camera, null);
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
