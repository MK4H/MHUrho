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
using Google.Protobuf;

namespace MHUrho.Packaging
{
    public class LevelRep : IDisposable {
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

			protected IntVector2 MapSize => Context.MapSize;

			protected string SavePath => Context.savePath;
			protected string ThumbnailPath => Context.ThumbnailPath;

			protected volatile ILevelManager RunningLevel;

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

				if (GamePack.TryGetLevel(Name, out LevelRep oldLevel)) {
					throw new InvalidOperationException("Level with the same name already existed in the package");
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

				levelsElement.Add(new XElement(LevelsXml.Inst.Level,
												new XAttribute(LevelXml.Inst.NameAttribute, Name),
												new XElement(LevelXml.Inst.Description, Description),
												new XElement(LevelXml.Inst.Thumbnail, ThumbnailPath),
												new XElement(LevelXml.Inst.AssemblyPath, LevelPluginAssemblyPath),
												new XElement(LevelXml.Inst.DataPath, SavePath),
												XmlHelpers.IntVector2ToXmlElement(LevelXml.Inst.MapSize, MapSize)));
			}

			public abstract LevelState CloneWith(LevelRep newContext);

			public void ClearRunningLevel()
			{
				RunningLevel = null;
			}

			protected virtual void RunningLevelLoaded(Task<ILevelManager> loadingTask)
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

			public override LevelState CloneWith(LevelRep newContext)
			{
				return new CreatedLevel(mapSize, newContext) {RunningLevel = this.RunningLevel};
			}
		}

		class LoadedLevelPrototype : LevelState {

			public LoadedLevelPrototype(LevelRep context)
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

			public override LevelState CloneWith(LevelRep newContext)
			{
				return new LoadedLevelPrototype(newContext) {RunningLevel = this.RunningLevel};
			}
		}

		class LoadedSavedLevel : LevelState {

			readonly string savedLevelPath;
			StLevel savedLevel;

			public LoadedSavedLevel(LevelRep context, string savedLevelPath, StLevel savedLevel)
				: base(context)
			{
				this.savedLevelPath = savedLevelPath;
				this.savedLevel = savedLevel;
			}

			public override ILevelLoader LoadForEditing()
			{
				throw new InvalidOperationException("Level loaded from save cannot be edited");
			}

			public override ILevelLoader LoadForPlaying(PlayerSpecification players)
			{
				if (savedLevel == null) {
					savedLevel = GetSaveFromDynamicPath(savedLevelPath);
				}
				var loader = LevelManager.GetLoader();

				loader.LoadForPlaying(Context, savedLevel, players).ContinueWith(RunningLevelLoaded);

				return loader;
			}

			public override LevelState CloneWith(LevelRep newContext)
			{
				return new LoadedSavedLevel(newContext, savedLevelPath, savedLevel);
			}

			protected override void RunningLevelLoaded(Task<ILevelManager> loadingTask)
			{
				base.RunningLevelLoaded(loadingTask);
				savedLevel = null;
			}
		}

		public string Name { get; private set; }

		public string Description { get; private set; }

		public Texture2D Thumbnail { get; private set; }

		public LevelLogicPlugin LevelPlugin { get; private set; }

		public GamePack GamePack { get; private set; }

		public string LevelPluginAssemblyPath { get; private set; }

		public string ThumbnailPath { get; private set; }

		public IntVector2 MapSize { get; private set; }

		public int MaxNumberOfPlayers => LevelPlugin.NumberOfPlayers;

		readonly string savePath;

		readonly LevelState state;

		protected LevelRep(string name,
						string description,
						string thumbnailPath,
						string levelPluginAssemblyPath,
						IntVector2 mapSize,
						GamePack gamePack)
		{
			this.Name = name;
			this.Description = description;
			this.ThumbnailPath = thumbnailPath;
			this.LevelPluginAssemblyPath = levelPluginAssemblyPath;
			this.savePath = gamePack.GetLevelProtoSavePath(name);
			this.MapSize = mapSize;
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
			this.ThumbnailPath = thumbnailPath;
			this.LevelPluginAssemblyPath = other.LevelPluginAssemblyPath;
			this.GamePack = other.GamePack;
			this.MapSize = other.MapSize;
			this.savePath = GamePack.GetLevelProtoSavePath(name);

			this.Thumbnail = PackageManager.Instance.GetTexture2D(thumbnailPath);
			this.LevelPlugin = LoadLogicPlugin(LevelPluginAssemblyPath);


			state = other.state.CloneWith(this);
		}

		protected LevelRep(GamePack gamePack, XElement levelXmlElement)
		{
			this.GamePack = gamePack;
			//TODO: Check for errors
			Name = XmlHelpers.GetName(levelXmlElement);
			Description = levelXmlElement.Element(LevelXml.Inst.Description).GetString();
			ThumbnailPath = XmlHelpers.GetPath(levelXmlElement.Element(LevelXml.Inst.Thumbnail));			
			LevelPluginAssemblyPath = FileManager.CorrectRelativePath(levelXmlElement.Element(LevelXml.Inst.AssemblyPath).GetString());
			savePath = XmlHelpers.GetPath(levelXmlElement.Element(LevelXml.Inst.DataPath));

			this.MapSize = XmlHelpers.GetIntVector2(levelXmlElement.Element(LevelXml.Inst.MapSize));

			this.Thumbnail = PackageManager.Instance.GetTexture2D(ThumbnailPath);
			this.LevelPlugin = LoadLogicPlugin(LevelPluginAssemblyPath);

			state = new LoadedLevelPrototype(this);
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
			this.Description = "Temporary level loaded from a saved game";
			//TODO: Default thumbnail
			//this.Thumbnail = 

			this.MapSize = storedLevel.Map.Size.ToIntVector2();

			this.LevelPluginAssemblyPath = storedLevel.Plugin.AssemblyPath;
			LevelPlugin = LoadLogicPlugin(LevelPluginAssemblyPath);

			state = new LoadedSavedLevel(this, storedLevelPath, storedLevel);
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
			return new LevelRep(gamePack, storedLevelPath, storedLevel);
			
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="overrideLevel"></param>
		/// <exception cref="InvalidOperationException"/>
		public void SaveToGamePack(bool overrideLevel)
		{
			GamePack.SaveLevel(this, overrideLevel);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newName"></param>
		/// <param name="newDescription"></param>
		/// <param name="newThumbnailPath"></param>
		/// <param name="overrideLevel"></param>
		/// <exception cref="InvalidOperationException"/>
		public void SaveToGamePackAs(string newName, string newDescription, string newThumbnailPath, bool overrideLevel)
		{
			LevelRep clone = new LevelRep(this, newName, newDescription, newThumbnailPath);
			clone.SaveToGamePack(overrideLevel);
			clone.state.ClearRunningLevel();
		}

		public void SaveTo(XElement levelsElement)
		{
			state.SaveTo(levelsElement);
		}

		public void LevelEnded()
		{
			state.ClearRunningLevel();
		}

		public void RemoveDataFile()
		{
			//TODO: Move this to state, maybe the file may not exist yet
			MyGame.Files.DeleteDynamicFile(Path.Combine(GamePack.DirectoryPath, savePath));
		}

		public void Dispose()
		{
			Thumbnail.Dispose();
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


	
	}
}
