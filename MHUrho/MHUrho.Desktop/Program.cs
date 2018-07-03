using System.Collections.Generic;
using Urho;
using MHUrho;
using System.IO;


namespace MHUrho.Desktop
{
	class Program
	{
		static void Main(string[] args) {

			MyGame.Files = FileManagerDesktop.LoadFileManager();
			MyGame.Files.CopyStaticToDynamic(Path.Combine("Data","Test"));

			new MyGame(new ApplicationOptions("Data")).Run();

		}
	}
}
