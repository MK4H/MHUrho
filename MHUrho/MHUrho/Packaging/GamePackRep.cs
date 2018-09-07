using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using Urho;
using Urho.Urho2D;

namespace MHUrho.Packaging
{
    public class GamePackRep {
		static readonly XName PackageElement = PackageManager.XMLNamespace + "gamePack";
		static readonly XName NameAttribute = "name";
		static readonly XName DescriptionElement = PackageManager.XMLNamespace + "description";
		static readonly XName ThumbnailElement = PackageManager.XMLNamespace + "thumbnailPath";

		public string Name { get; private set; }

		public string Description { get; private set; }

		public Texture2D Thumbnail { get; private set; }

		public PackageManager PackageManager { get; private set; }

		public string XmlDirectoryPath => System.IO.Path.GetDirectoryName(pathToXml);

		readonly string pathToXml;

		public GamePackRep(string pathToXml, PackageManager packageManager, XmlSchemaSet schemas)
		{
			this.pathToXml = pathToXml;
			this.PackageManager = packageManager;

			Stream file = null;
			XDocument data = null;
			try {
				file = MyGame.Files.OpenDynamicFile(pathToXml, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				data = XDocument.Load(file);
				data.Validate(schemas, null);
			}
			catch (XmlSchemaValidationException e) {
				Urho.IO.Log.Write(LogLevel.Warning, $"Package XML was invalid. Package at: {pathToXml}");
				//TODO: Exception
				throw new ApplicationException($"Package XML was invalid. Package at: {pathToXml}", e);
			}
			finally {
				file?.Dispose();
			}

			XElement packageElement = data.Element(PackageElement);

			Name = packageElement.Attribute(NameAttribute).Value;
			Description = packageElement.Element(DescriptionElement)?.Value ?? "";

			string thumbnailPath = packageElement.Element(ThumbnailElement)?.Value;
			if (thumbnailPath != null) {
				thumbnailPath = Path.Combine(XmlDirectoryPath, FileManager.CorrectRelativePath(thumbnailPath));
				Thumbnail = PackageManager.Instance.GetTexture2D(thumbnailPath);
			}
			else {
				Thumbnail = PackageManager.Instance.DefaultIcon;
			}
			
		}

		public GamePack LoadPack(XmlSchemaSet schemas, ILoadingSignaler loadingProgress)
		{
			return new GamePack(pathToXml, this, schemas, loadingProgress);
		}
	}
}
