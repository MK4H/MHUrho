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
using System.Threading;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NUnit.Tests")]

namespace MHUrho
{
	public class MyGame : Application
	{
		public static FileManager Config;

		[Preserve]
		public MyGame(ApplicationOptions opts) : base(opts) { }

		public IMenuController menuController;

		static int mainThreadID;

		MonoDebugHud monoDebugHud;

		static MyGame()
		{
			UnhandledException += (s, e) => {
				if (Debugger.IsAttached)
					Debugger.Break();
				e.Handled = true;
			};
		}

		public static bool IsMainThread(Thread thread)
		{
			//TODO: Better
			return thread.ManagedThreadId == mainThreadID;
		}

		/// <summary>
		/// Invokes <paramref name="action"/> in main thread, does not deadlock even when called from the main thread
		/// </summary>
		/// <param name="action"></param>
		public static void InvokeOnMainSafe(Action action)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				action();
			}
			else {
				InvokeOnMainAsync(action).Wait();
			}
		}

		public static T InvokeOnMainSafe<T>(Func<T> function)
		{
			T value = default(T);
			InvokeOnMainSafe(() => { return value = function(); });
			return value;
		}

		public static async Task InvokeOnMainSafeAsync(Action action)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				action();
			}
			else {
				await InvokeOnMainAsync(action);
			}
		}

		public static async Task<T> InvokeOnMainSafeAsync<T>(Func<T> function)
		{
			T value = default(T);
			await InvokeOnMainAsync(() => { value = function(); });
			return value;
		}

		protected override void Start() {
			mainThreadID = Thread.CurrentThread.ManagedThreadId;

			Log.Open(Config.LogPath);

			Log.LogLevel = Debugger.IsAttached ? LogLevel.Debug : LogLevel.Info;

			PackageManager.CreateInstance(ResourceCache);

			

			if (Platform == Platforms.Android ||
				Platform == Platforms.iOS) {
				menuController = new MenuTouchController(this);
			}
			else {
				menuController = new MenuMandKController(this);
			}
			

			monoDebugHud = new MonoDebugHud(this);
			monoDebugHud.Show();

			//var monitor = Graphics.CurrentMonitor;
			//var resolution = Graphics.GetDesktopResolution(monitor);
			//Graphics.SetMode(resolution.X, resolution.Y);
			//Graphics.ToggleFullscreen();
			
		}

		//async void CreateScene() {

		//    // UI text 
		//    var helloText = new Text(Context);
		//    helloText.Value = "Hello World from UrhoSharp";
		//    helloText.HorizontalAlignment = HorizontalAlignment.Center;
		//    helloText.VerticalAlignment = VerticalAlignment.Top;
		//    helloText.SetColor(new Color(r: 0f, g: 1f, b: 1f));
		//    helloText.SetFont(font: ResourceCache.GetFont("Fonts/Font.ttf"), size: 30);
		//    UI.Root.AddChild(helloText);

		//    // 3D scene with Octree
		//    var scene = new Scene(Context);
		//    scene.CreateComponent<Octree>();

		//    var levelNode = scene.CreateChild("Level Node");
		//    var defaultLevel = LevelManager.LoadDefaultLevel(scene, new IntVector2(100, 100), new List<string>());

		//    // Box	
		//    Node boxNode = scene.CreateChild(name: "Box node");
		//    boxNode.Position = new Vector3(x: 0, y: 0, z: 5);
		//    boxNode.SetScale(0f);
		//    boxNode.Rotation = new Quaternion(x: 60, y: 0, z: 30);

		//    StaticModel boxModel = boxNode.CreateComponent<StaticModel>();
		//    boxModel.Model = ResourceCache.GetModel("Models/Box.mdl");
		//    boxModel.SetMaterial(ResourceCache.GetMaterial("Materials/BoxMaterial.xml"));
		//    boxModel.CastShadows = true;

		//    // Light
		//    Node lightNode = scene.CreateChild(name: "light");
		//    //lightNode.Position = new Vector3(0, 5, 0);
		//    lightNode.Rotation = new Quaternion(45, 0, 0);
		//    var light = lightNode.CreateComponent<Light>();
		//    light.LightType = LightType.Directional;
		//    //light.Range = 10;
		//    light.Brightness = 1f;
		//    light.CastShadows = true;
		//    light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
		//    light.ShadowCascade = new CascadeParameters(20.0f, 0f, 0f, 0.0f, 0.8f);

		//    // Camera

		//    cameraController = CameraController.GetCameraController(scene);


		//    touchControler = new GameTouchController(cameraController, Input);
		//    mouseController = new GameMandKController(this, Input, UI, Context, ResourceCache);

		//    // Viewport
		//    var viewport = new Viewport(Context, scene, cameraController.Camera, null);
		//    viewport.SetClearColor(Color.White);
		//    Renderer.SetViewport(0, viewport );

		//    // Do actions
		//    //cameraNode.RunActionsAsync(new RepeatForever(new RotateAroundBy(5, new Vector3(0, 0, 0), 45, 0, 45)));
		//    await boxNode.RunActionsAsync(new EaseBounceOut(new ScaleTo(duration: 1f, scale: 1)));
		//    await boxNode.RunActionsAsync(new RepeatForever(
		//        new RotateBy(duration: 1, deltaAngleX: 90, deltaAngleY: 0, deltaAngleZ: 0)));
			
		//}

	//    public void StartDefaultLevel() {
	//        // 3D scene with Octree
	//        var scene = new Scene(Context);
	//        scene.CreateComponent<Octree>();

	//        var defaultLevel = LevelManager.LoadDefaultLevel(scene, new IntVector2(100, 100), new List<string>());

	//        // Box	
	//        Node boxNode = scene.CreateChild(name: "Box node");
	//        boxNode.Position = new Vector3(x: 0, y: 0, z: 5);
	//        boxNode.SetScale(0f);
	//        boxNode.Rotation = new Quaternion(x: 60, y: 0, z: 30);

	//        StaticModel boxModel = boxNode.CreateComponent<StaticModel>();
	//        boxModel.Model = ResourceCache.GetModel("Models/Box.mdl");
	//        boxModel.SetMaterial(ResourceCache.GetMaterial("Materials/BoxMaterial.xml"));
	//        boxModel.CastShadows = true;

	//        // Light
	//        Node lightNode = scene.CreateChild(name: "light");
	//        //lightNode.Position = new Vector3(0, 5, 0);
	//        lightNode.Rotation = new Quaternion(45, 0, 0);
	//        var light = lightNode.CreateComponent<Light>();
	//        light.LightType = LightType.Directional;
	//        //light.Range = 10;
	//        light.Brightness = 1f;
	//        light.CastShadows = true;
	//        light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
	//        light.ShadowCascade = new CascadeParameters(20.0f, 0f, 0f, 0.0f, 0.8f);

	//        // Camera

	//        cameraController = CameraController.GetCameraController(scene);
	//        mouseController.ConnectCamera(cameraController);

	//        //touchControler = new TouchControler(cameraController, Input);
			

	//        // Viewport
	//        var viewport = new Viewport(Context, scene, cameraController.Camera, null);
	//        viewport.SetClearColor(Color.White);
	//        Renderer.SetViewport(0, viewport);

	//        // Do actions
	//        //cameraNode.RunActionsAsync(new RepeatForever(new RotateAroundBy(5, new Vector3(0, 0, 0), 45, 0, 45)));
	//        boxNode.RunActionsAsync(new EaseBounceOut(new ScaleTo(duration: 1f, scale: 1)));
	//        boxNode.RunActionsAsync(new RepeatForever(
	//            new RotateBy(duration: 1, deltaAngleX: 90, deltaAngleY: 0, deltaAngleZ: 0)));

	//    }

	//    public void EndCurrentLevel() {
	//        LevelManager.CurrentLevel.End();
			
	//    }

	}
}
