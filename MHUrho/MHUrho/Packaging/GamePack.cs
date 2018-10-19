using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Plugins;
using Urho;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.Packaging {

	public class GamePack : IDisposable {
		const string DefaultThumbnailPath = "Textures/xamarin.png";

		public GamePackRep GamePackRep { get; private set; }

		public string Name => GamePackRep.Name;

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
		public string RootedDirectoryPath => Path.Combine(MyGame.Files.DynamicDirPath, DirectoryPath);

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
		/// 
		/// </summary>
		/// <param name="pathToXml"></param>
		/// <param name="gamePackRep"></param>
		/// <param name="schemas"></param>
		/// <param name="loadingProgress"></param>
		/// <returns></returns>
		/// <exception cref="PackageLoadingException">Thrown when the package loading failed</exception>
		public static async Task<GamePack> Load(string pathToXml,
												GamePackRep gamePackRep,
												XmlSchemaSet schemas,
												ILoadingSignaler loadingProgress)
		{
			GamePack newPack = new GamePack(pathToXml, gamePackRep, schemas);

			pathToXml = FileManager.CorrectRelativePath(pathToXml);

			try
			{
				newPack.data = await MyGame.InvokeOnMainSafeAsync(() => StartLoading(pathToXml, schemas));
				//Validation in StartLoading should take care of any missing elements
				newPack.levelSavingDirPath = FileManager.CorrectRelativePath(newPack.data.Root
																						.Element(GamePackXml.Inst.Levels)
																						.Element(LevelsXml.Inst.DataDirPath)
																						.Value.Trim());

				loadingProgress.TextAndPercentageUpdate("Loading tile types", 12.5f);
				await MyGame.InvokeOnMainSafeAsync(newPack.LoadAllTileTypes);

				loadingProgress.TextAndPercentageUpdate("Loading unit types", 12.5f);
				await MyGame.InvokeOnMainSafeAsync(newPack.LoadAllUnitTypes);

				loadingProgress.TextAndPercentageUpdate("Loading building types", 12.5f);
				await MyGame.InvokeOnMainSafeAsync(newPack.LoadAllBuildingTypes);

				loadingProgress.TextAndPercentageUpdate("Loading projectile types", 12.5f);
				await MyGame.InvokeOnMainSafeAsync(newPack.LoadAllProjectileTypes);

				loadingProgress.TextAndPercentageUpdate("Loading resource types", 12.5f);
				await MyGame.InvokeOnMainSafeAsync(newPack.LoadAllResourceTypes);

				loadingProgress.TextAndPercentageUpdate("Loading player types", 12.5f);
				await MyGame.InvokeOnMainSafeAsync(newPack.LoadAllPlayerTypes);

				loadingProgress.TextAndPercentageUpdate("Loading level logic types", 12.5f);
				await MyGame.InvokeOnMainSafeAsync(newPack.LoadAllLevelLogicTypes);

				loadingProgress.TextAndPercentageUpdate("Loading levels", 12.5f);
				await MyGame.InvokeOnMainSafeAsync(newPack.LoadAllLevels);
			}
			//TODO: Catch only the expected exceptions
			catch (Exception e)
			{
				string message = $"Package loading failed with: \"{e.Message}\"";
				Urho.IO.Log.Write(LogLevel.Warning, message);
				throw new PackageLoadingException(message, e);
			}
			finally
			{
				newPack.FinishLoading();
			}

			return newPack;
		}

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

		public TileType GetTileType(int ID) {

			if (tileTypesByID.TryGetValue(ID, out TileType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID, GamePackXml.Inst.TileTypes, TileTypesXml.Inst.TileType, tileTypesByName, tileTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown tile type");
		}

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

		public UnitType GetUnitType(int ID) {

			if (unitTypesByID.TryGetValue(ID, out  UnitType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID, GamePackXml.Inst.UnitTypes, UnitTypesXml.Inst.UnitType, unitTypesByName, unitTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown unit type");

		}

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

		public IEnumerable<PlayerType> GetPlayersWithTypeCategory(PlayerTypeCategory category)
		{
			return from playerType in PlayerTypes
					where playerType.Category == category
					select playerType;
		}

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

		public LevelRep GetLevel(string name)
		{
			if (TryGetLevel(name, out LevelRep value)) {
				return value;
			}
			else {
				throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown level");
			}
		}

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
		public void SaveLevel(LevelRep level, bool overrideLevel)
		{
			if (TryGetLevel(level.Name, out LevelRep oldLevel))
			{
				if (overrideLevel)
				{
					RemoveLevel(oldLevel);
				}
				else
				{
					throw new InvalidOperationException("Level with the same name already exists in the package");
				}
			}

			data = StartLoading(pathToXml, schemas);
			//Should not be null, because startLoading validates the xml
			level.SaveTo(data.Root.Element(GamePackXml.Inst.Levels));
			WriteData();
			FinishLoading();
		}


		public void RemoveLevel(LevelRep level)
		{
			if (level.GamePack != this) {
				throw new ArgumentException("The provided level was not part of this gamePack", nameof(level));
			}

			data = StartLoading(pathToXml, schemas);

			if (!levelsByName.Remove(level.Name)) {
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

			if (levelElement == null) {
				throw new
					InvalidOperationException("Xml changed outside the control of the program, you should restart the program to fix this");
			}

			levelElement.Remove();
			level.RemoveDataFile();

			WriteData();
			FinishLoading();
		}

		/// <summary>
		/// Returns the path to the file to which the level should be saved
		///
		/// The path is relative to the <see cref="DirectoryPath"/>
		/// </summary>
		/// <param name="levelName"></param>
		/// <returns></returns>
		public string GetLevelProtoSavePath(string levelName)
		{
			//Just strip it to bare minimum
			var newPath = new StringBuilder(levelSavingDirPath);
			newPath.Append(Path.DirectorySeparatorChar);
			foreach (var ch in levelName.Where(char.IsLetterOrDigit)) {
				newPath.Append(ch);
			}

			//TODO: Better generation of random filename
			var random = new Random();
			while (MyGame.Files.FileExists(newPath.ToString())) {
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

		public void UnLoad()
		{
			Dispose();
		}

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

		static XDocument StartLoading(string pathToXml, XmlSchemaSet schemas)
		{
			Stream file = null;
			XDocument data;
			//TODO: Handler and signal that resource pack is in invalid state
			try {
				file = MyGame.Files.OpenDynamicFile(pathToXml, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				data = XDocument.Load(file);
				data.Validate(schemas, null);
			}
			catch (XmlSchemaValidationException e) {
				Urho.IO.Log.Write(LogLevel.Warning, $"Package XML was invalid. Package at: {pathToXml}");
				//TODO: Exception
				throw new ApplicationException($"Package XML was invalid. Package at: {pathToXml}", e);
			}
			//TODO: Catch file opening failed
			finally {
				file?.Dispose();
			}

			return data;
		}

		IEnumerable<TileType> LoadAllTileTypes()
		{
			CheckIfLoading();

			//data.Root cannot be null because xsd does not allow it
			TileIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.TileIconTexturePath)));

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

			//data.Root cannot be null because xsd does not allow it
			UnitIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.UnitIconTexturePath)));

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

			//data.Root cannot be null because xsd does not allow it
			BuildingIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.BuildingIconTexturePath)));

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

			//data.Root cannot be null because xsd does not allow it
			ResourceIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.ResourceIconTexturePath)));

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

			//data.Root cannot be null because xsd does not allow it
			PlayerIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(GamePackXml.Inst.PlayerIconTexturePath)));

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

		void FinishLoading()
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
			where T : IIDNameAndPackage {

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
				throw new ArgumentException("type of that name does not exist in this package");
			}

			var typeElement = typeElements.Current;

			if (typeElements.MoveNext()) {
				//TODO: Exception
				throw new Exception("Duplicate type names");
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
			//TODO: Handler and signal that resource pack is in invalid state
			try {
				//Validate before truncating the file
				data.Validate(schemas, null);
				file = MyGame.Files.OpenDynamicFile(pathToXml, System.IO.FileMode.Truncate, System.IO.FileAccess.Write);
				data.Save(file);
			}
			//TODO: Other exceptions
			catch (XmlSchemaValidationException e) {
				Urho.IO.Log.Write(LogLevel.Warning, $"Package XML was invalid. Package at: {pathToXml}");
				//TODO: Exception
				throw new ApplicationException($"Package XML was invalid. Package at: {pathToXml}", e);
			}
			//TODO: Catch file opening failed
			finally {
				file?.Dispose();
			}

		}


	}
}
