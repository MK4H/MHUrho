using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using Urho.Audio;
using Urho.Gui;
using Urho.IO;
using Urho.Resources;
using Urho.Urho2D;
using Path = System.IO.Path;

namespace MHUrho.Packaging
{
	/// <summary>
	/// ResourceCache wrapper providing loading, unloading and downloading of GamePacks
	/// </summary>
	public class PackageManager  {
		public static XNamespace XMLNamespace = "http://www.MobileHold.cz/GamePack.xsd";

		public static PackageManager Instance { get; private set; }

		public static string PackageDirectoryPath => MyGame.Files.PackageDirectoryPath;

		public static string PackageDirectoryAbsolutePath => MyGame.Files.PackageDirectoryAbsolutePath;

		public const string GamePackDirFileName = "DirDescription.xml";

		public IEnumerable<GamePackRep> AvailablePacks => availablePacks.Values;



		/// <summary>
		/// Path to the schema for Resource Pack Directory xml files
		/// </summary>
		static readonly string GamePackageSchemaPath = Path.Combine("Data","Schemas","GamePack.xsd");

		static string GamePackDirFilePath => Path.Combine(PackageDirectoryPath, GamePackDirFileName);

		static string GamePackDirFileAbsolutePath => Path.Combine(PackageDirectoryAbsolutePath, GamePackDirFileName);

		static readonly string defaultIconPath = Path.Combine("Textures","xamarin.png");


		public Texture2D DefaultIcon { get; private set; }

		public GamePack ActivePackage { get; private set; }

		readonly XmlSchemaSet schemas;

		readonly Dictionary<string, GamePackRep> availablePacks = new Dictionary<string, GamePackRep>();
		readonly Dictionary<GamePackRep, string> dirEntries = new Dictionary<GamePackRep, string>();
		readonly ResourceCache resourceCache;


		protected PackageManager(ResourceCache resourceCache) {
			this.resourceCache = resourceCache;

			schemas = new XmlSchemaSet();
		}

		public static void CreateInstance(ResourceCache resourceCache) {
			Instance = new PackageManager(resourceCache);
			try {

				Instance.schemas.Add(XMLNamespace.NamespaceName, XmlReader.Create(MyGame.Files.OpenStaticFileRO(GamePackageSchemaPath)));
			}
			catch (IOException e) {
				Log.Write(LogLevel.Error, $"Error loading GamePack schema: {e}");
				if (Debugger.IsAttached) Debugger.Break();
				//Reading of static file of this app failed, something is horribly wrong, die
				//TODO: Error reading static data of app, game data corrupted
			}

			try {
				Instance.DefaultIcon = resourceCache.GetTexture2D(defaultIconPath);
			}
			catch (IOException e) {
				Log.Write(LogLevel.Error, $"Error loading the default icon at {defaultIconPath}");
				if (Debugger.IsAttached) Debugger.Break();
				//TODO: Error loading the default icon, game corrupted
			}

			string[] failedPackagePaths = Instance.ParseGamePackDir();
			//TODO: Display as error message
		}

		public GamePack LoadPackage(string packageName, 
								ILoadingSignaler loadingProgress = null)
		{
			return LoadPackage(availablePacks[packageName], loadingProgress);
		}

		public GamePack LoadPackage(GamePackRep package, ILoadingSignaler loadingProgress = null)
		{
			if (loadingProgress == null) {
				loadingProgress = new LoadingWatcher();
			}

			loadingProgress.TextAndPercentageUpdate("Clearing previous games", 5);
			if (ActivePackage != null) {
				UnloadActivePack();
			}

			resourceCache.AddResourceDir(Path.Combine(MyGame.Files.DynamicDirPath,package.XmlDirectoryPath), 1);

			ActivePackage = package.LoadPack(schemas, loadingProgress);
			return ActivePackage;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xmlRelativePath">Path of the xml file relative to the <see cref="PackageDirectoryAbsolutePath"/></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"/>
		public GamePackRep AddGamePack(string xmlRelativePath)
		{
			
			GamePackRep newPack = LoadPack(xmlRelativePath);
			
			if (newPack != null) {

				if (availablePacks.ContainsKey(newPack.Name)) {
					throw new ArgumentException("GamePack of the same name was already loaded",
												nameof(xmlRelativePath));
				}

				//Already validated
				XDocument document = LoadGamePackDirXml(GamePackDirFilePath);

				XElement root = document.Root;
				root.Add(new XElement(GamePackDirectoryXml.Inst.GamePack, xmlRelativePath));

				WriteGamePackDir(document, GamePackDirFilePath);

				AddToAvailable(newPack, xmlRelativePath);
			}

			return newPack;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gamePack"></param>
		public void RemoveGamePack(GamePackRep gamePack)
		{
			if (!availablePacks.ContainsValue(gamePack))
			{
				throw new ArgumentException("GamePack was not registered as available");
			}

			string dirEntry = dirEntries[gamePack];

			//TODO: Exception
			//Already validated
			XDocument document = LoadGamePackDirXml(GamePackDirFilePath);

			

			XElement packDirElement = (from element in document.Root.Elements()
									where element.Value == dirEntry
									select element).FirstOrDefault();

			if (packDirElement == null) {
				throw new ArgumentException("Did not find entry for the gamePack in gamePack directory xml file",
											nameof(gamePack));
			}

			packDirElement.Remove();

			//TODO: Exception
			WriteGamePackDir(document, GamePackDirFilePath);

			//Only after the change to xml file went through, remove from runtime
			RemoveFromAvailable(gamePack);
		}

		public GamePackRep GetGamePack(string name) {
			//TODO: React if it does not exist
			return availablePacks[name];
		}

		public void UnloadActivePack()
		{
			var activePackage = ActivePackage;
			ActivePackage = null;
			UnloadPackage(activePackage);
		}

		public bool Exists(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.Exists(name));
		}

		public Animation GetAnimation(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetAnimation(name));
		}

		public AnimationSet2D GetAnimationSet2D(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetAnimationSet2D(name));
		}

		public Urho.IO.File GetFile(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetFile(name));
		}

		public Font GetFont(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetFont(name));
		}

		public Image GetImage(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetImage(name));
		}

		public JsonFile GetJsonFile(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetJsonFile(name));
		}

		public Material GetMaterial(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetMaterial(name));
		}

		public Model GetModel(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetModel(name));
		}

		public ParticleEffect GetParticleEffect(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetParticleEffect(name));
		}

		public ParticleEffect2D GetParticleEffect2D(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetParticleEffect2D(name));
		}

		public PListFile GetPListFile(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetPListFile(name));
		}

		public Shader GetShader(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetShader(name));
		}

		public Sound GetSound(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetSound(name));
		}

		public Sprite2D GetSprite2D(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetSprite2D(name));
		}

		public SpriteSheet2D GetSpriteSheet2D(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetSpriteSheet2D(name));
		}

		public Technique GetTechnique(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetTechnique(name));
		}

		public Texture2D GetTexture2D(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetTexture2D(name));
		}

		public Texture3D GetTexture3D(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetTexture3D(name));
		}

		public Texture GetTextureCube(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetTextureCube(name));
		}

		public TmxFile2D GetTmxFile2D(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetTmxFile2D(name));
		}

		public ValueAnimation GetValueAnimation(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetValueAnimation(name));
		}

		public XmlFile GetXmlFile(string name)
		{
			return MyGame.InvokeOnMainSafe(() => resourceCache.GetXmlFile(name));
		}

		public Material GetMaterialFromImage(Image image)
		{
			return MyGame.InvokeOnMainSafe(() => Material.FromImage(image));
		}

		public Material GetMaterialFromImage(string image)
		{
			return MyGame.InvokeOnMainSafe(() => Material.FromImage(image));
		}

		public Material GetMaterialFromImage(string image, string normals)
		{
			return MyGame.InvokeOnMainSafe(() => Material.FromImage(image, normals));
		}

		/// <summary>
		/// Pulls data about the resource packs contained in this directory from XML file
		/// </summary>
		/// <returns>Array of package paths that could not be loaded</returns>
		string[] ParseGamePackDir()
		{
			IEnumerable<string> packagePaths = null;

			try {

				XDocument doc = LoadGamePackDirXml(GamePackDirFilePath);

				packagePaths = from packagePath in doc.Root.Elements(GamePackDirectoryXml.Inst.GamePack)
							   select packagePath.Value;
			}
			catch (IOException e)
			{
				//Creation of the FileStream failed, cannot load this directory
				Log.Write(LogLevel.Warning, $"Opening ResourcePack directory file at {GamePackDirFilePath} failed: {e}");
				if (Debugger.IsAttached) Debugger.Break();
				throw;
			}
			//TODO: Exceptions
			catch (XmlSchemaValidationException e)
			{
				//Invalid resource pack description file, dont load this pack directory
				Log.Write(LogLevel.Warning, $"ResourcePack directory file at {GamePackDirFilePath} does not conform to the schema: {e}");
				if (Debugger.IsAttached) Debugger.Break();
				throw;
			}
			catch (XmlException e)
			{
				//TODO: Alert user for corrupt file
				Log.Write(LogLevel.Warning, $"ResourcePack directory file at {GamePackDirFilePath} was corrupted : {e}");
				if (Debugger.IsAttached) Debugger.Break();
				throw;
			}

			List<string> failedPackagePaths = new List<string>();
			//Adds all the discovered packs into the availablePacks list
			foreach (var packagePath in packagePaths) {
				try {
					GamePackRep newPack = LoadPack(packagePath);
					AddToAvailable(newPack, packagePath);
				}
				catch (Exception e) {
					Urho.IO.Log.Write(LogLevel.Warning, $"Loading package at {packagePath} failed with: {e}");
					failedPackagePaths.Add(packagePath);
				}
			}

			return failedPackagePaths.ToArray();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns>Validated xml file of game pack directory</returns>
		/// <exception cref="XmlSchemaValidationException"/>
		/// <exception cref="IOException"/>
		XDocument LoadGamePackDirXml(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path), "Path cannot be null");
			}

			using (Stream stream = MyGame.Files.OpenDynamicFile(path, System.IO.FileMode.Open, FileAccess.Read)) {
				XDocument doc = XDocument.Load(stream);
				doc.Validate(schemas, null);
				return doc;
			}						
		}

		/// <summary>
		/// Validates the <paramref name="document"/> and writes it to the file at <paramref name="path"/>
		///
		/// If the <paramref name="document"/> is invalid, throws <see cref="XmlSchemaValidationException"/>
		/// If the file IO cannot be processed, throws some child of <see cref="IOException"/>
		/// </summary>
		/// <param name="document"></param>
		/// <param name="path"></param>
		/// <exception cref="XmlSchemaValidationException"/>
		/// <exception cref="IOException"/>
		void WriteGamePackDir(XDocument document, string path)
		{
			document.Validate(schemas, null);
			using (Stream stream = MyGame.Files.OpenDynamicFile(path, System.IO.FileMode.Truncate, FileAccess.Write)) {
				document.Save(stream);
			}
		}

		void UnloadPackage(GamePack package) {
			resourceCache.RemoveResourceDir(package.DirectoryPath);
			package.UnLoad();
		}


		GamePackRep LoadPack(string pathInDirEntry)
		{
			string packXmlPath = Path.Combine(PackageDirectoryPath, FileManager.CorrectRelativePath(pathInDirEntry));
			GamePackRep newPack = null;
			try
			{
				newPack = new GamePackRep(packXmlPath,
										this,
										schemas);
			}
			catch (XmlSchemaValidationException e)
			{
				//TODO: Exception
				throw;
			}
			catch (IOException e)
			{
				throw;
			}

			return newPack;
		}

		void AddToAvailable(GamePackRep newPack, string pathInDirEntry)
		{
			if (availablePacks.ContainsKey(newPack.Name))
			{
				throw new ArgumentException("GamePack of the same name was already loaded",
											nameof(newPack));
			}

			availablePacks.Add(newPack.Name, newPack);
			dirEntries.Add(newPack, pathInDirEntry);
		}

		void RemoveFromAvailable(GamePackRep gamePack)
		{
			if (!availablePacks.ContainsValue(gamePack))
			{
				throw new ArgumentException("GamePack was not registered as available");
			}

			availablePacks.Remove(gamePack.Name);
			dirEntries.Remove(gamePack);
		}
	}
}
