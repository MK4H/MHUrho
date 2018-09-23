using System.Collections.Generic;
using Urho;
using MHUrho;
using System.IO;
using MHUrho.StartupManagement;


namespace MHUrho.Desktop
{
	class Program
	{
		static void Main(string[] args) {

			MyGame.Files = FileManagerDesktop.LoadFileManager();
			MyGame.StartupOptions = StartupOptions.FromCommandLineParams(args);

			MyGame.Files.CopyStaticToDynamic(Path.Combine("Data", "Test"));

			new MyGame(new ApplicationOptions("Data")).Run();

		}
	}
}
