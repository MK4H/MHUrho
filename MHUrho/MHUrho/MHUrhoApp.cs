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
using System.Text;
using System.Threading;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Input.Touch;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using MHUrho.Threading;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NUnit.Tests")]

namespace MHUrho
{
	public class MHUrhoApp : Application
	{
		public static MHUrhoApp Instance { get; private set; }

		/// <summary>
		/// File manager set by system specific startup code.
		/// Has to be static for Android startup.
		/// </summary>
		public static FileManager FileManager { get; set; }

		/// <summary>
		/// Startup options (command line options) set by system specific startup code.
		/// Has to be static for Android startup.
		/// </summary>
		public static StartupOptions StartupArgs { get; set; }

		public FileManager Files => FileManager;

		public StartupOptions StartupOptions => StartupArgs;

		public AppConfig Config { get; private set; }

		public PackageManager PackageManager { get; private set; }

		public IMenuController MenuController { get; private set; }

		public IControllerFactory ControllerFactory { get; private set; }

		static int mainThreadID;

		MonoDebugHud monoDebugHud;

		static MHUrhoApp()
		{
			UnhandledException += ErrorRecovery;
		}

		[Preserve]
		public MHUrhoApp(ApplicationOptions opts)
			: base(opts)
		{
		}

		public static bool IsMainThread(Thread thread)
		{
			//NOTE: When UrhoSharp implements the Urho3D method of checking main thread, delegate to it
			return thread.ManagedThreadId == mainThreadID;
		}

		/// <summary>
		/// Invokes <paramref name="action"/> in main thread, does not deadlock even when called from the main thread
		/// </summary>
		/// <param name="action"></param>
		public static void InvokeOnMainSafe(Action action)
		{
			Exception exc = null;
			if (IsMainThread(Thread.CurrentThread)) {
				try {
					action();
				}
				catch (Exception e) {
					exc = e;
				}
			}
			else {
				InvokeOnMainAsync(() => {
									try {
										action();
									}
									catch (Exception e) {
										exc = e;
									}
								}).Wait();
			}

			if (exc != null) {
				throw new MethodInvocationException($"Method invocation ended with an exception: {exc.Message}", exc);
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
			Exception exc = null;

			await InvokeOnMainAsync(() => {
										try {
											action();
										}
										catch (Exception e) {
											exc = e;
										}
									});

			if (exc != null) {
				throw new MethodInvocationException($"Method invocation ended with an exception: {exc.Message}", exc);
			}
		}

		public static async Task<T> InvokeOnMainSafeAsync<T>(Func<T> function)
		{
			T value = default(T);
			Exception exc = null;

			await InvokeOnMainSafeAsync(() => {
											try {
												value = function();
											}
											catch (Exception e) {
												exc = e;
											}								
										});
			if (exc != null) {
				throw new MethodInvocationException("Method invocation ended with an exception.", exc);
			}
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

			SetConfigOptions();

			string[] failedPackages;
			try {
				PackageManager = new PackageManager(this);
				failedPackages = PackageManager.ParseGamePackDir();
			}
			catch (FatalPackagingException) {
				//Exception is already logged at the lowest level where it was detected and then translated into the FatalPackagingException
				Stop();
				throw;
			}

			if (Platform == Platforms.Android ||
				Platform == Platforms.iOS) {
				ControllerFactory = new TouchFactory();
			}
			else {
				ControllerFactory = new MandKFactory();
			}

			MenuController = ControllerFactory.CreateMenuController(this);

			if (failedPackages.Length == 0) {
				MenuController.InitialSwitchToMainMenu();
				StartupOptions.UIActions?.RunActions(this);
			}
			else {
				StringBuilder message = new StringBuilder();
				message.AppendLine("Failed to load packages from these paths:");
				foreach (string failedPackage in failedPackages) {
					message.AppendLine(failedPackage);			
				}

				MenuController.InitialSwitchToMainMenu("LOADING ERROR", message.ToString());
			}
		}

		//public void Dispose()
		//{
		//	Urho.IO.Log.Write(LogLevel.Debug, "Game disposed.");
		//	base.Dispose();
		//}

		void SetConfigOptions()
		{
			int monitor = Graphics.CurrentMonitor;

			if (Config.DebugHUD) {
				monoDebugHud = new MonoDebugHud(this);
				monoDebugHud.Show();
			}

			Config.SetGraphicsMode(Graphics);

		}

		static void ErrorRecovery(object s, Urho.UnhandledExceptionEventArgs e)
		{
			if (Debugger.IsAttached)
			{
				//Debugger.Break();
			}

			string message = $"Native exception occured: {e.Exception.Message}";
			Urho.IO.Log.Write(LogLevel.Error, message);
			//e.Handled = true;
			/*
			LevelManager.CurrentLevel?.End();
			Instance.MenuController.InitialSwitchToMainMenu("Game error", message);
			*/
		}
	
	}
}
