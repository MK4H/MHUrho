using System;
using System.Collections.Generic;
using System.Configuration;
using Urho;
using MHUrho;
using System.IO;
using MHUrho.Packaging;
using MHUrho.StartupManagement;


namespace MHUrho.Desktop
{
	class Program {
		const string appDataDirName = "MHUrho";

		static void Main(string[] args)
		{
#if DEBUG
			string appDataAppPath = Path.Combine(Directory.GetCurrentDirectory(), "DynData");
#else
			string appDataAppPath = null;
			if ((appDataAppPath = ConfigurationManager.AppSettings["appDataPath"]) == null) {
				appDataAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appDataDirName);
			}
#endif


			MHUrhoApp.FileManager = FileManagerDesktop.LoadFileManager(Directory.GetCurrentDirectory(),
															  appDataAppPath,
															  Path.Combine(appDataAppPath, "Packages"),
															  Path.Combine(appDataAppPath, "Log.txt"),
															  "config.xml",
															  "SavedGames");

			MHUrhoApp.StartupArgs = StartupOptions.FromCommandLineParams(args, MHUrhoApp.FileManager);

			try {
				new MHUrhoApp(new ApplicationOptions("Data")).Run();
			}
			catch (InvalidOperationException) {
				//Ignore, Error with the current release of UrhoSharp https://github.com/xamarin/urho-samples/issues/45
			}

		}
	}
}
