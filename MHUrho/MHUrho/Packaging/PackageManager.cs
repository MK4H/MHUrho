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

		public IEnumerable<GamePackRep> AvailablePacks => availablePacks.Values;
		
		/// <summary>
		/// Path to the schema for Resource Pack Directory xml files
		/// </summary>
		static readonly string GamePackageSchemaPath = Path.Combine("Data","Schemas","GamePack.xsd");

		const string defaultIconPath = "Textures/xamarin.png";
		public Texture2D DefaultIcon { get; private set; }

		public GamePack ActivePackage { get; private set; }

		readonly XmlSchemaSet schemas;

		readonly Dictionary<string, GamePackRep> availablePacks = new Dictionary<string, GamePackRep>();
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
				;
				Instance.DefaultIcon = resourceCache.GetTexture2D(defaultIconPath);
			}
			catch (IOException e) {
				Log.Write(LogLevel.Error, $"Error loading the default icon at {defaultIconPath}");
				if (Debugger.IsAttached) Debugger.Break();
				//TODO: Error loading the default icon, game corrupted
			}

			foreach (var path in MyGame.Files.PackagePaths) {
				Instance.ParseGamePackDir(path);
			}
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

			resourceCache.AddResourceDir(package.XmlDirectoryPath, 1);

			ActivePackage = package.LoadPack(schemas, loadingProgress);
			return ActivePackage;
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
		/// <param name="path">Path to the XML file of Resource pack directory</param>
		/// <returns>True if successfuly read, False if there was an error while loading</returns>
		void ParseGamePackDir(string path)
		{

			if (path == null) {
				//TODO: Exception
				throw new ArgumentNullException();
			}

			IEnumerable<GamePackRep> presentPacks = null;

			try
			{
				XDocument doc = XDocument.Load(MyGame.Files.OpenDynamicFile(path, System.IO.FileMode.Open, FileAccess.Read));
				doc.Validate(schemas, null);

				string directoryPath = Path.GetDirectoryName(path);

				presentPacks = from packagePath in doc.Root.Elements(XMLNamespace + "gamePack")
								select new GamePackRep(Path.Combine(directoryPath, 
																	FileManager.CorrectRelativePath(packagePath.Value)),
														this,
														schemas);
				//select GamePack.InitialLoad(packages.Attribute("name").Value,
				//				//PathtoXml is relative to GamePackDir.xml directory path
				//				Path.Combine(directoryPath,packages.Element(XMLNamespace + "pathToXml").Value), 
				//				packages.Element(XMLNamespace + "description")?.Value,
				//				packages.Element(XMLNamespace + "thumbnailPath")?.Value,
				//				this);
			}
			catch (IOException e)
			{
				//Creation of the FileStream failed, cannot load this directory
				Log.Write(LogLevel.Warning, $"Opening ResroucePack directory file at {path} failed: {e}");
				if (Debugger.IsAttached) Debugger.Break();
			}
			//TODO: Exceptions
			catch (XmlSchemaValidationException e)
			{
				//Invalid resource pack description file, dont load this pack directory
				Log.Write(LogLevel.Warning, $"ResroucePack directory file at {path} does not conform to the schema: {e}");
				if (Debugger.IsAttached) Debugger.Break();
			}
			catch (XmlException e)
			{
				//TODO: Alert user for corrupt file
				Log.Write(LogLevel.Warning, $"ResourcePack directory file at {path} : {e}");
				if (Debugger.IsAttached) Debugger.Break();
			}

			//If loading failed completely, dont add anything
			if (presentPacks == null) return;

			//Adds all the discovered packs into the availablePacks list
			foreach (var presentPack in presentPacks) {
				availablePacks.Add(presentPack.Name, presentPack);
			}

		}


		void UnloadPackage(GamePack package) {
			resourceCache.RemoveResourceDir(package.XmlDirectoryPath);
			package.UnLoad();
		}

	}
}
