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

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NUnit.Tests")]

namespace MHUrho
{
	public class MyGame : Application
	{
		public static FileManager Files { get; set; }

		public AppOptions Config { get; private set; }

		[Preserve]
		public MyGame(ApplicationOptions opts) : base(opts) { }

		public IMenuController menuController;

		public ControllerFactory ControllerFactory { get; private set; }

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
			Graphics.WindowTitle = "MHUrho";

			mainThreadID = Thread.CurrentThread.ManagedThreadId;

			Log.Open(Files.LogPath);
			Log.LogLevel = Debugger.IsAttached ? LogLevel.Debug : LogLevel.Info;

			//TODO: DEBUG
			//Stream newConfigFile = Files.OpenDynamicFile(Files.ConfigFilePath, System.IO.FileMode.Create, FileAccess.Write);
			//AppOptions.GetDefaultAppOptions().SaveTo(newConfigFile);

			if (!Files.FileExists(Path.Combine(Files.DynamicDirPath,Files.ConfigFilePath))) {
				Files.CopyStaticToDynamic(Files.ConfigFilePath);
			}

			//TODO: Copy from static if not present
			Stream configFile = Files.OpenDynamicFile(Files.ConfigFilePath, System.IO.FileMode.Open, FileAccess.Read);
			Config = AppOptions.LoadFrom(configFile);
			
			PackageManager.CreateInstance(ResourceCache);

			SetConfigOptions();

			if (Platform == Platforms.Android ||
				Platform == Platforms.iOS) {
				ControllerFactory = new TouchFactory(this);
			}
			else {
				ControllerFactory = new MandKFactory(this);
			}

			menuController = ControllerFactory.CreateMenuController();

		}

		void SetConfigOptions()
		{
			var monitor = Graphics.CurrentMonitor;

			if (Config.DebugHUD) {
				monoDebugHud = new MonoDebugHud(this);
				monoDebugHud.Show();
			}

			Config.SetGraphicsMode(Graphics);

		}

	}
}
