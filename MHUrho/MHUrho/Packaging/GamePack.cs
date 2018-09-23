using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using MHUrho.Helpers;
using MHUrho.Logic;
using Urho;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.Packaging {

	public class GamePack : IDisposable {
		const string DefaultThumbnailPath = "Textures/xamarin.png";

		public GamePackRep GamePackRep { get; private set; }

		public string Name => GamePackRep.Name;

		public PackageManager PackageManager => GamePackRep.PackageManager;

		public string XmlDirectoryPath => GamePackRep.XmlDirectoryPath;

		public string RootedXmlDirectoryPath => Path.Combine(MyGame.Files.DynamicDirPath, XmlDirectoryPath);

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
		readonly Dictionary<string, LevelRep> levelsByName;

		readonly Dictionary<int, TileType> tileTypesByID;
		readonly Dictionary<int, UnitType> unitTypesByID;
		readonly Dictionary<int, BuildingType> buildingTypesByID;
		readonly Dictionary<int, ProjectileType> projectileTypesByID;
		readonly Dictionary<int, ResourceType> resourceTypesByID;
		readonly Dictionary<int, PlayerType> playerAITypesByID;

		readonly string pathToXml;
		readonly XmlSchemaSet schemas;
		XDocument data;

		/// <summary>
		/// Directory path to save level prototypes in
		/// </summary>
		readonly string levelSavingDirPath;

		public GamePack(string pathToXml,
						GamePackRep gamePackRep,
						XmlSchemaSet schemas,
						ILoadingSignaler loadingProgress) {

			tileTypesByName = new Dictionary<string, TileType>();
			unitTypesByName = new Dictionary<string, UnitType>();
			buildingTypesByName = new Dictionary<string, BuildingType>();
			projectileTypesByName = new Dictionary<string, ProjectileType>();
			resourceTypesByName = new Dictionary<string, ResourceType>();
			playerAITypesByName = new Dictionary<string, PlayerType>();
			levelsByName = new Dictionary<string, LevelRep>();

			tileTypesByID = new Dictionary<int, TileType>();
			unitTypesByID = new Dictionary<int, UnitType>();
			buildingTypesByID = new Dictionary<int, BuildingType>();
			projectileTypesByID = new Dictionary<int, ProjectileType>();
			resourceTypesByID = new Dictionary<int, ResourceType>();
			playerAITypesByID = new Dictionary<int, PlayerType>();

			this.GamePackRep = gamePackRep;
			this.schemas = schemas;
			this.pathToXml = pathToXml;

			pathToXml = FileManager.CorrectRelativePath(pathToXml);

			try {
				XDocument xml = StartLoading(pathToXml, schemas);

				//Validation in StartLoading should take care of any missing elements
				levelSavingDirPath = FileManager.CorrectRelativePath(xml.Root
																		.Element(PackageManager.XMLNamespace + "levels")
																		.Element(PackageManager.XMLNamespace + "dataDirPath")
																		.Value.Trim());

				loadingProgress.TextAndPercentageUpdate("Loading tile types", 5);
				LoadAllTileTypes();

				loadingProgress.TextAndPercentageUpdate("Loading unit types", 5);
				LoadAllUnitTypes();

				loadingProgress.TextAndPercentageUpdate("Loading building types", 5);
				LoadAllBuildingTypes();

				loadingProgress.TextAndPercentageUpdate("Loading projectile types", 5);
				LoadAllProjectileTypes();

				loadingProgress.TextAndPercentageUpdate("Loading resource types", 5);
				LoadAllResourceTypes();

				loadingProgress.TextAndPercentageUpdate("Loading player types", 5);
				LoadAllPlayerTypes();

				loadingProgress.TextAndPercentageUpdate("Loading levels", 5);
				LoadAllLevels();
			}
			//TODO: Catch only the expected exceptions
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Warning, $"Package loading failed with: \"{e}\"");
				throw new PackageLoadingException($"Package loading failed with: \"{e}\"", e);
			}
			finally {
				FinishLoading();
			}

		}

		public TileType GetTileType(string name) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name),"Name of the tileType cannot be null");
			}

			if (tileTypesByName.TryGetValue(name, out TileType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(name, GamePackXml.TileTypes, GamePackXml.TileType, tileTypesByName, tileTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown tile type");
		}

		public TileType GetTileType(int ID) {

			if (tileTypesByID.TryGetValue(ID, out TileType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID, GamePackXml.TileTypes, GamePackXml.TileType, tileTypesByName, tileTypesByID);
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
				return LoadType(name, GamePackXml.UnitTypes, GamePackXml.UnitType, unitTypesByName, unitTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown unit type");

		}

		public UnitType GetUnitType(int ID) {

			if (unitTypesByID.TryGetValue(ID, out  UnitType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID, GamePackXml.UnitTypes, GamePackXml.UnitType, unitTypesByName, unitTypesByID);
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
								GamePackXml.BuildingTypes,
								GamePackXml.BuildingType,
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
								GamePackXml.BuildingTypes,
								GamePackXml.BuildingType,
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
								GamePackXml.ProjectileTypes,
								GamePackXml.ProjectileType,
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
								GamePackXml.ProjectileTypes,
								GamePackXml.ProjectileType,
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
								GamePackXml.ResourceTypes,
								GamePackXml.ResourceType,
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
								GamePackXml.ResourceTypes,
								GamePackXml.ResourceType,
								resourceTypesByName,
								resourceTypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown resource type");
		}

		public PlayerType GetPlayerAIType(string name)
		{
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the PlayerAIType cannot be null");
			}

			if (playerAITypesByName.TryGetValue(name, out PlayerType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(name,
								GamePackXml.PlayerAITypes,
								GamePackXml.PlayerAIType,
								playerAITypesByName,
								playerAITypesByID);
			}

			throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown player type");
		}

		public PlayerType GetPlayerAIType(int ID) {

			if (playerAITypesByID.TryGetValue(ID, out PlayerType value)) {
				return value;
			}

			if (IsLoading()) {
				return LoadType(ID, 
								GamePackXml.PlayerAITypes,
								GamePackXml.PlayerAIType,
								playerAITypesByName,
								playerAITypesByID);
			}
			
			throw new ArgumentOutOfRangeException(nameof(ID), ID, "Unknown player type");
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

		public void SaveLevel(LevelRep level)
		{
			var xmlData = StartLoading(pathToXml, schemas);
			//Should not be null, because startLoading validates the xml
			level.SaveTo(xmlData.Root.Element(GamePackXml.Levels));
			WriteData();
			FinishLoading();
		}

		public string GetLevelProtoSavePath(string levelName)
		{
			//Just strip it to bare minimum
			var newPath = new StringBuilder(levelSavingDirPath);
			newPath.Append(Path.DirectorySeparatorChar);
			foreach (var ch in levelName.Where(char.IsLetterOrDigit)) {
				newPath.Append(ch);
			}

			var random = new Random();
			while (MyGame.Files.FileExists(newPath.ToString())) {
				int randomDigit = random.Next(10);
				newPath.Append(randomDigit);
			}
			//TODO: THIS
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

		XDocument StartLoading(string pathToXml, XmlSchemaSet schemas)
		{
			Stream file = null;
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

			TileIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"tileIconTexturePath")));

			var tileTypesElement = data.Root.Element(PackageManager.XMLNamespace + "tileTypes");

			// Changed xsd to require playerTypes element
			//if (tileTypesElement == null) {
			//	//There are no tile types in this package
			//	throw new InvalidOperationException("Default tile type is missing");
			//}

			var defaultTileTypeElement = tileTypesElement.Element(PackageManager.XMLNamespace + "defaultTileType");

			DefaultTileType = LoadType<TileType>(defaultTileTypeElement, tileTypesByName, tileTypesByID);

			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return tileTypesElement.Elements(PackageManager.XMLNamespace + "tileType")
								   .Select(element => LoadType<TileType>(element, tileTypesByName, tileTypesByID))
								   .ToArray();
		}

		IEnumerable<UnitType> LoadAllUnitTypes()
		{
			CheckIfLoading();

			UnitIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"unitIconTexturePath")));

			var unitTypesElement = data.Root.Element(PackageManager.XMLNamespace + "unitTypes");

			// Changed xsd to require unitTypes element
			//if (unitTypesElement == null) {
			//	//There are no unit types in this package
			//	return Enumerable.Empty<UnitType>();
			//}

			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return unitTypesElement.Elements(PackageManager.XMLNamespace + "unitType")
								   .Select(unitTypeElement =>
											   LoadType<UnitType>(unitTypeElement, unitTypesByName, unitTypesByID))
								   .ToArray();
		}

		IEnumerable<BuildingType> LoadAllBuildingTypes()
		{
			CheckIfLoading();

			BuildingIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"buildingIconTexturePath")));

			var buildingTypesElement = data.Root.Element(PackageManager.XMLNamespace + "buildingTypes");

			// Changed xsd to require buildingTypes element
			//if (buildingTypesElement == null) {
			//	//There are no building types in this package
			//	return Enumerable.Empty<BuildingType>();
			//}

			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return buildingTypesElement.Elements(PackageManager.XMLNamespace + "buildingType")
									   .Select(buildingTypeElement =>
												   LoadType<BuildingType>(buildingTypeElement,
																		  buildingTypesByName,
																		  buildingTypesByID))
									   .ToArray();
		}

		IEnumerable<ProjectileType> LoadAllProjectileTypes()
		{
			CheckIfLoading();

			var projectileTypesElement = data.Root.Element(PackageManager.XMLNamespace + "projectileTypes");

			// Changed xsd to require playerTypes element
			//if (projectileTypesElement == null) {
			//	//There are no projectile types in this package
			//	return Enumerable.Empty<ProjectileType>();
			//}


			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return projectileTypesElement.Elements(PackageManager.XMLNamespace + "projectileType")
										 .Select(projectileTypeElement =>
												   LoadType<ProjectileType>(projectileTypeElement,
																			projectileTypesByName,
																			projectileTypesByID))
										 .ToArray();
		}

		IEnumerable<ResourceType> LoadAllResourceTypes()
		{
			CheckIfLoading();

			ResourceIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"resourceIconTexturePath")));

			var resourceTypesElement = data.Root.Element(PackageManager.XMLNamespace + "resourceTypes");

			// Changed xsd to require resourceTypes element
			//if (resourceTypesElement == null) {
			//	return Enumerable.Empty<ResourceType>();
			//}

			return resourceTypesElement.Elements(PackageManager.XMLNamespace + "resourceType")
									   .Select(resourceTypeElement =>
												   LoadType<ResourceType>(resourceTypeElement,
																		  resourceTypesByName,
																		  resourceTypesByID))
									   .ToArray();
		}

		IEnumerable<PlayerType> LoadAllPlayerTypes()
		{
			CheckIfLoading();

			PlayerIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"playerIconTexturePath")));

			XElement playerTypes = data.Root.Element(PackageManager.XMLNamespace + "playerAITypes");

			// Changed xsd to require playerTypes element
			//if (playerTypes == null) {
			//	return Enumerable.Empty<PlayerType>();
			//}

			return playerTypes.Elements(PackageManager.XMLNamespace + "playerAIType")
							.Select(playerTypeElement =>
										LoadType<PlayerType>(playerTypeElement,
															playerAITypesByName,
															playerAITypesByID))
							.ToArray();
		}

		IEnumerable<LevelRep> LoadAllLevels()
		{
			CheckIfLoading();

			XElement levels = data.Root.Element(PackageManager.XMLNamespace + "levels");

			// Changed xsd to require levels element
			//if (levels == null) {
			//	return Enumerable.Empty<LevelRep>();
			//}

			return levels.Elements(PackageManager.XMLNamespace + "level")
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

		XElement GetXmlTypeDescription(string typeName ,XName groupName, XName itemName) {
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
				file = MyGame.Files.OpenDynamicFile(pathToXml, System.IO.FileMode.Open, System.IO.FileAccess.Write);
				data.Validate(schemas, null);
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
