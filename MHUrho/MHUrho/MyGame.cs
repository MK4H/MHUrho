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
using MHUrho.StartupManagement;
using MHUrho.Threading;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NUnit.Tests")]

namespace MHUrho
{
	public class MyGame : Application
	{
		public static MyGame Instance { get; private set; }

		public static FileManager Files { get; set; }

		public static StartupOptions StartupOptions { get; set; }

		public AppConfig Config { get; private set; }



		public IMenuController MenuController { get; private set; }

		public ControllerFactory ControllerFactory { get; private set; }

		static int mainThreadID;

		MonoDebugHud monoDebugHud;

		static MyGame()
		{
			UnhandledException += 
				(s, e) => {
				if (Debugger.IsAttached) {
						Debugger.Break();
					}
					e.Handled = true;
				};
		}

		[Preserve]
		public MyGame(ApplicationOptions opts)
			: base(opts)
		{

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
			InvokeOnMainSafe(() => { value = function(); });
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

		protected override void Start()
		{
			Instance = this;
			Graphics.WindowTitle = "MHUrho";
			
			mainThreadID = Thread.CurrentThread.ManagedThreadId;
			SynchronizationContext.SetSynchronizationContext(new MHUrhoSynchronizationContext());

			Log.Open(Files.LogPath);
			Log.LogLevel = Debugger.IsAttached ? LogLevel.Debug : LogLevel.Info;

			//NOTE: DEBUG
			//Stream newConfigFile = Files.OpenDynamicFile(Files.ConfigFilePath, System.IO.FileMode.Create, FileAccess.Write);
			//AppConfig.GetDefaultAppOptions().SaveTo(newConfigFile);

			if (!Files.FileExists(Path.Combine(Files.DynamicDirPath, Files.ConfigFilePath))) {
				Files.CopyStaticToDynamic(Files.ConfigFilePath);
			}

			//TODO: Copy from static if not present
			using (Stream configFile = Files.OpenDynamicFile(Files.ConfigFilePath, System.IO.FileMode.Open, FileAccess.Read)) {
				Config = AppConfig.LoadFrom(configFile);
			}
				
			PackageManager.CreateInstance(ResourceCache);

			SetConfigOptions();

			if (Platform == Platforms.Android ||
				Platform == Platforms.iOS) {
				ControllerFactory = new TouchFactory();
			}
			else {
				ControllerFactory = new MandKFactory();
			}

			MenuController = ControllerFactory.CreateMenuController();

			StartupOptions.UIActions?.RunActions(this);
		}

		void SetConfigOptions()
		{
			int monitor = Graphics.CurrentMonitor;

			if (Config.DebugHUD) {
				monoDebugHud = new MonoDebugHud(this);
				monoDebugHud.Show();
			}

			Config.SetGraphicsMode(Graphics);

		}

	}
}
