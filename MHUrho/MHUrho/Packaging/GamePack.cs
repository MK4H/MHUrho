using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using MHUrho.Threading;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Plugins;
using Urho;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.Packaging {

	/// <summary>
	/// Class representing loaded game package.
	/// Contains all loaded types, textures etc.
	/// Implements the loading of package into the running game.
	/// Provides API to get references to types, either by ID or by name.
	/// </summary>
	public class GamePack : IDisposable {
		public GamePackRep GamePackRep { get; private set; }

		public string Name => GamePackRep.Name;

		public MHUrhoApp App => PackageManager.App;

		public PackageManager PackageManager => GamePackRep.PackageManager;

		/// <summary>
		/// Path to the base directory of this package
		///
		/// This is the directory where the package xml file is.
		/// </summary>
		public string DirectoryPath => GamePackRep.XmlDirectoryPath;

		/// <summary>
		/// See <see cref="DirectoryPath"/>
		/// </summary>
		public string RootedDirectoryPath => Path.Combine(App.Files.DynamicDirPath, DirectoryPath);

		public TileType DefaultTileType { get; private set; }

		public int TileTypeCount => tileTypesByName.Count;

		public IEnumerable<TileType> TileTypes => tileTypesByName.Values;

		public int UnitTypeCount => unitTypesByName.Count;

		public IEnumerable<UnitType> UnitTypes => unitTypesByName.Values;

		public int BuildingTypeCount => buildingTypesByName.Count;

		public IEnumerable<BuildingType> BuildingTypes => buildingTypesByName.Values;

		public int ProjectileTypeCount => projectileTypesByName.Count;

		public IEnumerable<ProjectileType> ProjectileTypes => projectileTypesByName.Values;

		public int ResourceTypeCount => resourceTypesByName.Count;

		public IEnumerable<ResourceType> ResourceTypes => resourceTypesByName.Values;

		public int PlayerAITypesCount => playerAITypesByName.Count;

		public IEnumerable<PlayerType> PlayerTypes => playerAITypesByName.Values;

		public int LevelCount => levelsByName.Count;

		public IEnumerable<LevelRep> Levels => levelsByName.Values;

		public int LevelLogicTypeCount => levelLogicTypesByName.Count;

		public IEnumerable<LevelLogicType> LevelLogicTypes => levelLogicTypesByName.Values;

		public IEnumerable<PlayerType> HumanPlayerTypes => GetPlayersWithTypeCategory(PlayerTypeCategory.Human);

		public IEnumerable<PlayerType> NeutralPlayerTypes => GetPlayersWithTypeCategory(PlayerTypeCategory.Neutral);

		public IEnumerable<PlayerType> AIPlayerTypes => GetPlayersWithTypeCategory(PlayerTypeCategory.AI);

		public Texture2D ResourceIconTexture { get; private set; }
		public Texture2D TileIconTexture { get; private set; }
		public Texture2D UnitIconTexture { get; private set; }
		public Texture2D BuildingIconTexture { get; private set; }
		public Texture2D PlayerIconTexture { get; private set; }
		public Texture2D ToolIconTexture { get; private set; }
	   
		readonly Dictionary<string, TileType> tileTypesByName;
		readonly Dictionary<string, UnitType> unitTypesByName;
		readonly Dictionary<string, BuildingType> buildingTypesByName;
		readonly Dictionary<string, ProjectileType> projectileTypesByName;
		readonly Dictionary<string, ResourceType> resourceTypesByName;
		readonly Dictionary<string, PlayerType> playerAITypesByName;
		readonly Dictionary<string, LevelLogicType> levelLogicTypesByName;
		readonly Dictionary<string, LevelRep> levelsByName;

		readonly Dictionary<int, TileType> tileTypesByID;
		readonly Dictionary<int, UnitType> unitTypesByID;
		readonly Dictionary<int, BuildingType> buildingTypesByID;
		readonly Dictionary<int, ProjectileType> projectileTypesByID;
		readonly Dictionary<int, ResourceType> resourceTypesByID;
		readonly Dictionary<int, PlayerType> playerAITypesByID;
		readonly Dictionary<int, LevelLogicType> levelLogicTypesByID;

		readonly string pathToXml;
		readonly XmlSchemaSet schemas;

		/// <summary>
		/// Used during loading to enable Type plugins requesting yet unloaded types to load them on request
		/// Circular loading is prevented by spliting it between instance creation, which does not need any types,
		/// and plugin loading, which may request other types, but the instance is already loaded, so others can request the type
		/// </summary>
		XDocument data;

		/// <summary>
		/// Directory path to save level prototypes in
		///
		/// Relative to the <see cref="DirectoryPath"/> directory
		/// </summary>
		string levelSavingDirPath;

		GamePack(string pathToXml,
					GamePackRep gamePackRep,
					XmlSchemaSet schemas) {

			tileTypesByName = new Dictionary<string, TileType>();
			unitTypesByName = new Dictionary<string, UnitType>();
			buildingTypesByName = new Dictionary<string, BuildingType>();
			projectileTypesByName = new Dictionary<string, ProjectileType>();
			resourceTypesByName = new Dictionary<string, ResourceType>();
			playerAITypesByName = new Dictionary<string, PlayerType>();
			levelLogicTypesByName = new Dictionary<string, LevelLogicType>();
			levelsByName = new Dictionary<string, LevelRep>();

			tileTypesByID = new Dictionary<int, TileType>();
			unitTypesByID = new Dictionary<int, UnitType>();
			buildingTypesByID = new Dictionary<int, BuildingType>();
			projectileTypesByID = new Dictionary<int, ProjectileType>();
			resourceTypesByID = new Dictionary<int, ResourceType>();
			playerAITypesByID = new Dictionary<int, PlayerType>();
			levelLogicTypesByID = new Dictionary<int, LevelLogicType>();

			this.GamePackRep = gamePackRep;
			this.schemas = schemas;
			this.pathToXml = pathToXml;

			

		}

		/// <summary>
		/// Starts a task loading new game pack from XML file at <paramref name="pathToXml"/>,
		/// represented by <paramref name="gamePackRep"/>.
		/// You can watch the progress of loading by providing <paramref name="loadingProgress"/>, which will be updated from 0 to 100%.
		/// </summary>
		/// <param name="pathToXml">Path to the XML file describing the Game pack</param>
		/// <param name="gamePackRep">Representant of the GamePack</param>
		/// <param name="schemas">Schemas to validate the loaded XML file with.</param>
		/// <param name="loadingProgress">Optional loading progress watcher, which will be updated from 0 to 100%.</param>
		/// <returns>A task representing the loading of the gamePack</returns>
		/// <exception cref="PackageLoadingException">Thrown when the package loading failed</exception>
		public static async Task<GamePack> Load(string pathToXml,
												GamePackRep gamePackRep,
												XmlSchemaSet schemas,
												IProgressEventWatcher loadingProgress = null)
		{
			//Relative loading time of the parts, should add up to 100. Used to send accurate percentage update.
			const double tileTypesPartSize = 12.5;
			const double unitTypesPartSize = 12.5;
			const double buildingTypesPartSize = 12.5;
			const double projectileTypesPartSize = 12.5;
			const double resourceTypesPartSize = 12.5;
			const double playerTypesPartSize = 12.5;
			const double levelLogicTypesPartSize = 12.5;
			const double levelsPartSize = 12.5;

			GamePack newPack = new GamePack(pathToXml, gamePackRep, schemas);

			pathToXml = FileManager.ReplaceDirectorySeparators(pathToXml);

			try {
				newPack.data = await MHUrhoApp.InvokeOnMainSafeAsync(() => newPack.LoadXml(pathToXml, schemas));

				//Validation in StartLoading should take care of any missing elements
				newPack.levelSavingDirPath = FileManager.ReplaceDirectorySeparators(newPack.data.Root
																					.Element(GamePackXml.Inst.Levels)
																					.Element(LevelsXml.Inst.DataDirPath)
																					.Value.Trim());

				loadingProgress?.SendTextUpdate("Loading tile types");
				await MHUrhoApp.InvokeOnMainSafeAsync(newPack.LoadAllTileTypes);
				loadingProgress?.SendUpdate(tileTypesPartSize,"Loaded tile types");

				loadingProgress?.SendTextUpdate("Loading unit types");
				await MHUrhoApp.InvokeOnMainSafeAsync(newPack.LoadAllUnitTypes);
				loadingProgress?.SendUpdate(unitTypesPartSize, "Loaded unit types");

				loadingProgress?.SendTextUpdate("Loading building types");
				await MHUrhoApp.InvokeOnMainSafeAsync(newPack.LoadAllBuildingTypes);
				loadingProgress?.SendUpdate(buildingTypesPartSize, "Loaded building types");

				loadingProgress?.SendTextUpdate("Loading projectile types");
				await MHUrhoApp.InvokeOnMainSafeAsync(newPack.LoadAllProjectileTypes);
				loadingProgress?.SendUpdate(projectileTypesPartSize, "Loaded projectile types");

				loadingProgress?.SendTextUpdate("Loading resource types");
				await MHUrhoApp.InvokeOnMainSafeAsync(newPack.LoadAllResourceTypes);
				loadingProgress?.SendUpdate(resourceTypesPartSize, "Loaded resource types");

				loadingProgress?.SendTextUpdate("Loading player types");
				await MHUrhoApp.InvokeOnMainSafeAsync(newPack.LoadAllPlayerTypes);
				loadingProgress?.SendUpdate(playerTypesPartSize, "Loaded player types");

				loadingProgress?.SendTextUpdate("Loading level logic types");
				await MHUrhoApp.InvokeOnMainSafeAsync(newPack.LoadAllLevelLogicTypes);
				loadingProgress?.SendUpdate(levelLogicTypesPartSize, "Loaded level logic types");

				loadingProgress?.SendTextUpdate("Loading icon textures");
				await MHUrhoApp.InvokeOnMainSafeAsync(newPack.LoadIconTextures);
				loadingProgress?.SendUpdate(playerTypesPartSize, "Loaded icon textures");

				loadingProgress?.SendTextUpdate("Loading levels");
				await MHUrhoApp.InvokeOnMainSafeAsync(newPack.LoadAllLevels);
				loadingProgress?.SendUpdate(levelsPartSize, "Loaded levels");
			}
			catch (MethodInvocationException e)
			{
				//TODO: Dispose on error
				string message = $"Package loading failed with: \"{e.InnerException?.Message ?? ""}\"";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message, e);
			}
			catch (Exception e) {
				//TODO: Dispose on error
				string message = $"Package loading failed with: \"{e.Message}\"";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message, e);
			}			
			finally
			{
				newPack.ReleaseXml();
			}

			loadingProgress?.SendFinished();
			return newPack;
		}

		/// <summary>
		/// Returns the <see cref="TileType"/> with the given <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of the wanted <see cref="TileType"/></param>
		/// <returns>Returns the <see cref="TileType"/> with the given <paramref name="name"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when argument is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type of that name is not present in this package</exception>
		public TileType GetTileType(string name) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name),"Name of the tileType cannot be null");
			}

			if (tileTypesByName.TryGetValue(name, out TileType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(name, GamePackXml.Inst.TileTypes, TileTypesXml.Inst.TileType, tileTypesByName, tileTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown tile type");
		}

		/// <summary>
		/// Returns the <see cref="TileType"/> with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">ID of the wanted <see cref="TileType"/></param>
		/// <returns>Returns the <see cref="TileType"/> with the given <paramref name="ID"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type with the given <paramref name="ID"/> is not present in this package</exception>
		public TileType GetTileType(int ID) {

			if (tileTypesByID.TryGetValue(ID, out TileType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID, GamePackXml.Inst.TileTypes, TileTypesXml.Inst.TileType, tileTypesByName, tileTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown tile type");
		}

		/// <summary>
		/// Returns the <see cref="UnitType"/> with the given <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of the wanted <see cref="UnitType"/></param>
		/// <returns>Returns the <see cref="UnitType"/> with the given <paramref name="name"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type of that name is not present in this package</exception>
		public UnitType GetUnitType(string name) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the unitType cannot be null");
			}

			if (unitTypesByName.TryGetValue(name, out  UnitType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(name, GamePackXml.Inst.UnitTypes, UnitTypesXml.Inst.UnitType, unitTypesByName, unitTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown unit type");

		}

		/// <summary>
		/// Returns the <see cref="UnitType"/> with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">ID of the wanted <see cref="UnitType"/></param>
		/// <returns>Returns the <see cref="UnitType"/> with the given <paramref name="ID"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type with the given <paramref name="ID"/> is not present in this package</exception>
		public UnitType GetUnitType(int ID) {

			if (unitTypesByID.TryGetValue(ID, out  UnitType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID, GamePackXml.Inst.UnitTypes, UnitTypesXml.Inst.UnitType, unitTypesByName, unitTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown unit type");

		}

		/// <summary>
		/// Returns the <see cref="BuildingType"/> with the given <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of the wanted <see cref="BuildingType"/></param>
		/// <returns>Returns the <see cref="BuildingType"/> with the given <paramref name="name"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type of that name is not present in this package</exception>
		public BuildingType GetBuildingType(string name) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the buildingType cannot be null");
			}

			if (buildingTypesByName.TryGetValue(name, out BuildingType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(name,
								GamePackXml.Inst.BuildingTypes,
								BuildingTypesXml.Inst.BuildingType,
								buildingTypesByName,
								buildingTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown building type");
		}

		/// <summary>
		/// Returns the <see cref="BuildingType"/> with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">ID of the wanted <see cref="BuildingType"/></param>
		/// <returns>Returns the <see cref="BuildingType"/> with the given <paramref name="ID"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type with the given <paramref name="ID"/> is not present in this package</exception>
		public BuildingType GetBuildingType(int ID) {

			if (buildingTypesByID.TryGetValue(ID, out BuildingType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID,
								GamePackXml.Inst.BuildingTypes,
								BuildingTypesXml.Inst.BuildingType,
								buildingTypesByName,
								buildingTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown building type");
		}

		/// <summary>
		/// Returns the <see cref="ProjectileType"/> with the given <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of the wanted <see cref="ProjectileType"/></param>
		/// <returns>Returns the <see cref="ProjectileType"/> with the given <paramref name="name"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type of that name is not present in this package</exception>
		public ProjectileType GetProjectileType(string name) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the projectileType cannot be null");
			}

			if (projectileTypesByName.TryGetValue(name, out ProjectileType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(name,
								GamePackXml.Inst.ProjectileTypes,
								ProjectileTypesXml.Inst.ProjectileType,
								projectileTypesByName,
								projectileTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown projectile type");
		}

		/// <summary>
		/// Returns the <see cref="ProjectileType"/> with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">ID of the wanted <see cref="ProjectileType"/></param>
		/// <returns>Returns the <see cref="ProjectileType"/> with the given <paramref name="ID"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type with the given <paramref name="ID"/> is not present in this package</exception>
		public ProjectileType GetProjectileType(int ID) {

			if (projectileTypesByID.TryGetValue(ID, out ProjectileType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID,
								GamePackXml.Inst.ProjectileTypes,
								ProjectileTypesXml.Inst.ProjectileType,
								projectileTypesByName,
								projectileTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown projectile type");
		}

		/// <summary>
		/// Returns the <see cref="ResourceType"/> with the given <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of the wanted <see cref="ResourceType"/></param>
		/// <returns>Returns the <see cref="ResourceType"/> with the given <paramref name="name"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type of that name is not present in this package</exception>
		public ResourceType GetResourceType(string name) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the ResourceType cannot be null");
			}

			if (resourceTypesByName.TryGetValue(name, out ResourceType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(name,
								GamePackXml.Inst.ResourceTypes,
								ResourceTypesXml.Inst.ResourceType,
								resourceTypesByName,
								resourceTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown resource type");
		}

		/// <summary>
		/// Returns the <see cref="ResourceType"/> with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">ID of the wanted <see cref="ResourceType"/></param>
		/// <returns>Returns the <see cref="ResourceType"/> with the given <paramref name="ID"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type with the given <paramref name="ID"/> is not present in this package</exception>
		public ResourceType GetResourceType(int ID) {
			if (resourceTypesByID.TryGetValue(ID, out ResourceType value)) {
				return value;
			}

			
			if (IsLoading()) {
				return LoadType(ID,
								GamePackXml.Inst.ResourceTypes,
								ResourceTypesXml.Inst.ResourceType,
								resourceTypesByName,
								resourceTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown resource type");
		}

		/// <summary>
		/// Returns the <see cref="PlayerType"/> with the given <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of the wanted <see cref="PlayerType"/></param>
		/// <returns>Returns the <see cref="PlayerType"/> with the given <paramref name="name"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type of that name is not present in this package</exception>
		public PlayerType GetPlayerType(string name)
		{
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the PlayerAIType cannot be null");
			}

			if (playerAITypesByName.TryGetValue(name, out PlayerType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(name,
								GamePackXml.Inst.PlayerAITypes,
								PlayerAITypesXml.Inst.PlayerAIType,
								playerAITypesByName,
								playerAITypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown player type");
		}

		/// <summary>
		/// Returns the <see cref="PlayerType"/> with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">ID of the wanted <see cref="PlayerType"/></param>
		/// <returns>Returns the <see cref="PlayerType"/> with the given <paramref name="ID"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type with the given <paramref name="ID"/> is not present in this package</exception>
		public PlayerType GetPlayerType(int ID) {

			if (playerAITypesByID.TryGetValue(ID, out PlayerType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID, 
								GamePackXml.Inst.PlayerAITypes,
								PlayerAITypesXml.Inst.PlayerAIType,
								playerAITypesByName,
								playerAITypesByID);
			}
			
			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown player type");
		}

		/// <summary>
		/// Returns every <see cref="PlayerType"/> with the <paramref name="category"/>.
		///
		/// Mainly used to get all the AI player types, Human player types or Neutral player types.
		/// </summary>
		/// <param name="category">Category of the wanted playerTypes</param>
		/// <returns>Returns every <see cref="PlayerType"/> with the <paramref name="category"/>.</returns>
		public IEnumerable<PlayerType> GetPlayersWithTypeCategory(PlayerTypeCategory category)
		{
			return from playerType in PlayerTypes
					where playerType.Category == category
					select playerType;
		}

		/// <summary>
		/// Returns the <see cref="LevelLogicType"/> with the given <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of the wanted <see cref="LevelLogicType"/></param>
		/// <returns>Returns the <see cref="LevelLogicType"/> with the given <paramref name="name"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type of that name is not present in this package</exception>
		public LevelLogicType GetLevelLogicType(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name), "Name of the LevelLogicType cannot be null");
			}

			if (levelLogicTypesByName.TryGetValue(name, out LevelLogicType value))
			{
				return value;
			}

			if (IsLoading())
			{
				return LoadType(Name,
								GamePackXml.Inst.LevelLogicTypes,
								LevelLogicTypesXml.Inst.LevelLogicType,
								levelLogicTypesByName,
								levelLogicTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown level logic type");
		}

		/// <summary>
		/// Returns the <see cref="LevelLogicType"/> with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">ID of the wanted <see cref="LevelLogicType"/></param>
		/// <returns>Returns the <see cref="LevelLogicType"/> with the given <paramref name="ID"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when type with the given <paramref name="ID"/> is not present in this package</exception>
		public LevelLogicType GetLevelLogicType(int ID)
		{
			if (levelLogicTypesByID.TryGetValue(ID, out LevelLogicType value))
			{
				return value;
			}

			if (IsLoading())
			{
				return LoadType(ID,
								GamePackXml.Inst.LevelLogicTypes,
								LevelLogicTypesXml.Inst.LevelLogicType,
								levelLogicTypesByName,
								levelLogicTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown level logic type");
		}

		/// <summary>
		/// Returns an instance of <see cref="LevelRep"/> representing the level with the given <paramref name="name"/>.
		///
		/// This instance can be used to manipulate the level.
		/// Throws an exception if a level with the given <paramref name="name"/> cannot be found.
		/// Alternatively you can use <see cref="TryGetLevel(string, out LevelRep)"/> to check the existence of the level.
		/// </summary>
		/// <param name="name">Name of the wanted level.</param>
		/// <returns>Returns an instance of <see cref="LevelRep"/> representing the level with the given <paramref name="name"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when a level with the given <paramref name="name"/> cannot be found.</exception>
		/// <exception cref="ArgumentNullException">Thrown when the given <paramref name="name"/> is null.</exception>
		public LevelRep GetLevel(string name)
		{
			if (TryGetLevel(name, out LevelRep value)) {
				return value;
			}
			else {
				throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown level");
			}
		}

		/// <summary>
		/// Gets an instance of <see cref="LevelRep"/> representing the level with the given <paramref name="name"/> if
		/// such level exists.
		///
		/// Alternatively, you can use <see cref="GetLevel(string)"/> to be informed by exception if the level is not present.
		/// </summary>
		/// <param name="name">Name of the wanted level.</param>
		/// <param name="value">If return value is true, contains the <see cref="LevelRep"/> representing the level with the given <paramref name="name"/></param>
		/// <returns>True if level with the given <paramref name="name"/> was found,
		/// and sets the <paramref name="value"/> to the <see cref="LevelRep"/> representing the level,
		/// or returns false if no level with this <paramref name="name"/> exists.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="name"/> is null.</exception>
		public bool TryGetLevel(string name, out LevelRep value)
		{
			//To enable loading saved games even if their source level was deleted

			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the level cannot be null");
			}

			return levelsByName.TryGetValue(name, out value);
		}

		/// <summary>
		/// Saves the <paramref name="level"/> and adds it to the choice of levels for this gamePack
		/// If there is another level with the same name, and the <paramref name="overrideLevel"/> is not set, throws InvalidOperationException
		/// 
		/// Before saving level without the <paramref name="overrideLevel"/> flag, you should check that there is no level with the same name
		/// Use <see cref="TryGetLevel(string, out LevelRep)"/>
		/// </summary>
		/// <param name="level"></param>
		/// <param name="overrideLevel"></param>
		/// <exception cref="InvalidOperationException">Thrown when a level with the same name as <paramref name="level"/> already exists and the <paramref name="overrideLevel"/> is not set</exception>
		/// <exception cref="PackageLoadingException">Thrown when we could not open or write to the package file.</exception>
		public void SaveLevelPrototype(LevelRep level, bool overrideLevel)
		{
			bool removeExisting = false;
			if (TryGetLevel(level.Name, out LevelRep oldLevel))
			{
				if (overrideLevel)
				{
					removeExisting = true;
				}
				else
				{
					throw new InvalidOperationException("Level with the same name already exists in the package");
				}
			}

			data = LoadXml(pathToXml, schemas);
			//Save the level to xml only if the saving went without exception
			XElement element = level.SaveAsPrototype();

			if (removeExisting) {
				RemoveLevelFromLoadedXml(oldLevel, level != oldLevel);
			}

			levelsByName.Add(level.Name, level);
			XElement levels = data.Root.Element(GamePackXml.Inst.Levels);
			levels.Add(element);

			WriteData();
			ReleaseXml();
		}


		/// <summary>
		/// Removes the <paramref name="level"/> from this package.
		///
		/// Also removes the data of the level.
		/// </summary>
		/// <param name="level">Representant of the level to remove.</param>
		/// <exception cref="ArgumentException">Thrown when the <paramref name="level"/> is not part of this package.</exception>
		/// <exception cref="PackageLoadingException">Thrown when the package file could not be opened, read or written.</exception>
		public void RemoveLevel(LevelRep level)
		{
			if (level.GamePack != this) {
				throw new ArgumentException("The provided level was not part of this gamePack", nameof(level));
			}

			data = LoadXml(pathToXml, schemas);

			RemoveLevelFromLoadedXml(level, true);

			WriteData();
			ReleaseXml();
		}

		/// <summary>
		/// Generates a path to the file to which the level should be saved.
		/// This path is derived from the given <paramref name="levelName"/>,
		///  but is made unique if any levels with the same or similar name already exist.
		///
		/// The path is relative to the <see cref="DirectoryPath"/>
		/// </summary>
		/// <param name="levelName">Name of the level the path is for.</param>
		/// <returns>Returns the path to the file to which the level should be saved.</returns>
		public string GetLevelProtoSavePath(string levelName)
		{
			//Just strip it to bare minimum
			var newPath = new StringBuilder(levelSavingDirPath);
			newPath.Append(Path.DirectorySeparatorChar);
			foreach (var ch in levelName.Where(char.IsLetterOrDigit)) {
				newPath.Append(ch);
			}

			//NOTE: Maybe do better generation of random filename
			var random = new Random();
			while (App.Files.FileExists(newPath.ToString())) {
				int randomDigit = random.Next(10);
				newPath.Append(randomDigit);
			}
			
			return newPath.ToString();
		}

		public void Dispose()
		{
			if (this == PackageManager.ActivePackage) {
				//Clears the PackageManager.ActivePackage and calls this again
				PackageManager.UnloadActivePack();
				return;
			}

			ResourceIconTexture.Dispose();
			TileIconTexture.Dispose();
			UnitIconTexture.Dispose();
			BuildingIconTexture.Dispose();
			PlayerIconTexture.Dispose();
			ToolIconTexture.Dispose();

			foreach (var unitType in unitTypesByName.Values) {
				unitType.Dispose();
			}

			foreach (var buildingType in buildingTypesByName.Values) {
				buildingType.Dispose();
			}

			foreach (var projectileType in projectileTypesByName.Values) {
				projectileType.Dispose();
			}
		}

		/// <summary>
		/// Alias of <see cref="Dispose"/>
		/// </summary>
		public void UnLoad()
		{
			Dispose();
		}

		/// <summary>
		/// Clears caches of all type in this package.
		/// Caches are used in types to enable reuse of instances of the type.
		/// Should be called on level end.
		/// </summary>
		public void ClearCaches()
		{

			foreach (var tileType in  TileTypes) {
				tileType.ClearCache();
			}

			foreach (var unitType in UnitTypes) {
				unitType.ClearCache();
			}

			foreach (var buildingType in BuildingTypes) {
				buildingType.ClearCache();
			}

			foreach (var projectileType in ProjectileTypes) {
				projectileType.ClearCache();
			}

			foreach (var resourceType in ResourceTypes) {
				resourceType.ClearCache();
			}

			foreach (var playerType in PlayerTypes) {
				playerType.ClearCache();
			};
		}

		/// <summary>
		/// Loads XML data from file at <paramref name="pathToXml"/> and
		/// validates this data against  <paramref name="schemas"/>.
		/// </summary>
		/// <param name="pathToXml">Path to the XML to load.</param>
		/// <param name="schemas">XSD schema to validate against.</param>
		/// <returns>Loaded XML document.</returns>
		XDocument LoadXml(string pathToXml, XmlSchemaSet schemas)
		{
			Stream file = null;
			XDocument data;
			try {
				file = App.Files.OpenDynamicFile(pathToXml, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				data = XDocument.Load(file);
				data.Validate(schemas, null);
			}
			catch (XmlSchemaValidationException e) {
				string message = $"Package XML was invalid. Package at: {pathToXml}";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message, e);
			}
			catch (IOException e) {
				string message = $"Failed to open or read the package file at: {pathToXml}";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message, e);
			}
			catch (Exception e)
			{
				string message = $"There was an unexpected exception while reading the package at: {pathToXml}";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message, e);
			}
			finally {
				file?.Dispose();
			}

			return data;
		}

		IEnumerable<TileType> LoadAllTileTypes()
		{
			CheckIfLoading();


			var tileTypesElement = data.Root.Element(GamePackXml.Inst.TileTypes);

			//tileTypes element cannot be null because xsd does not allow it
			var defaultTileTypeElement = tileTypesElement.Element(TileTypesXml.Inst.DefaultTileType);

			DefaultTileType = LoadType<TileType>(defaultTileTypeElement, tileTypesByName, tileTypesByID);


			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return tileTypesElement.Elements(TileTypesXml.Inst.TileType)
								   .Select(element => LoadType<TileType>(element, tileTypesByName, tileTypesByID))
								   .ToArray();
		}

		IEnumerable<UnitType> LoadAllUnitTypes()
		{
			CheckIfLoading();

			var unitTypesElement = data.Root.Element(GamePackXml.Inst.UnitTypes);

			//unitTypes element cannot be null because xsd does not allow it
			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return unitTypesElement.Elements(UnitTypesXml.Inst.UnitType)
								   .Select(unitTypeElement =>
											   LoadType<UnitType>(unitTypeElement, unitTypesByName, unitTypesByID))
								   .ToArray();
		}

		IEnumerable<BuildingType> LoadAllBuildingTypes()
		{
			CheckIfLoading();


			var buildingTypesElement = data.Root.Element(GamePackXml.Inst.BuildingTypes);

			//buildingTypes element cannot be null because xsd does not allow it
			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return buildingTypesElement.Elements(BuildingTypesXml.Inst.BuildingType)
									   .Select(buildingTypeElement =>
												   LoadType<BuildingType>(buildingTypeElement,
																		  buildingTypesByName,
																		  buildingTypesByID))
									   .ToArray();
		}

		IEnumerable<ProjectileType> LoadAllProjectileTypes()
		{
			CheckIfLoading();

			//data.Root cannot be null because xsd does not allow it
			var projectileTypesElement = data.Root.Element(GamePackXml.Inst.ProjectileTypes);

			//projectileTypes element cannot be null because xsd does not allow it
			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return projectileTypesElement.Elements(ProjectileTypesXml.Inst.ProjectileType)
										 .Select(projectileTypeElement =>
												   LoadType<ProjectileType>(projectileTypeElement,
																			projectileTypesByName,
																			projectileTypesByID))
										 .ToArray();
		}

		IEnumerable<ResourceType> LoadAllResourceTypes()
		{
			CheckIfLoading();

			var resourceTypesElement = data.Root.Element(GamePackXml.Inst.ResourceTypes);

			//resourceTypes element cannot be null because xsd does not allow it
			return resourceTypesElement.Elements(ResourceTypesXml.Inst.ResourceType)
									   .Select(resourceTypeElement =>
												   LoadType<ResourceType>(resourceTypeElement,
																		  resourceTypesByName,
																		  resourceTypesByID))
									   .ToArray();
		}

		IEnumerable<PlayerType> LoadAllPlayerTypes()
		{
			CheckIfLoading();

			XElement playerTypes = data.Root.Element(GamePackXml.Inst.PlayerAITypes);

			//playerTypes element cannot be null because xsd does not allow it
			return playerTypes.Elements(PlayerAITypesXml.Inst.PlayerAIType)
							.Select(playerTypeElement =>
										LoadType<PlayerType>(playerTypeElement,
															playerAITypesByName,
															playerAITypesByID))
							.ToArray();
		}

		IEnumerable<LevelLogicType> LoadAllLevelLogicTypes()
		{
			CheckIfLoading();

			//data.Root cannot be null because xsd does not allow it
			XElement levelLogicTypes = data.Root.Element(GamePackXml.Inst.LevelLogicTypes);

			return levelLogicTypes.Elements(LevelLogicTypesXml.Inst.LevelLogicType)
								.Select(levelLogicTypeElement =>
											LoadType<LevelLogicType>(levelLogicTypeElement,
																	levelLogicTypesByName,
																	levelLogicTypesByID))
								.ToArray();
		}

		void LoadIconTextures()
		{
			CheckIfLoading();
			//data.Root cannot be null because xsd does not allow it
			TileIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.TileIconTexturePath)));
			UnitIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.UnitIconTexturePath)));
			BuildingIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.BuildingIconTexturePath)));
			ResourceIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.ResourceIconTexturePath)));
			PlayerIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.PlayerIconTexturePath)));
			ToolIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.ToolIconTexturePath)));
		}

		IEnumerable<LevelRep> LoadAllLevels()
		{
			CheckIfLoading();

			XElement levels = data.Root.Element(GamePackXml.Inst.Levels);

			// Changed xsd to require levels element
			//if (levels == null) {
			//	return Enumerable.Empty<LevelRep>();
			//}

			return levels.Elements(LevelsXml.Inst.Level)
						.Select(LoadLevelRep)
						.ToArray();
		}

		void ReleaseXml()
		{
			data = null;
		}

		void CheckIfLoading()
		{
			if (!IsLoading()) {
				throw new InvalidOperationException("GamePack was not in a loading state");
			}
		}

		bool IsLoading()
		{
			return data != null;
		}

		/// <summary>
		/// Removes all keys whose value has ID 0, returns true if it deleted something, false if not
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="dictionary">removes items with Value.ID 0 from this dictionary</param>
		/// <returns>true if deleted something, false if didnt delete anything</returns>
		bool RemoveUnused<T>(IDictionary<string,T> dictionary)
			where T : IIdentifiable {

			bool deleted = false;
			var toRemove = new List<string>();
			foreach (var item in dictionary) {
				if (item.Value.ID == 0) {
					toRemove.Add(item.Key);
				}
			}

			foreach (var key in toRemove) {
				dictionary.Remove(key);
				deleted = true;
			}
			return deleted;
		}

		XElement GetXmlTypeDescription(string typeName, XName groupName, XName itemName) {
			//Load from file
			var typeElements = (from element in data.Root
													.Element(groupName)
													.Elements(itemName)
										  where GetTypeName(element) == typeName
										  select element).GetEnumerator();

			return GetXmlTypeDescription(typeElements);
		}

		XElement GetXmlTypeDescription(int typeID, XName groupName, XName itemName) {
			var typeElements = (from element in data.Root
													.Element(groupName)
													.Elements(itemName)
								where GetTypeID(element) == typeID
								select element).GetEnumerator();
			return GetXmlTypeDescription(typeElements);
		}

		XElement GetXmlTypeDescription(IEnumerator<XElement> typeElements) {
			if (!typeElements.MoveNext()) {
				throw new ArgumentException("Type of that name does not exist in this package");
			}

			var typeElement = typeElements.Current;

			if (typeElements.MoveNext()) {
				throw new ArgumentException("Duplicate type names in a package");
			}

			typeElements.Dispose();

			return typeElement;
		}


		static string GetTypeName(XElement typeElement) {
			return typeElement.Attribute("name").Value;
		}

		static int GetTypeID(XElement typeElement) {
			return XmlHelpers.GetIntAttribute(typeElement, "ID");
		}

		T LoadType<T>(XElement typeElement, IDictionary<string, T> typesByName, IDictionary<int, T> typesByID)
			where T : ILoadableType, new() {
			string name = GetTypeName(typeElement);
			int ID = GetTypeID(typeElement);

			T typeInstance;

			bool byName = typesByName.TryGetValue(name, out T typeInstanceByName);
			bool byID = typesByID.TryGetValue(ID, out T typeInstanceByID);

			if (!byName && !byID) {

				typeInstance = new T();
				typesByName.Add(name, typeInstance);
				typesByID.Add(ID, typeInstance);

				typeInstance.Load(typeElement, this);

			}
			else if (byName && byID) {
				if (typeInstanceByName.Equals(typeInstanceByID)) {
					typeInstance = typeInstanceByName;
				}
				else {
					throw new
						InvalidOperationException("There were two different types under the ID and Name of the loaded type");
				}
			}
			else {
				throw new InvalidOperationException("The type was only mapped by one of its parameters, not both");
			}

			return typeInstance;

		}

		T LoadType<T>(int ID,
					  XName groupName,
					  XName itemName,
					  IDictionary<string, T> typesByName,
					  IDictionary<int, T> typesByID)
			where T : ILoadableType, new()
		{
			CheckIfLoading();

			var typeElement = GetXmlTypeDescription(ID,
													groupName,
													itemName);

			return LoadType(typeElement, typesByName, typesByID);
		}

		T LoadType<T>(string name,
					  XName groupName,
					  XName itemName,
					  IDictionary<string, T> typesByName,
					  IDictionary<int, T> typesByID) 
			where T: ILoadableType, new()
		{
			CheckIfLoading();

			if (name == null) {
				throw new ArgumentNullException("Name of the type cannot be null");
			}

			var typeElement = GetXmlTypeDescription(name,
													groupName,
													itemName);

			return LoadType<T>(typeElement, typesByName, typesByID);
		}

		LevelRep LoadLevelRep(XElement levelElement)
		{
			LevelRep newLevel = LevelRep.GetFromLevelPrototype(this, levelElement);
			levelsByName.Add(newLevel.Name, newLevel);

			return newLevel;
		}

		void WriteData()
		{
			Stream file = null;
			try {
				//Validate before truncating the file
				data.Validate(schemas, null);
				file = App.Files.OpenDynamicFile(pathToXml, System.IO.FileMode.Truncate, System.IO.FileAccess.Write);
				data.Save(file);
			}
			catch (XmlSchemaValidationException e) {
				string message = $"Package XML was invalid. Package at: {pathToXml}";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message, e);
			}
			catch (IOException e) {
				string message = $"Could not open or write to the package file at: {pathToXml}";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message, e);
			}
			catch (Exception e) {
				string message = $"There was an unexpected exception while saving the package at: {pathToXml}";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message, e);
			}
			finally {
				file?.Dispose();
			}

		}

		void RemoveLevelFromLoadedXml(LevelRep level, bool removeData)
		{
			if (!levelsByName.Remove(level.Name))
			{
				//This should not happen, but just to be safe
				throw new
					InvalidOperationException("Bug in the program, the level was not present in the levels dictionary even though it was from this gamePack");
			}

			//This should be correct thanks to xsd validation
			XElement levels = data.Root.Element(GamePackXml.Inst.Levels);

			var levelElement = levels.Elements(LevelsXml.Inst.Level)
									.FirstOrDefault(levelElem => string.Equals(levelElem.Attribute(LevelXml.Inst.NameAttribute).Value,
																				level.Name,
																				StringComparison.InvariantCultureIgnoreCase));

			if (levelElement == null)
			{
				throw new
					InvalidOperationException("Xml changed outside the control of the program, you should restart the program to fix this");
			}

			levelElement.Remove();
			if (removeData) {
				level.RemoveDataFile();
			}
		}
	}
}
