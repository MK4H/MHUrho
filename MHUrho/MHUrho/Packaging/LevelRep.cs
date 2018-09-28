using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;
using Urho.Urho2D;

namespace MHUrho.Packaging
{
    public class LevelRep : IDisposable {
		static readonly XName DescriptionElement = PackageManager.XMLNamespace + "description";
		static readonly XName ThumbnailElement = PackageManager.XMLNamespace + "thumbnail";
		static readonly XName DataPathElement = PackageManager.XMLNamespace + "dataPath";
		static readonly XName AssemblyPathElement = PackageManager.XMLNamespace + "assemblyPath";

		abstract class LevelState {

			protected LevelRep Context;

			protected string Name {
				get => Context.Name;
				set => Context.Name = value;
			}

			protected string Description {
				get => Context.Description;
				set => Context.Description = value; 
			}


			protected Texture2D Thumbnail {
				get => Context.Thumbnail;
				private set => Context.Thumbnail = value; 
			}


			protected LevelLogicPlugin LevelPlugin {
				get => Context.LevelPlugin;
				set => Context.LevelPlugin = value; 
			}


			protected GamePack GamePack {
				get => Context.GamePack;
				set => Context.GamePack = value; 
			}


			protected string LevelPluginAssemblyPath {
				get => Context.LevelPluginAssemblyPath;
				set => Context.LevelPluginAssemblyPath = value; 
			}


			protected string SavePath => Context.savePath;
			protected string ThumbnailPath => Context.thumbnailPath;

			volatile protected ILevelManager RunningLevel;

			protected LevelState(LevelRep context)
			{
				this.Context = context;
			}

			public abstract ILevelLoader LoadForEditing();

			public abstract ILevelLoader LoadForPlaying(PlayerSpecification players);

			public virtual void SaveTo(XElement levelsElement)
			{
				if (RunningLevel == null) {
					throw new InvalidOperationException("Level was not loaded, so there is nothing to save");
				}


				foreach (var level in levelsElement.Elements(GamePackXml.Level)) {
					if (level.Attribute("name").Value == Name) {
						//TODO: Ask for override
						level.Remove();
					}
				}

				Stream saveFile = null;
				try {
					saveFile = MyGame.Files.OpenDynamicFileInPackage(SavePath, FileMode.Create, FileAccess.Write, GamePack);
					RunningLevel.SaveTo(saveFile);
				}
				//TODO: Catch just the expected exceptions
				catch (Exception e) {
					//TODO: Display message that saving failed
					throw;
				}
				finally {
					saveFile?.Dispose();
				}

				levelsElement.Add(new XElement(GamePackXml.Level,
												new XAttribute("name", Name),
												new XElement(DescriptionElement, Description),
												new XElement(ThumbnailElement, ThumbnailPath),
												new XElement(AssemblyPathElement, LevelPluginAssemblyPath),
												new XElement(DataPathElement, SavePath)));
			}


			protected void RunningLevelLoaded(Task<ILevelManager> loadingTask)
			{
				//TODO: Check status, react to loading failure
				RunningLevel = loadingTask.Result;
			}
		}

		class CreatedLevel : LevelState {

			readonly IntVector2 mapSize;

			public CreatedLevel(IntVector2 mapSize, LevelRep context)
				:base(context)
			{
				this.mapSize = mapSize;
			}

			public override ILevelLoader LoadForEditing()
			{
				var loader = LevelManager.GetLoader();
				loader.LoadDefaultLevel(Context, mapSize).ContinueWith(RunningLevelLoaded);
				return loader;
			}

			public override ILevelLoader LoadForPlaying(PlayerSpecification players)
			{
				throw new InvalidOperationException("Cannot play a default level");
			}
		}

		class LoadedLevel : LevelState {

			public LoadedLevel(LevelRep context)
				:base(context)
			{

			}

			public override ILevelLoader LoadForEditing()
			{
				var savedLevel = GetSaveFromPackagePath(SavePath, GamePack);
				var loader = LevelManager.GetLoader();
				//TODO: Maybe add failure continuation
				loader.LoadForEditing(Context, savedLevel).ContinueWith(RunningLevelLoaded);
				return loader;
			}

			public override ILevelLoader LoadForPlaying(PlayerSpecification players)
			{
				var savedLevel = GetSaveFromPackagePath(SavePath, GamePack);
				var loader = LevelManager.GetLoader();
				//TODO: Maybe add failure continuation
				loader.LoadForPlaying(Context, savedLevel, players).ContinueWith(RunningLevelLoaded);
				return loader;
			}
		}

		public string Name { get; private set; }

		public string Description { get; private set; }

		public Texture2D Thumbnail { get; private set; }

		public LevelLogicPlugin LevelPlugin { get; private set; }

		public GamePack GamePack { get; private set; }

		public string LevelPluginAssemblyPath { get; private set; }

		public int MaxNumberOfPlayers => LevelPlugin.NumberOfPlayers;

		readonly string savePath;
		readonly string thumbnailPath;

		LevelState state;

		protected LevelRep(string name,
						string description,
						string thumbnailPath,
						string levelPluginAssemblyPath,
						IntVector2 mapSize,
						GamePack gamePack)
		{
			this.Name = name;
			this.Description = description;
			this.thumbnailPath = thumbnailPath;
			this.LevelPluginAssemblyPath = levelPluginAssemblyPath;
			this.savePath = gamePack.GetLevelProtoSavePath(name);
			this.GamePack = gamePack;

			this.Thumbnail = PackageManager.Instance.GetTexture2D(thumbnailPath);
			this.LevelPlugin = LoadLogicPlugin(LevelPluginAssemblyPath);

			state = new CreatedLevel(mapSize, this);
		}

		/// <summary>
		/// Creates temporary clone with new name, description and thumbnail for the SaveAs call
		/// </summary>
		/// <param name="other"></param>
		/// <param name="name"></param>
		/// <param name="description"></param>
		/// <param name="thumbnailPath"></param>
		protected LevelRep(LevelRep other,
							string name,
							string description,
							string thumbnailPath)
		{
			this.Name = name;
			this.Description = description;
			this.thumbnailPath = thumbnailPath;
			this.LevelPluginAssemblyPath = other.LevelPluginAssemblyPath;
			this.GamePack = other.GamePack;
			this.savePath = GamePack.GetLevelProtoSavePath(name);

			this.Thumbnail = PackageManager.Instance.GetTexture2D(thumbnailPath);
			this.LevelPlugin = other.LevelPlugin;

			state = other.state;
		}

		protected LevelRep(GamePack gamePack, XElement levelXmlElement)
		{
			this.GamePack = gamePack;
			//TODO: Check for errors
			Name = XmlHelpers.GetName(levelXmlElement);
			Description = levelXmlElement.Element(DescriptionElement).GetString();
			thumbnailPath = XmlHelpers.GetPath(levelXmlElement.Element(ThumbnailElement));			
			LevelPluginAssemblyPath = FileManager.CorrectRelativePath(levelXmlElement.Element(AssemblyPathElement).GetString());
			savePath = XmlHelpers.GetPath(levelXmlElement.Element(DataPathElement));

			this.Thumbnail = PackageManager.Instance.GetTexture2D(thumbnailPath);
			this.LevelPlugin = LoadLogicPlugin(LevelPluginAssemblyPath);

			state = new LoadedLevel(this);
		}

		/// <summary>
		/// Representation for temporary level, where the proper LevelRep was deleted from the package,
		/// but a save from that level still exists
		/// </summary>
		/// <param name="gamePack"></param>
		/// <param name="storedLevelPath"></param>
		/// <param name="storedLevel"></param>
		protected LevelRep(GamePack gamePack, string storedLevelPath, StLevel storedLevel)
		{
			this.GamePack = gamePack;
			this.savePath = storedLevelPath;

			this.Name = storedLevel.LevelName;
			this.Description = "Temporary level";
			//TODO: Default thumbnail
			//this.Thumbnail = 

			this.LevelPluginAssemblyPath = storedLevel.Plugin.AssemblyPath;
			LevelPlugin = LoadLogicPlugin(LevelPluginAssemblyPath);

			//TODO: Maybe singletons
			state = new LoadedLevel(this);
		}

		public static LevelRep CreateNewLevel(string name,
											string description,
											string thumbnailPath,
											string levelPluginAssemblyPath,
											IntVector2 mapSize,
											GamePack gamePack)
		{
			return new LevelRep(name, description, thumbnailPath, levelPluginAssemblyPath, mapSize, gamePack);
		}

		public static LevelRep GetFromLevelPrototype(GamePack gamePack, XElement levelXmlElement)
		{
			return new LevelRep(gamePack, levelXmlElement);
		}

		public static LevelRep GetFromSavedGame(string storedLevelPath)
		{

			StLevel storedLevel = GetSaveFromDynamicPath(storedLevelPath);

			//TODO: Exception if pack is not present
			var gamePack = PackageManager.Instance.LoadPackage(storedLevel.PackageName);
			if (gamePack.TryGetLevel(storedLevel.LevelName, out LevelRep value)) {
				return value;
			}
			else {
				return new LevelRep(gamePack, storedLevelPath, storedLevel);
			}
		}

		public static bool IsNameValid(string name)
		{
			foreach (var ch in name)
			{
				if (!char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch))
				{
					return false;
				}
			}
			return true;
		}

		public static bool IsDescriptionValid(string description)
		{
			foreach (var ch in description)
			{
				if (!char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch) && !char.IsPunctuation(ch))
				{
					return false;
				}
			}
			return true;
		}

		public ILevelLoader LoadForEditing()
		{
			return state.LoadForEditing();
		}

		public ILevelLoader LoadForPlaying(PlayerSpecification players)
		{
			return state.LoadForPlaying(players);
		}

		public void SaveToGamePack()
		{
			GamePack.SaveLevel(this);
		}

		public void SaveToGamePackAs(string newName, string newDescription, string newThumbnailPath)
		{
			LevelRep clone = new LevelRep(this, newName, newDescription, newThumbnailPath);
			clone.SaveToGamePack();
			clone.CloneDispose();
		}

		public void SaveTo(XElement levelsElement)
		{
			state.SaveTo(levelsElement);
		}

		public void Dispose()
		{
			CloneDispose();
			LevelPlugin.Dispose();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path">Relative path based in <see cref="GamePack.DirectoryPath"/> of the owning gamePack</param>
		/// <param name="package">Package from which the path is based</param>
		/// <returns></returns>
		static StLevel GetSaveFromPackagePath(string path, GamePack package)
		{
			StLevel storedLevel = null;
			Stream saveFile = null;
			try {
				saveFile = MyGame.Files.OpenDynamicFileInPackage(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, package);
				storedLevel = StLevel.Parser.ParseFrom(saveFile);
			}
			//TODO: Exceptions from opening stream and from decoding level
			finally {
				saveFile?.Dispose();
			}

			return storedLevel;
		}

		static StLevel GetSaveFromDynamicPath(string path)
		{
			StLevel storedLevel = null;
			Stream saveFile = null;
			try
			{
				saveFile = MyGame.Files.OpenDynamicFile(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				storedLevel = StLevel.Parser.ParseFrom(saveFile);
			}
			//TODO: Exceptions from opening stream and from decoding level
			finally
			{
				saveFile?.Dispose();
			}

			return storedLevel;
		}

		LevelLogicPlugin LoadLogicPlugin(string levelPluginAssemblyPath)
		{
			string levelPluginPath = Path.Combine(GamePack.RootedDirectoryPath,
												levelPluginAssemblyPath);

			return LevelLogicPlugin.Load(levelPluginPath, Name);
		}

		void CloneDispose()
		{
			Thumbnail.Dispose();
		}

	
	}
}
