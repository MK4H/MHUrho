using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using Urho.IO;
using Urho.Resources;
using Path = System.IO.Path;

namespace MHUrho.Packaging
{
	/// <summary>
	/// ResourceCache wrapper providing loading, unloading and downloading of GamePacks
	/// </summary>
	public class PackageManager {
		public static XNamespace XMLNamespace = "http://www.MobileHold.cz/GamePack.xsd";

		public static PackageManager Instance { get; private set; }

		public ResourceCache ResourceCache { get; private set; }

		/// <summary>
		/// Path to the schema for Resource Pack Directory xml files
		/// </summary>
		private static readonly string GamePackageSchemaPath = Path.Combine("Data","Schemas","GamePack.xsd");


		public GamePack ActiveGame { get; private set; }

		private readonly XmlSchemaSet schemas;

		private readonly Dictionary<string, GamePack> availablePacks = new Dictionary<string, GamePack>();



		protected PackageManager(ResourceCache resourceCache) {
			this.ResourceCache = resourceCache;

			schemas = new XmlSchemaSet();
		}

		public static void CreateInstance(ResourceCache resourceCache) {
			Instance = new PackageManager(resourceCache);
			try {

				Instance.schemas.Add(XMLNamespace.NamespaceName, XmlReader.Create(MyGame.Config.OpenStaticFileRO(GamePackageSchemaPath)));
			}
			catch (IOException e) {
				Log.Write(LogLevel.Error, string.Format("Error loading GamePack schema: {0}", e));
				if (Debugger.IsAttached) Debugger.Break();
				//Reading of static file of this app failed, something is horribly wrong, die
				//TODO: Error reading static data of app
			}

			foreach (var path in MyGame.Config.PackagePaths) {
				Instance.ParseGamePackDir(path);
			}
		}

		public void LoadPackage(string packageName) {

			if (ActiveGame != null) {
				UnloadPackage(ActiveGame);
				ActiveGame = null;
			}

			ActiveGame = availablePacks[packageName];

			ResourceCache.AddResourceDir(ActiveGame.XmlDirectoryPath,1);

			ActiveGame.Load(schemas);
		}

		public GamePack GetGamePack(string name) {
			//TODO: React if it does not exist
			return availablePacks[name];
		}
	
		/// <summary>
		/// Pulls data about the resource packs contained in this directory from XML file
		/// </summary>
		/// <param name="path">Path to the XML file of Resource pack directory</param>
		/// <param name="schema">Schema for the resource pack directory type of XML files</param>
		/// <returns>True if successfuly read, False if there was an error while loading</returns>
		private void ParseGamePackDir(string path)
		{

			IEnumerable<GamePack> loadedPacks = null;

			try
			{
				XDocument doc = XDocument.Load(MyGame.Config.OpenDynamicFile(path, System.IO.FileMode.Open, FileAccess.Read));
				doc.Validate(schemas, null);

				string directoryPath = Path.GetDirectoryName(path);

				loadedPacks = from packages in doc.Root.Elements(XMLNamespace + "gamePack")
							  select GamePack.InitialLoad(packages.Attribute("name").Value,
													//PathtoXml is relative to GamePackDir.xml directory path
													Path.Combine(directoryPath,packages.Element(XMLNamespace + "pathToXml").Value), 
													packages.Element(XMLNamespace + "description")?.Value,
													packages.Element(XMLNamespace + "thumbnailPath")?.Value,
													this);
			}
			catch (IOException e)
			{
				//Creation of the FileStream failed, cannot load this directory
				Log.Write(LogLevel.Warning, string.Format("Opening ResroucePack directory file at {0} failed: {1}", path,e));
				if (Debugger.IsAttached) Debugger.Break();
			}
			//TODO: Exceptions
			catch (XmlSchemaValidationException e)
			{
				//Invalid resource pack description file, dont load this pack directory
				Log.Write(LogLevel.Warning, string.Format("ResroucePack directory file at {0} does not conform to the schema: {1}", path, e));
				if (Debugger.IsAttached) Debugger.Break();
			}
			catch (XmlException e)
			{
				//TODO: Alert user for corrupt file
				Log.Write(LogLevel.Warning, string.Format("ResroucePack directory file at {0} : {1}", path, e));
				if (Debugger.IsAttached) Debugger.Break();
			}

			//If loading failed completely, dont add anything
			if (loadedPacks == null) return;

			//Adds all the discovered packs into the availablePacks list
			foreach (var loadedPack in loadedPacks) {
				availablePacks.Add(loadedPack.Name, loadedPack);
			}

		}


		private void UnloadPackage(GamePack package) {
			ResourceCache.RemoveResourceDir(package.XmlDirectoryPath);
			package.UnLoad();
		}


	}
}
