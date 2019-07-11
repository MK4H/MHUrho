using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Urho;
using Urho.Urho2D;

namespace MHUrho.Packaging
{
	/// <summary>
	/// Represents a game package that can be loaded into the currently running instance of the platform.
	/// Can start the loading of the package.
	/// This class is used to enable quick validation of the package integrity and partial load, to preserve memory.
	/// </summary>
    public class GamePackRep {

		public string Name { get; private set; }

		public string Description { get; private set; }

		public Texture2D Thumbnail { get; private set; }

		public PackageManager PackageManager { get; private set; }

		public string XmlDirectoryPath => System.IO.Path.GetDirectoryName(pathToXml);

		readonly string pathToXml;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pathToXml"></param>
		/// <param name="packageManager"></param>
		/// <param name="schemas"></param>
		/// <exception cref="PackageLoadingException">Thrown when the package loading failed for any reason</exception>
		public GamePackRep(string pathToXml, PackageManager packageManager, XmlSchemaSet schemas)
		{
			this.pathToXml = pathToXml;
			this.PackageManager = packageManager;

			Stream file = null;
			XDocument data = null;
			try {
				file = packageManager.App.Files.OpenDynamicFile(pathToXml, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				data = XDocument.Load(file);
				data.Validate(schemas, null);
			}
			catch (XmlSchemaValidationException e) {
				string errorMessage =
					$"Package XML was invalid.{Environment.NewLine} Package at: {pathToXml}{Environment.NewLine} Xml validation error: {e.Message}";
				Urho.IO.Log.Write(LogLevel.Warning, errorMessage);
				throw new PackageLoadingException(errorMessage, e);
			}
			catch (XmlException e) {
				string errorMessage = $"File at {pathToXml} was not an XML file:{Environment.NewLine}{e.Message}";
				Urho.IO.Log.Write(LogLevel.Warning, errorMessage);
				throw new PackageLoadingException(errorMessage, e);
			}
			catch (IOException e) {
				string errorMessage = $"File operation with package XML file at \"{pathToXml}\" failed:{Environment.NewLine}{e.Message}";
				Urho.IO.Log.Write(LogLevel.Warning, errorMessage);
				throw new PackageLoadingException(errorMessage, e);
			}
			finally {
				file?.Dispose();
			}

			XElement packageElement = data.Element(GamePackXml.Inst.GamePackElement);

			if (packageElement == null) {
				string message = $"Package XML did not have root element {GamePackXml.Inst.GamePackElement}.";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message);
			}

			//Element should not be null because the XML was validated and the schema does not allow it
			Name = packageElement.Attribute(GamePackXml.Inst.NameAttribute).Value;

			//Description element is optional in the XML schema
			Description = packageElement.Element(GamePackXml.Inst.Description)?.Value ?? "";

			//Thumbnail path element is optional in the XML schema
			string thumbnailPath = packageElement.Element(GamePackXml.Inst.PathToThumbnail)?.Value;
			if (thumbnailPath != null) {
				thumbnailPath = Path.Combine(XmlDirectoryPath, FileManager.ReplaceDirectorySeparators(thumbnailPath));
				Thumbnail = packageManager.GetTexture2D(thumbnailPath);
			}
			else {
				//If no thumbnail provided, show default icon
				Thumbnail = packageManager.DefaultIcon;
			}
			
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="schemas"></param>
		/// <param name="loadingProgress"></param>
		/// <returns></returns>
		/// <exception cref="PackageLoadingException">Thrown when the package loading failed</exception>
		public Task<GamePack> LoadPack(XmlSchemaSet schemas, IProgressEventWatcher loadingProgress)
		{
			return GamePack.Load(pathToXml, this, schemas, loadingProgress);
		}
	}
}
