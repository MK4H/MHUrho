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

			protected readonly LevelRep Context;

			public string Name {
				get => Context.Name;
			}

			public string Description {
				get => Context.Description;
				set => Context.Description = value; 
			}

			public Texture2D Thumbnail {
				get => Context.Thumbnail;
				private set => Context.Thumbnail = value; 
			}

			public LevelLogicType LevelLogicType {
				get => Context.LevelLogicType;
				set => Context.LevelLogicType = value;
			}

			public GamePack GamePack {
				get => Context.GamePack;
				set => Context.GamePack = value; 
			}

			public IntVector2 MapSize => Context.MapSize;

			public string SavePath => Context.savePath;
			public string ThumbnailPath => Context.ThumbnailPath;

			protected LevelState(LevelRep context)
			{
				this.Context = context;
			}

			public abstract ILevelLoader LoadForEditing(ILoadingSignaler loadingSignaler);

			public abstract ILevelLoader LoadForPlaying(PlayerSpecification players, LevelLogicCustomSettings customSettings, ILoadingSignaler loadingSignaler);

			public abstract XElement SaveAsPrototype();

			public abstract LevelState CloneToNewContext(LevelRep newContext);

			public abstract void DetachFromRunningLevel();

			protected XElement ToXml()
			{
				return new XElement(LevelsXml.Inst.Level,
									new XAttribute(LevelXml.Inst.NameAttribute, Name),
									new XElement(LevelXml.Inst.Description, Description),
									new XElement(LevelXml.Inst.Thumbnail, ThumbnailPath),
									new XElement(LevelXml.Inst.LogicTypeName, LevelLogicType.Name),
									new XElement(LevelXml.Inst.DataPath, SavePath),
									XmlHelpers.IntVector2ToXmlElement(LevelXml.Inst.MapSize, MapSize));
			}
		}

		class CreatedLevel : LevelState {

			readonly IntVector2 mapSize;

			public CreatedLevel(IntVector2 mapSize, LevelRep context)
				:base(context)
			{
				this.mapSize = mapSize;
			}

			public override ILevelLoader LoadForEditing(ILoadingSignaler loadingSignaler)
			{
				var loader = LevelManager.GetLoader();
				loader.LoadDefaultLevel(Context, mapSize, loadingSignaler).ContinueWith(LevelLoaded, TaskContinuationOptions.OnlyOnRanToCompletion);
				return loader;
			}

			public override ILevelLoader LoadForPlaying(PlayerSpecification players, LevelLogicCustomSettings customSettings, ILoadingSignaler loadingSignaler)
			{
				throw new InvalidOperationException("Cannot play a default level");
			}

			public override XElement SaveAsPrototype()
			{
				throw new InvalidOperationException("Cannot save a freshly created level");
			}

			public override LevelState CloneToNewContext(LevelRep newContext)
			{
				throw new
					InvalidOperationException("Cannot clone freshly created level, load it, save it, then clone it");
			}

			public override void DetachFromRunningLevel()
			{
				throw new
					InvalidOperationException("Cannot detach, there is no running level");
			}

			void LevelLoaded(Task<ILevelManager> task)
			{
				//TODO: Check for exceptions
				//TODO: Maybe lock state
				Context.state = new EditingLevel(Context, task.Result);
			}

		}

		class LevelPrototype : LevelState {

			public LevelPrototype(LevelRep context)
				:base(context)
			{

			}

			public override ILevelLoader LoadForEditing(ILoadingSignaler loadingSignaler)
			{
				var savedLevel = GetSaveFromPackagePath(SavePath, GamePack);
				var loader = LevelManager.GetLoader();
				//TODO: Maybe add failure continuation
				loader.LoadForEditing(Context, savedLevel, loadingSignaler).ContinueWith(LoadedForEditing, TaskContinuationOptions.OnlyOnRanToCompletion);
				return loader;
			}

			public override ILevelLoader LoadForPlaying(PlayerSpecification players, LevelLogicCustomSettings customSettings, ILoadingSignaler loadingSignaler)
			{
				var savedLevel = GetSaveFromPackagePath(SavePath, GamePack);
				var loader = LevelManager.GetLoader();
				//TODO: Maybe add failure continuation
				loader.LoadForPlaying(Context, savedLevel, players, customSettings, loadingSignaler).ContinueWith(LoadedForPlaying, TaskContinuationOptions.OnlyOnRanToCompletion);
				return loader;
			}

			public override XElement SaveAsPrototype()
			{
				throw new
					InvalidOperationException("Cannot save level prototype again, try cloning it with a new name and then saving it");
			}

			public override LevelState CloneToNewContext(LevelRep newContext)
			{
				return new ClonedLevelPrototype(newContext, this);
			}

			public override void DetachFromRunningLevel()
			{
				throw new
					InvalidOperationException("Cannot detach, there is no running level");
			}

			void LoadedForPlaying(Task<ILevelManager> task)
			{
				//Dont have to check for exceptions, this is OnlyOnCompletion continuation
				//TODO: Maybe lock state
				Context.state = new PlayingLevel(Context, this, task.Result);
			}

			void LoadedForEditing(Task<ILevelManager> task)
			{
				//Dont have to check for exceptions, this is OnlyOnCompletion continuation
				//TODO: Maybe lock state
				Context.state = new EditingLevel(Context, task.Result);
			}
		}

		class EditingLevel : LevelState {

			public ILevelManager RunningLevel { get; protected set; }

			public EditingLevel(LevelRep context, ILevelManager runningLevel)
				:base(context)
			{
				this.RunningLevel = runningLevel;
				runningLevel.Ending += RunningLevelEnding;
			}

			

			public override ILevelLoader LoadForEditing(ILoadingSignaler loadingSignaler)
			{
				throw new NotImplementedException();
			}

			public override ILevelLoader LoadForPlaying(PlayerSpecification players,
														LevelLogicCustomSettings customSettings,
														ILoadingSignaler loadingSignaler)
			{
				throw new NotImplementedException();
			}

			public override XElement SaveAsPrototype()
			{
				if (RunningLevel == null)
				{
					throw new InvalidOperationException("Level was not loaded, so there is nothing to save");
				}

				Stream saveFile = null;
				try
				{
					var storedLevel = RunningLevel.Save();
					saveFile = MyGame.Files.OpenDynamicFileInPackage(SavePath, FileMode.Create, FileAccess.Write, GamePack);
					storedLevel.WriteTo(saveFile);
				}
				//TODO: Catch just the expected exceptions
				catch (Exception e)
				{
					Urho.IO.Log.Write(LogLevel.Error, $"Level saving to {SavePath} failed with: {e.Message}");
					throw;
				}
				finally
				{
					saveFile?.Dispose();
				}

				return ToXml();
			}

			public override LevelState CloneToNewContext(LevelRep newContext)
			{
				return new ClonedEditingLevel(newContext, RunningLevel);
			}

			public override void DetachFromRunningLevel()
			{
				RunningLevel.Ending -= RunningLevelEnding;
				Context.state = new LevelPrototype(Context);
			}

			void RunningLevelEnding()
			{
				Context.state = new LevelPrototype(Context);
			}
		}

		class PlayingLevel : LevelState {
			public ILevelManager RunningLevel { get; protected set; }

			readonly LevelState sourceState;

			public PlayingLevel(LevelRep context, LevelState sourceState, ILevelManager runningLevel)
				: base(context)
			{
				this.RunningLevel = runningLevel;
				this.sourceState = sourceState;
				runningLevel.Ending += RunningLevelEnding;
			}

			

			public override ILevelLoader LoadForEditing(ILoadingSignaler loadingSignaler)
			{
				throw new NotImplementedException();
			}

			public override ILevelLoader LoadForPlaying(PlayerSpecification players,
														LevelLogicCustomSettings customSettings,
														ILoadingSignaler loadingSignaler)
			{
				throw new NotImplementedException();
			}

			public override XElement SaveAsPrototype()
			{
				throw new InvalidOperationException("Cannot save level in play mode as prototype");
			}

			public override LevelState CloneToNewContext(LevelRep newContext)
			{
				throw new InvalidOperationException("Cannot clone level in play mode");
			}

			public override void DetachFromRunningLevel()
			{
				RunningLevel.Ending -= RunningLevelEnding;
				Context.state = sourceState;
			}

			void RunningLevelEnding()
			{
				Context.state = sourceState;
			}
		}

		/// <summary>
		/// Created when level prototype is loaded for editing
		/// If the prototype was loaded without changing the name, the old level will be overwritten on the first save
		/// If the prototype was loaded with new name, new level will be created
		/// </summary>
		class ClonedLevelPrototype : LevelState {

			LevelPrototype sourceState;

			public ClonedLevelPrototype(LevelRep context, LevelPrototype sourceState)
				: base(context)
			{
				this.sourceState = sourceState;
			}

			public override ILevelLoader LoadForEditing(ILoadingSignaler loadingSignaler)
			{
				throw new InvalidOperationException("Cloned level cannot be loaded before it is saved");
			}

			public override ILevelLoader LoadForPlaying(PlayerSpecification players, LevelLogicCustomSettings customSettings, ILoadingSignaler loadingSignaler)
			{
				throw new InvalidOperationException("Cloned level cannot be loaded before it is saved");
			}

			public override XElement SaveAsPrototype()
			{
				try
				{
					if (sourceState.SavePath != SavePath) {
						MyGame.Files.Copy(Path.Combine(GamePack.RootedDirectoryPath, sourceState.SavePath),
										Path.Combine(GamePack.RootedDirectoryPath, SavePath),
										true);
					}
				}
				//TODO: Catch just the expected exceptions
				catch (Exception e)
				{
					Urho.IO.Log.Write(LogLevel.Error, $"Level saving to {SavePath} failed with: {e.Message}");
					throw;
				}

				Context.state = new LevelPrototype(Context);
				return ToXml();
			}

			public override LevelState CloneToNewContext(LevelRep newContext)
			{
				return new ClonedLevelPrototype(newContext, sourceState);
			}

			public override void DetachFromRunningLevel()
			{
				throw new
					InvalidOperationException("Cannot detach, there is no running level");
			}
		}

		/// <summary>
		/// State of LevelRep after cloning editing level
		/// Both the original and this copy point to the same running level
		/// Acts only as a temporary clone, until it is saved or discarded
		/// After saving, changes to LevelPrototype
		/// </summary>
		class ClonedEditingLevel : EditingLevel {
			public ClonedEditingLevel(LevelRep context, ILevelManager runningLevel)
				: base(context, runningLevel)
			{ }

			public override XElement SaveAsPrototype()
			{
				XElement xml = base.SaveAsPrototype();
				RunningLevel = null;
				Context.state = new LevelPrototype(Context);
				return xml;
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

			public override ILevelLoader LoadForEditing(ILoadingSignaler loadingSignaler)
			{
				throw new InvalidOperationException("Level loaded from save cannot be edited");
			}

			public override ILevelLoader LoadForPlaying(PlayerSpecification players, LevelLogicCustomSettings customSettings, ILoadingSignaler loadingSignaler)
			{
				if (savedLevel == null) {
					savedLevel = GetSaveFromDynamicPath(savedLevelPath);
				}
				var loader = LevelManager.GetLoader();

				loader.LoadForPlaying(Context, savedLevel, players, customSettings, loadingSignaler).ContinueWith(LevelLoaded, TaskContinuationOptions.OnlyOnRanToCompletion);

				return loader;
			}

			public override XElement SaveAsPrototype()
			{
				throw new InvalidOperationException("Cannot save loaded saved level as a prototype");
			}

			public override LevelState CloneToNewContext(LevelRep newContext)
			{
				throw new InvalidOperationException("Cannot clone loaded saved level");
			}

			public override void DetachFromRunningLevel()
			{
				throw new
					InvalidOperationException("Cannot detach, there is no running level");
			}

			void LevelLoaded(Task<ILevelManager> loadingTask)
			{
				savedLevel = null;
				//TODO: Maybe lock context to synchronize
				Context.state = new PlayingLevel(Context, this, loadingTask.Result);
			}
		}

		public string Name { get; }

		public string Description { get; private set; }

		public Texture2D Thumbnail { get; private set; }

		public LevelLogicType LevelLogicType { get; private set; }

		public GamePack GamePack { get; private set; }

		public string ThumbnailPath { get; private set; }

		public IntVector2 MapSize { get; private set; }

		public int MaxNumberOfPlayers => LevelLogicType.MaxNumberOfPlayers;

		public int MinNumberOfPLayers => LevelLogicType.MinNumberOfPlayers;

		readonly string savePath;

		LevelState state;

		protected LevelRep(string name,
						string description,
						string thumbnailPath,
						LevelLogicType levelLogicType,
						IntVector2 mapSize,
						GamePack gamePack)
		{
			this.Name = name;
			this.Description = description;
			this.ThumbnailPath = thumbnailPath;
			this.LevelLogicType = levelLogicType;
			this.savePath = gamePack.GetLevelProtoSavePath(name);
			this.MapSize = mapSize;
			this.GamePack = gamePack;

			this.Thumbnail = PackageManager.Instance.GetTexture2D(thumbnailPath);

			state = new CreatedLevel(mapSize, this);
		}

		/// <summary>
		/// Creates clone with new name, description and thumbnail for the SaveAs call
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
			this.LevelLogicType = other.LevelLogicType;
			this.GamePack = other.GamePack;
			this.MapSize = other.MapSize;
			this.savePath = GamePack.GetLevelProtoSavePath(name);

			this.Thumbnail = PackageManager.Instance.GetTexture2D(thumbnailPath);


			state = other.state.CloneToNewContext(this);
		}

		protected LevelRep(GamePack gamePack, XElement levelXmlElement)
		{
			this.GamePack = gamePack;
			//TODO: Check for errors
			Name = XmlHelpers.GetName(levelXmlElement);
			Description = levelXmlElement.Element(LevelXml.Inst.Description).GetString();
			ThumbnailPath = XmlHelpers.GetPath(levelXmlElement.Element(LevelXml.Inst.Thumbnail));
			LevelLogicType = gamePack.GetLevelLogicType(levelXmlElement.Element(LevelXml.Inst.LogicTypeName).Value);
			savePath = XmlHelpers.GetPath(levelXmlElement.Element(LevelXml.Inst.DataPath));

			this.MapSize = XmlHelpers.GetIntVector2(levelXmlElement.Element(LevelXml.Inst.MapSize));

			this.Thumbnail = PackageManager.Instance.GetTexture2D(ThumbnailPath);

			state = new LevelPrototype(this);
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

			this.LevelLogicType = gamePack.GetLevelLogicType(storedLevel.Plugin.TypeID);

			this.state = new LoadedSavedLevel(this, storedLevelPath, storedLevel);
		}

		public static LevelRep CreateNewLevel(string name,
											string description,
											string thumbnailPath,
											LevelLogicType levelLogicType,
											IntVector2 mapSize,
											GamePack gamePack)
		{
			return new LevelRep(name, description, thumbnailPath, levelLogicType, mapSize, gamePack);
		}

		public static LevelRep GetFromLevelPrototype(GamePack gamePack, XElement levelXmlElement)
		{
			return new LevelRep(gamePack, levelXmlElement);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="storedLevelPath"></param>
		/// <param name="loadingSignaler"></param>
		/// <returns></returns>
		/// <exception cref="LevelLoadingException">Thrown when the loading of the saved level failed</exception>
		public static async Task<LevelRep> GetFromSavedGame(string storedLevelPath, ILoadingSignaler loadingSignaler = null)
		{

			loadingSignaler?.TextUpdate("Getting level from save file");
			StLevel storedLevel;
			try {
				storedLevel = GetSaveFromDynamicPath(storedLevelPath);
			}
			catch (Exception e) {
				string message = $"Deserialization of the saved level failed with:{Environment.NewLine}{e.Message}";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new LevelLoadingException(message, e);
			}

			loadingSignaler?.TextUpdate("Loading package");
			try {
				var gamePack =
					await PackageManager.Instance.LoadPackage(storedLevel.PackageName,
															loadingSignaler?.GetWatcherForSubsection());
				return new LevelRep(gamePack, storedLevelPath, storedLevel);
			}
			catch (ArgumentOutOfRangeException e) {
				string message =
					$"Package the saved level \"{storedLevel.LevelName}\" belonged to is no longer installed, please add the package \"{storedLevel.PackageName}\" before loading this level";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new LevelLoadingException(message, e);
			}
			catch (ArgumentNullException e) {
				string message = $"Saved file at \"{storedLevelPath}\" was corrupted";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new LevelLoadingException(message, e);
			}
			catch (PackageLoadingException e) {
				string message = $"Package loading for the level failed with: {Environment.NewLine}{e.Message}";
				throw new LevelLoadingException(message, e);
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

		public static bool operator ==(LevelRep left, LevelRep right)
		{
			return left?.Equals(right) ?? null == right;
		}

		public static bool operator !=(LevelRep left, LevelRep right)
		{
			return !(left == right);
		}

		public override bool Equals(object obj)
		{
			if (obj is LevelRep other) {
				return Name == other.Name;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}



		public ILevelLoader LoadForEditing(ILoadingSignaler loadingSignaler)
		{
			return state.LoadForEditing(loadingSignaler);
		}

		public ILevelLoader LoadForPlaying(PlayerSpecification players, LevelLogicCustomSettings customSettings, ILoadingSignaler loadingSignaler)
		{
			return state.LoadForPlaying(players, customSettings, loadingSignaler);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="overrideLevel"></param>
		/// <exception cref="InvalidOperationException"/>
		public void SaveToGamePack(bool overrideLevel)
		{
			GamePack.SaveLevelPrototype(this, overrideLevel);
		}

		public LevelRep CreateClone(string newName, string newDescription, string newThumbnailPath)
		{
			return new LevelRep(this, newName, newDescription, newThumbnailPath);
		}

		public XElement SaveAsPrototype()
		{
			return state.SaveAsPrototype();
		}

		public void DetachFromLevel()
		{
			state.DetachFromRunningLevel();
		}

		public void RemoveDataFile()
		{
			//TODO: Move this to state, maybe the file may not exist yet
			MyGame.Files.DeleteDynamicFile(Path.Combine(GamePack.DirectoryPath, savePath));
		}

		public void Dispose()
		{
			Thumbnail.Dispose();
			LevelLogicType.Dispose();
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

	
	}
}
