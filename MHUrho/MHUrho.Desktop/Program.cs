using System.Collections.Generic;
using Urho;
using MHUrho;
using System.IO;
using MHUrho.Packaging;
using MHUrho.StartupManagement;


namespace MHUrho.Desktop
{
	class Program
	{
		static void Main(string[] args) {

			MyGame.Files = FileManagerDesktop.LoadFileManager();


			if (!MyGame.Files.FileExists(Path.Combine(MyGame.Files.PackageDirectoryAbsolutePath, PackageManager.GamePackDirFileName))) {
				MyGame.Files.Copy(Path.Combine(MyGame.Files.StaticDirPath, "Data", "Test", "ResourceDir"),
								MyGame.Files.PackageDirectoryAbsolutePath, false);
			}

			MyGame.StartupOptions = StartupOptions.FromCommandLineParams(args);

			new MyGame(new ApplicationOptions("Data")).Run();

		}
	}
}
