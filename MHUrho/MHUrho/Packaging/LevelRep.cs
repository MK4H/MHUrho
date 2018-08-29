using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Plugins;
using Urho.Urho2D;

namespace MHUrho.Packaging
{
    public class LevelRep {
		static readonly XName DescriptionElement = PackageManager.XMLNamespace + "description";
		static readonly XName ThumbnailElement = PackageManager.XMLNamespace + "thumbnail";
		static readonly XName AssemblyPath = PackageManager.XMLNamespace + "assemblyPath";


		public string Name { get; private set; }
		public string Description { get; private set; }

		public Texture2D Thumbnail { get; private set; }

		public LevelLogicPlugin LevelPlugin { get; private set; }

		public GamePack GamePack { get; private set; }

		public string LevelPluginAssemblyPath { get; private set; }

		string savePath;

		MyGame game;

		public LevelRep(GamePack gamePack, XElement levelXmlElement)
		{
			this.GamePack = gamePack;
			//TODO: Check for errors
			Name = XmlHelpers.GetName(levelXmlElement);
			Description = levelXmlElement.Element(DescriptionElement).GetString();
			Thumbnail = PackageManager.Instance.GetTexture2D( XmlHelpers.GetPath(levelXmlElement.Element(ThumbnailElement)));

			LevelPluginAssemblyPath = FileManager.CorrectRelativePath(levelXmlElement.Element(AssemblyPath).GetString());

			string levelPluginPath = Path.Combine(gamePack.RootedXmlDirectoryPath,
												LevelPluginAssemblyPath);

			LevelPlugin = LevelLogicPlugin.Load(levelPluginPath, Name);
		}

		public ILevelLoader StartLoading(bool editorMode)
		{
			Stream saveFile = MyGame.Files.OpenDynamicFile(savePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			var loader = LevelManager.GetLoader(game);
			loader.LoadFrom(saveFile, editorMode);
			return loader;
		}

    }
}
