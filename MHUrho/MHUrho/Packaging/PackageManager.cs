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

		

		/// <summary>
		/// Path to the schema for Resource Pack Directory xml files
		/// </summary>
		private static readonly string GamePackageSchemaPath = Path.Combine("Data","Schemas","GamePack.xsd");


		public GamePack ActiveGame { get; private set; }

		readonly XmlSchemaSet schemas;

		readonly Dictionary<string, GamePack> availablePacks = new Dictionary<string, GamePack>();
		readonly ResourceCache resourceCache;


		protected PackageManager(ResourceCache resourceCache) {
			this.resourceCache = resourceCache;

			schemas = new XmlSchemaSet();
		}

		public static void CreateInstance(ResourceCache resourceCache) {
			Instance = new PackageManager(resourceCache);
			try {

				Instance.schemas.Add(XMLNamespace.NamespaceName, XmlReader.Create(MyGame.Config.OpenStaticFileRO(GamePackageSchemaPath)));
			}
			catch (IOException e) {
				Log.Write(LogLevel.Error, $"Error loading GamePack schema: {e}");
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

			resourceCache.AddResourceDir(ActiveGame.XmlDirectoryPath,1);

			ActiveGame.Load(schemas);
		}

		public GamePack GetGamePack(string name) {
			//TODO: React if it does not exist
			return availablePacks[name];
		}

		public bool Exists(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.Exists(name);
			}
			else {
				bool exists = false;
				Application.InvokeOnMainAsync(() => { exists = resourceCache.Exists(name); }).Wait();
				return exists;
			}
		}

		public Animation GetAnimation(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetAnimation(name);
			}
			else {
				Animation animation = null;
				Application.InvokeOnMainAsync(() => { animation = resourceCache.GetAnimation(name); }).Wait();
				return animation;
			}
		}

		public AnimationSet2D GetAnimationSet2D(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetAnimationSet2D(name);
			}
			else {
				AnimationSet2D animationSet2D = null;
				Application.InvokeOnMainAsync(() => { animationSet2D = resourceCache.GetAnimationSet2D(name); }).Wait();
				return animationSet2D;
			}
		}

		public Urho.IO.File GetFile(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetFile(name);
			}
			else {
				Urho.IO.File file = null;
				Application.InvokeOnMainAsync(() => { file = resourceCache.GetFile(name); }).Wait();
				return file;
			}
		}

		public Font GetFont(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetFont(name);
			}
			else {
				Font font = null;
				Application.InvokeOnMainAsync(() => { font = resourceCache.GetFont(name); }).Wait();
				return font;
			}
		}

		public Image GetImage(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetImage(name);
			}
			else {
				Image image = null;
				Application.InvokeOnMainAsync(() => { image = resourceCache.GetImage(name); }).Wait();
				return image;
			}
		}

		public JsonFile GetJsonFile(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetJsonFile(name);
			}
			else {
				JsonFile jsonFile = null;
				Application.InvokeOnMainAsync(() => { jsonFile = resourceCache.GetJsonFile(name); }).Wait();
				return jsonFile;
			}
		}

		public Material GetMaterial(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetMaterial(name);
			}
			else {
				Material material = null;
				Application.InvokeOnMainAsync(() => { material = resourceCache.GetMaterial(name); }).Wait();
				return material;
			}
		}

		public Model GetModel(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetModel(name);
			}
			else {
				Model model = null;
				Application.InvokeOnMainAsync(() => { model = resourceCache.GetModel(name); }).Wait();
				return model;
			}
		}

		public ParticleEffect GetParticleEffect(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetParticleEffect(name);
			}
			else {
				ParticleEffect particleEffect = null;
				Application.InvokeOnMainAsync(() => { particleEffect = resourceCache.GetParticleEffect(name); }).Wait();
				return particleEffect;
			}
		}

		public ParticleEffect2D GetParticleEffect2D(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetParticleEffect2D(name);
			}
			else {
				ParticleEffect2D particleEffect2D = null;
				Application.InvokeOnMainAsync(() => { particleEffect2D = resourceCache.GetParticleEffect2D(name); }).Wait();
				return particleEffect2D;
			}
		}

		public PListFile GetPListFile(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetPListFile(name);
			}
			else {
				PListFile pListFile = null;
				Application.InvokeOnMainAsync(() => { pListFile = resourceCache.GetPListFile(name); }).Wait();
				return pListFile;
			}
		}

		public Shader GetShader(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetShader(name);
			}
			else {
				Shader shader = null;
				Application.InvokeOnMainAsync(() => { shader = resourceCache.GetShader(name); }).Wait();
				return shader;
			}
		}

		public Sound GetSound(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetSound(name);
			}
			else {
				Sound sound = null;
				Application.InvokeOnMainAsync(() => { sound = resourceCache.GetSound(name); }).Wait();
				return sound;
			}
		}

		public Sprite2D GetSprite2D(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetSprite2D(name);
			}
			else {
				Sprite2D sprite2D = null;
				Application.InvokeOnMainAsync(() => { sprite2D = resourceCache.GetSprite2D(name); }).Wait();
				return sprite2D;
			}
		}

		public SpriteSheet2D GetSpriteSheet2D(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetSpriteSheet2D(name);
			}
			else {
				SpriteSheet2D spriteSheet2D = null;
				Application.InvokeOnMainAsync(() => { spriteSheet2D = resourceCache.GetSpriteSheet2D(name); }).Wait();
				return spriteSheet2D;
			}
		}

		public Technique GetTechnique(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetTechnique(name);
			}
			else {
				Technique technique = null;
				Application.InvokeOnMainAsync(() => { technique = resourceCache.GetTechnique(name); }).Wait();
				return technique;
			}
		}

		public Texture2D GetTexture2D(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetTexture2D(name);
			}
			else {
				Texture2D texture2D = null;
				Application.InvokeOnMainAsync(() => { texture2D = resourceCache.GetTexture2D(name); }).Wait();
				return texture2D;
			}
		}

		public Texture3D GetTexture3D(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetTexture3D(name);
			}
			else {
				Texture3D texture3D = null;
				Application.InvokeOnMainAsync(() => { texture3D = resourceCache.GetTexture3D(name); }).Wait();
				return texture3D;
			}
		}

		public Texture GetTextureCube(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetTextureCube(name);
			}
			else {
				Texture textureCube = null;
				Application.InvokeOnMainAsync(() => { textureCube = resourceCache.GetTextureCube(name); }).Wait();
				return textureCube;
			}
		}

		public TmxFile2D GetTmxFile2D(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetTmxFile2D(name);
			}
			else {
				TmxFile2D tmxFile2D = null;
				Application.InvokeOnMainAsync(() => { tmxFile2D = resourceCache.GetTmxFile2D(name); }).Wait();
				return tmxFile2D;
			}
		}

		public ValueAnimation GetValueAnimation(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetValueAnimation(name);
			}
			else {
				ValueAnimation valueAnimation = null;
				Application.InvokeOnMainAsync(() => { valueAnimation = resourceCache.GetValueAnimation(name); }).Wait();
				return valueAnimation;
			}
		}

		public XmlFile GetXmlFile(string name)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return resourceCache.GetXmlFile(name);
			}
			else {
				XmlFile xmlFile = null;
				Application.InvokeOnMainAsync(() => { xmlFile = resourceCache.GetXmlFile(name); }).Wait();
				return xmlFile;
			}
		}

		public Material GetMaterialFromImage(Image image)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return Material.FromImage(image);
			}
			else {
				Material material = null;
				Application.InvokeOnMainAsync(() => { material = Material.FromImage(image); }).Wait();
				return material;
			}
		}

		public Material GetMaterialFromImage(string image)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return Material.FromImage(image);
			}
			else {
				Material material = null;
				Application.InvokeOnMainAsync(() => { material = Material.FromImage(image); }).Wait();
				return material;
			}
		}

		public Material GetMaterialFromImage(string image, string normals)
		{
			if (IsMainThread(Thread.CurrentThread)) {
				return Material.FromImage(image, normals);
			}
			else {
				Material material = null;
				Application.InvokeOnMainAsync(() => { material = Material.FromImage(image, normals); }).Wait();
				return material;
			}
		}

		/// <summary>
		/// Pulls data about the resource packs contained in this directory from XML file
		/// </summary>
		/// <param name="path">Path to the XML file of Resource pack directory</param>
		/// <param name="schema">Schema for the resource pack directory type of XML files</param>
		/// <returns>True if successfuly read, False if there was an error while loading</returns>
		void ParseGamePackDir(string path)
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


		void UnloadPackage(GamePack package) {
			resourceCache.RemoveResourceDir(package.XmlDirectoryPath);
			package.UnLoad();
		}

		bool IsMainThread(Thread thread)
		{
			return MyGame.IsMainThread(thread);
		}
	}
}
