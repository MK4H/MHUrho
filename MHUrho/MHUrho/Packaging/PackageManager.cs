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

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="resourceCache"></param>
		/// <returns>Paths of the packages that failed to load</returns>
		public static string[] CreateInstance(ResourceCache resourceCache) {
			Instance = new PackageManager(resourceCache);
			try {

				Instance.schemas.Add(XMLNamespace.NamespaceName, XmlReader.Create(MyGame.Files.OpenStaticFileRO(GamePackageSchemaPath)));
			}
			catch (IOException e) {
				string message = $"Error loading GamePack schema: {e.Message}";
				Log.Write(LogLevel.Error, message);
				if (Debugger.IsAttached) Debugger.Break();
				//Reading of static file of this app failed, something is horribly wrong, die
				//TODO: Error reading static data of app, game data corrupted
				throw new FatalPackagingException(message, e);
			}

			try {
				Instance.DefaultIcon = resourceCache.GetTexture2D(defaultIconPath);
			}
			catch (IOException e) {
				string message = $"Error loading the default icon at {defaultIconPath}";
				Log.Write(LogLevel.Error, message);
				if (Debugger.IsAttached) Debugger.Break();
				//TODO: Error loading the default icon, game corrupted
				throw new FatalPackagingException(message, e);
			}

			return Instance.ParseGamePackDir();
		}

		/// <summary>
		///	<para>Unloads the <see cref="ActivePackage"/> if there is any
		/// and then loads the <see cref="GamePack"/> represented by <paramref name="packageName"/></para>
		///	
		///
		/// <para>Optionally can signal loading progress if provided with <paramref name="loadingProgress"/></para>
		/// </summary>
		/// <param name="packageName">Name of the package to be loaded</param>
		/// <param name="loadingProgress">Optional watcher of the loading progress</param>
		/// <returns>A task that represents the asynchronous loading of the package</returns>
		/// <exception cref="PackageLoadingException">Thrown when the loading of the package failed</exception>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="packageName"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="packageName"/> is not present in the <see cref="AvailablePacks"/></exception>
		public Task<GamePack> LoadPackage(string packageName, 
										ILoadingSignaler loadingProgress = null)
		{
			if (packageName == null) {
				throw new ArgumentNullException(nameof(packageName), "Package name cannot be null");
			}

			if (availablePacks.TryGetValue(packageName, out GamePackRep packageRep)) {
				return LoadPackage(packageRep, loadingProgress);
			}
			else {
				throw new ArgumentOutOfRangeException(nameof(packageName),
													packageName,
													"No package of this name is registered");
			}

			
		}

		/// <summary>
		/// <para>Unloads the <see cref="ActivePackage"/> if there is any and then loads
		/// the package represented by <paramref name="package"/></para>
		///
		/// <para>Optionally can signal loading progress if provided with <paramref name="loadingProgress"/></para>
		/// </summary>
		/// <param name="package">Representation of the package to be loaded</param>
		/// <param name="loadingProgress">Optional watcher of the loading progress</param>
		/// <returns>A task that represents the asynchronous loading of the package</returns>
		/// <exception cref="PackageLoadingException">Thrown when the loading of the new package failed</exception>
		public async Task<GamePack> LoadPackage(GamePackRep package, ILoadingSignaler loadingProgress = null)
		{
			if (loadingProgress == null) {
				loadingProgress = new LoadingWatcher();
			}

			loadingProgress.TextUpdate("Clearing previous games");
			if (ActivePackage != null) {
				UnloadActivePack();
			}

			resourceCache.AddResourceDir(Path.Combine(MyGame.Files.DynamicDirPath,package.XmlDirectoryPath), 1);

			ActivePackage = await package.LoadPack(schemas, loadingProgress.GetWatcherForSubsection());
			return ActivePackage;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xmlRelativePath">Path of the xml file relative to the <see cref="PackageDirectoryAbsolutePath"/></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"/>
		/// <exception cref="FatalPackagingException"/>
		/// <exception cref="PackageLoadingException"/>
		public GamePackRep AddGamePack(string xmlRelativePath)
		{
			
			GamePackRep newPack = LoadPack(xmlRelativePath);
			
			if (newPack != null) {

				if (availablePacks.ContainsKey(newPack.Name)) {
					throw new ArgumentException("GamePack of the same name was already loaded",
												nameof(xmlRelativePath));
				}

				//Already validated
				XDocument document;
				try {
					document = LoadGamePackDirXml(GamePackDirFilePath);
				}
				catch (XmlSchemaValidationException e) {
					throw new
						FatalPackagingException($"Package directory xml document does not conform to GamePack.xsd schema: {e.Message}",
												e);
				}

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
			XDocument document = null;
			try {
				document = LoadGamePackDirXml(GamePackDirFilePath);
			}
			catch (XmlSchemaValidationException e) {
				throw new
					FatalPackagingException($"Package directory xml document does not conform to GamePack.xsd schema: {e.Message}",
											e);
			}
			catch (IOException e) {
				throw new FatalPackagingException($"Could not open the package directory xml file, {e.Message}", e);
			}

			

			XElement packDirElement = (from element in document.Root.Elements()
									where element.Value == dirEntry
									select element).FirstOrDefault();

			if (packDirElement == null) {
				throw new FatalPackagingException($"Did not find entry for the gamePack at {dirEntry} in gamePack directory xml file");
			}

			packDirElement.Remove();

			try {
				WriteGamePackDir(document, GamePackDirFilePath);
			}
			catch (XmlSchemaValidationException e)
			{
				throw new
					FatalPackagingException($"Package directory xml document did not conform to GamePack.xsd schema after adding new entry: {e.Message}",
											e);
			}
			catch (IOException e)
			{
				throw new FatalPackagingException($"Could not write to the package directory xml file, {e.Message}", e);
			}

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

		public Animation GetAnimation(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetAnimation(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public AnimationSet2D GetAnimationSet2D(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetAnimationSet2D(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Urho.IO.File GetFile(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetFile(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Font GetFont(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetFont(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Image GetImage(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetImage(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public JsonFile GetJsonFile(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetJsonFile(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Material GetMaterial(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetMaterial(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Model GetModel(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetModel(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public ParticleEffect GetParticleEffect(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetParticleEffect(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public ParticleEffect2D GetParticleEffect2D(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetParticleEffect2D(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public PListFile GetPListFile(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetPListFile(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Shader GetShader(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetShader(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Sound GetSound(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetSound(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Sprite2D GetSprite2D(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetSprite2D(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public SpriteSheet2D GetSpriteSheet2D(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetSpriteSheet2D(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Technique GetTechnique(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetTechnique(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Texture2D GetTexture2D(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetTexture2D(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Texture3D GetTexture3D(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetTexture3D(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public Texture GetTextureCube(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetTextureCube(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public TmxFile2D GetTmxFile2D(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetTmxFile2D(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public ValueAnimation GetValueAnimation(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetValueAnimation(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

		}

		public XmlFile GetXmlFile(string name, bool throwOnFailure = false)
		{
			var resource = MyGame.InvokeOnMainSafe(() => resourceCache.GetXmlFile(name));
			return !throwOnFailure ? resource : resource ?? throw new ResourceLoadingException(name);

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
		/// <exception cref="FatalPackagingException">Thrown when package directory loading completely failed, and the game can terminate</exception>
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
				string message = $"Opening ResourcePack directory file at {GamePackDirFilePath} failed: {e}";
				Log.Write(LogLevel.Error, message);
				if (Debugger.IsAttached) Debugger.Break();
				throw new FatalPackagingException(message, e);
			}
			//TODO: Exceptions
			catch (XmlSchemaValidationException e) {
				string message =
					$"ResourcePack directory file at {GamePackDirFilePath} does not conform to the schema: {e.Message}";
				//Invalid resource pack description file, dont load this pack directory
				Log.Write(LogLevel.Error, message);
				if (Debugger.IsAttached) Debugger.Break();
				throw new FatalPackagingException(message, e);
			}
			catch (XmlException e) {
				string message = $"ResourcePack directory file at {GamePackDirFilePath} was corrupted : {e.Message}";
				Log.Write(LogLevel.Error, message);
				if (Debugger.IsAttached) Debugger.Break();
				throw new FatalPackagingException(message, e);
			}

			List<string> failedPackagePaths = new List<string>();
			//Adds all the discovered packs into the availablePacks list
			foreach (var packagePath in packagePaths) {
				try {
					GamePackRep newPack = LoadPack(packagePath);
					AddToAvailable(newPack, packagePath);
				}
				catch (Exception) {
					//The package writes the error to the log by itself
					//Urho.IO.Log.Write(LogLevel.Warning, $"Loading package at {packagePath} failed with: {e}");
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
	
			newPack = new GamePackRep(packXmlPath,
									this,
									schemas);

			return newPack;
		}

		void AddToAvailable(GamePackRep newPack, string pathInDirEntry)
		{
			if (availablePacks.ContainsKey(newPack.Name)) {
				string message = $"GamePack of the name \"{newPack.Name}\" from \"{newPack.XmlDirectoryPath}\" was already loaded.";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new ArgumentException(message,nameof(newPack));
			}

			availablePacks.Add(newPack.Name, newPack);
			dirEntries.Add(newPack, pathInDirEntry);
		}

		void RemoveFromAvailable(GamePackRep gamePack)
		{
			if (!availablePacks.ContainsValue(gamePack)) {
				string message =
					$"GamePack {gamePack.Name} at {gamePack.XmlDirectoryPath} was not registered as available";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new ArgumentException(message, nameof(gamePack));
			}

			availablePacks.Remove(gamePack.Name);
			dirEntries.Remove(gamePack);
		}
	}
}
