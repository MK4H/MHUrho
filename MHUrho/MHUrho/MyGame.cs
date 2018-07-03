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

		protected override void Start() {
			mainThreadID = Thread.CurrentThread.ManagedThreadId;

			Log.Open(Files.LogPath);
			Log.LogLevel = Debugger.IsAttached ? LogLevel.Debug : LogLevel.Info;

			Stream configFile = Files.OpenDynamicFile(Files.ConfigFilePath, System.IO.FileMode.Open, FileAccess.Read);

			Config = AppOptions.LoadFrom(configFile);
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

		

	}
}
