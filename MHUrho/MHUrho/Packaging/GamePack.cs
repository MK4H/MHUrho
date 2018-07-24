using System;
using System.Collections.Generic;
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
	public class GamePack {
		const string DefaultThumbnailPath = "Textures/xamarin.png";

		const string TileTypeGroupName = "tileTypes";
		const string TileTypeItemName = "tileType";
		const string UnitTypeGroupName = "unitTypes";
		const string UnitTypeItemName = "unitType";
		const string BuildingTypeGroupName = "buildingTypes";
		const string BuildingTypeItemName = "buildingType";
		const string ProjectileTypeGroupName = "projectileTypes";
		const string ProjectileTypeItemName = "projectileType";
		const string ResourceTypeGroupName = "resourceTypes";
		const string ResourceTypeItemName = "resourceType";
		const string PlayerAITypeGroupName = "playerAITypes";
		const string PlayerAITypeItemName = "playerAIType";

		public string Name { get; private set; }
	  

		public string Description { get; private set; }

		public Image Thumbnail { get; private set; }

		public bool FullyLoaded { get; private set; }

		public string XmlDirectoryPath => System.IO.Path.GetDirectoryName(pathToXml);

		public PackageManager PackageManager { get; private set; }

		public TileType DefaultTileType { get; private set; }

		public int TileTypeCount => tileTypesByName.Count;

		public IEnumerable<TileType> TileTypes => tileTypesByName.Values;

		public int UnitTypeCount => unitTypesByName.Count;

		public IEnumerable<UnitType> UnitTypes => unitTypesByName.Values;

		public int BuildingTypeCount => buildingTypesByName.Count;

		public IEnumerable<BuildingType> BuildingTypes => buildingTypesByName.Values;

		public int ProjectileTypeCount => projectileTypesByName.Count;

		public IEnumerable<ProjectileType> ProjectileTypes => projectileTypesByName.Values;

		public Texture2D ResourceIconTexture { get; private set; }
		public Texture2D TileIconTexture { get; private set; }
		public Texture2D UnitIconTexture { get; private set; }
		public Texture2D BuildingIconTexture { get; private set; }
		public Texture2D PlayerIconTexture { get; private set; }

		readonly string pathToXml;

	   
		Dictionary<string, TileType> tileTypesByName;
		Dictionary<string, UnitType> unitTypesByName;
		Dictionary<string, BuildingType> buildingTypesByName;
		Dictionary<string, ProjectileType> projectileTypesByName;
		Dictionary<string, ResourceType> resourceTypesByName;
		Dictionary<string, PlayerType> playerAITypesByName;

		Dictionary<int, TileType> tileTypesByID;
		Dictionary<int, UnitType> unitTypesByID;
		Dictionary<int, BuildingType> buildingTypesByID;
		Dictionary<int, ProjectileType> projectileTypesByID;
		Dictionary<int, ResourceType> resourceTypesByID;
		Dictionary<int, PlayerType> playerAITypesByID;

		XDocument data;

		/// <summary>
		/// Loads data for initial resource pack managment, so the user can choose which resource packs to 
		/// use, which to download, which to delete and so on
		/// 
		/// Before using this resource pack in game, you need to call LoadAll(...)
		/// </summary>
		/// <param name="name">Name of the resource pack</param>
		/// <param name="pathToXml">Path to the resource pack XML description</param>
		/// <param name="description">Human readable description of the resource pack contents for the user</param>
		/// <param name="pathToThumbnail">Path to thumbnail to display</param>
		/// <param name="packageManager"></param>
		/// <returns>Initialized resource pack</returns>
		public static GamePack InitialLoad( string name,
											string pathToXml, 
											string description, 
											string pathToThumbnail,
											PackageManager packageManager) {
			pathToXml = FileManager.CorrectRelativePath(pathToXml);
			pathToThumbnail = FileManager.CorrectRelativePath(pathToThumbnail);
			var thumbnail = PackageManager.Instance.GetImage(pathToThumbnail ?? DefaultThumbnailPath);

			return new GamePack(name, pathToXml, description ?? "No description", thumbnail, packageManager);
		}

		protected GamePack(string name, string pathToXml, string description, Image thumbnail, PackageManager packageManager) {
			this.Name = name;
			this.pathToXml = pathToXml;
			this.Description = description;
			this.Thumbnail = thumbnail;
			this.PackageManager = packageManager;
			this.FullyLoaded = false;


		}

		public void StartLoading(XmlSchemaSet schemas) {
			data = XDocument.Load(MyGame.Files.OpenDynamicFile(pathToXml, System.IO.FileMode.Open, System.IO.FileAccess.Read));
			//TODO: Handler and signal that resource pack is in invalid state
			try {
				data.Validate(schemas, null);
			}
			catch (XmlSchemaValidationException e) {
				Urho.IO.Log.Write(LogLevel.Warning, $"Package XML was invalid. Package at: {pathToXml}");
				//TODO: Exception
				throw new ApplicationException($"Package XML was invalid. Package at: {pathToXml}", e);
			}
			

			tileTypesByName = new Dictionary<string, TileType>();
			unitTypesByName = new Dictionary<string, UnitType>();
			buildingTypesByName = new Dictionary<string, BuildingType>();
			projectileTypesByName = new Dictionary<string, ProjectileType>();
			resourceTypesByName = new Dictionary<string, ResourceType>();
			playerAITypesByName = new Dictionary<string, PlayerType>();

			tileTypesByID = new Dictionary<int, TileType>();
			unitTypesByID = new Dictionary<int, UnitType>();
			buildingTypesByID = new Dictionary<int, BuildingType>();
			projectileTypesByID = new Dictionary<int, ProjectileType>();
			resourceTypesByID = new Dictionary<int, ResourceType>();
			playerAITypesByID = new Dictionary<int, PlayerType>();

		}

		public void FinishLoading() {
			bool deleted = RemoveUnused(tileTypesByName);
			deleted = RemoveUnused(unitTypesByName) || deleted;
			data = null;

			FullyLoaded = !deleted;
		}

		public TileType GetTileType(string name, bool load = false) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name),"Name of the tileType cannot be null");
			}

			bool found = tileTypesByName.TryGetValue(name, out TileType value);

			if (load && !found) {
				return LoadType(name, TileTypeGroupName, TileTypeItemName, tileTypesByName, tileTypesByID);
			}
			else {
				return value;
			}
		}

		public TileType GetTileType(int ID, bool load = false) {
			bool found = tileTypesByID.TryGetValue(ID, out TileType value);

			if (load && !found) {
				return LoadType(ID, TileTypeGroupName, TileTypeItemName, tileTypesByName, tileTypesByID);
			}
			else {
				return value;
			}
		}

		public UnitType GetUnitType(string name, bool load = false) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the unitType cannot be null");
			}

			bool found = unitTypesByName.TryGetValue(name, out UnitType value);

			if (load && !found) {
				return LoadType(name, UnitTypeGroupName, UnitTypeItemName, unitTypesByName, unitTypesByID);
			}
			else {
				return value;
			}
		}

		public UnitType GetUnitType(int ID, bool load = false) {
			bool found = unitTypesByID.TryGetValue(ID, out UnitType value);

			if (load && !found) {
				return LoadType(ID, UnitTypeGroupName, UnitTypeItemName, unitTypesByName, unitTypesByID);
			}
			else {
				return value;
			}
		}

		public BuildingType GetBuildingType(string name, bool load = false) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the buildingType cannot be null");
			}

			bool found =  buildingTypesByName.TryGetValue(name, out BuildingType value);

			if (load && !found) {
				return LoadType(name, 
								BuildingTypeGroupName, 
								BuildingTypeItemName, 
								buildingTypesByName,
								buildingTypesByID);
			}
			else {
				return value;
			}
		}

		public BuildingType GetBuildingType(int ID, bool load = false) {
			bool found = buildingTypesByID.TryGetValue(ID, out BuildingType value);

			if (load && !found) {
				return LoadType(ID,
								BuildingTypeGroupName,
								BuildingTypeItemName,
								buildingTypesByName,
								buildingTypesByID);
			}
			else {
				return value;
			}
		}

		public ProjectileType GetProjectileType(string name, bool load = false) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the projectileType cannot be null");
			}

			bool found = projectileTypesByName.TryGetValue(name, out ProjectileType value);

			if (load && !found) {
				return LoadType(name,
								ProjectileTypeGroupName,
								ProjectileTypeItemName,
								projectileTypesByName,
								projectileTypesByID);
			}
			else {
				return value;
			}
		}

		public ProjectileType GetProjectileType(int ID, bool load = false) {
			bool found = projectileTypesByID.TryGetValue(ID, out ProjectileType value);

			if (load && !found) {
				return LoadType(ID,
								ProjectileTypeGroupName,
								ProjectileTypeItemName,
								projectileTypesByName,
								projectileTypesByID);
			}
			else {
				return value;
			}
		}

		public ResourceType GetResourceType(string name, bool load = false) {
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the ResourceType cannot be null");
			}

			bool found = resourceTypesByName.TryGetValue(name, out ResourceType value);

			if (load && !found) {
				return LoadType(name,
								ResourceTypeGroupName,
								ResourceTypeItemName,
								resourceTypesByName,
								resourceTypesByID);
			}
			else {
				return value;
			}
		}

		public ResourceType GetResourceType(int ID, bool load = false) {
			bool found = resourceTypesByID.TryGetValue(ID, out ResourceType value);

			if (load && !found) {
				return LoadType(ID,
								ResourceTypeGroupName,
								ResourceTypeItemName,
								resourceTypesByName,
								resourceTypesByID);
			}
			else {
				return value;
			}
		}

		public PlayerType GetPlayerAIType(string name, bool load = false)
		{
			if (name == null) {
				throw new ArgumentNullException(nameof(name), "Name of the PlayerAIType cannot be null");
			}

			bool found = playerAITypesByName.TryGetValue(name, out PlayerType value);

			if (load && !found) {
				return LoadType(name,
								PlayerAITypeGroupName,
								PlayerAITypeItemName,
								playerAITypesByName,
								playerAITypesByID);
			}
			else {
				return value;
			}
		}

		public PlayerType GetPlayerAIType(int ID, bool load = false) {
			bool found = playerAITypesByID.TryGetValue(ID, out PlayerType value);

			if (load && !found) {
				return LoadType(ID, 
								PlayerAITypeGroupName,
								PlayerAITypeItemName,
								playerAITypesByName,
								playerAITypesByID);
			}
			else {
				return value;
			}
		}

		public void Load(XmlSchemaSet schemas, 
						LoadingWatcher loadingProgress) {
			try {
				StartLoading(schemas);

				loadingProgress.EnterPhaseWithIncrement("Loading tile types", 5);
				LoadAllTileTypes();

				loadingProgress.EnterPhaseWithIncrement("Loading unit types" , 5);
				LoadAllUnitTypes();

				loadingProgress.EnterPhaseWithIncrement("Loading building types", 5);
				LoadAllBuildingTypes();

				loadingProgress.EnterPhaseWithIncrement("Loading projectile types", 5);
				LoadAllProjectileTypes();

				loadingProgress.EnterPhaseWithIncrement("Loading resource types", 5);
				LoadAllResourceTypes();

				loadingProgress.EnterPhaseWithIncrement("Loading player types", 5);
				LoadAllPlayerTypes();
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Warning, "Package loading failed");
			}
		}


		public IEnumerable<TileType> LoadAllTileTypes() {
			CheckIfLoading();

			TileIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"tileIconTexturePath")));

			var tileTypesElement = data.Root.Element(PackageManager.XMLNamespace + "tileTypes");

			if (tileTypesElement == null) {
				//There are no tile types in this package
				throw new InvalidOperationException("Default tile type is missing");
			}

			var defaultTileTypeElement = tileTypesElement.Element(PackageManager.XMLNamespace + "defaultTileType");

			DefaultTileType = LoadType<TileType>(defaultTileTypeElement, tileTypesByName, tileTypesByID);

			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return tileTypesElement.Elements(PackageManager.XMLNamespace + "tileType")
								   .Select(element => LoadType<TileType>(element, tileTypesByName, tileTypesByID))
								   .ToArray(); 
		}

		public IEnumerable<UnitType> LoadAllUnitTypes() {
			CheckIfLoading();

			UnitIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"unitIconTexturePath")));

			var unitTypesElement = data.Root.Element(PackageManager.XMLNamespace + "unitTypes");

			if (unitTypesElement == null) {
				//There are no unit types in this package
				return Enumerable.Empty<UnitType>();
			}
			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return unitTypesElement.Elements(PackageManager.XMLNamespace + "unitType")
								   .Select(unitTypeElement =>
											   LoadType<UnitType>(unitTypeElement, unitTypesByName, unitTypesByID))
								   .ToArray();
		}

		public IEnumerable<BuildingType> LoadAllBuildingTypes() {
			CheckIfLoading();

			BuildingIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"buildingIconTexturePath")));

			var buildingTypesElement = data.Root.Element(PackageManager.XMLNamespace + "buildingTypes");

			if (buildingTypesElement == null) {
				//There are no building types in this package
				return Enumerable.Empty<BuildingType>();
			}
			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return buildingTypesElement.Elements(PackageManager.XMLNamespace + "buildingType")
									   .Select(buildingTypeElement =>
												   LoadType<BuildingType>(buildingTypeElement,
																		  buildingTypesByName,
																		  buildingTypesByID))
									   .ToArray();
		}

		public IEnumerable<ProjectileType> LoadAllProjectileTypes() {
			CheckIfLoading();

			var projectileTypesElement = data.Root.Element(PackageManager.XMLNamespace + "projectileTypes");

			if (projectileTypesElement == null) {
				//There are no projectile types in this package
				return Enumerable.Empty<ProjectileType>();
			}
			//ended by ToArray because i dont want the Linq expression to be enumerated multiple times
			return projectileTypesElement.Elements(PackageManager.XMLNamespace + "projectileType")
										 .Select(projectileTypeElement =>
												   LoadType<ProjectileType>(projectileTypeElement,
																			projectileTypesByName,
																			projectileTypesByID))
										 .ToArray();
		}

		public IEnumerable<ResourceType> LoadAllResourceTypes() {
			CheckIfLoading();

			ResourceIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"resourceIconTexturePath")));

			var resourceTypesElement = data.Root.Element(PackageManager.XMLNamespace + "resourceTypes");

			if (resourceTypesElement == null) {
				return Enumerable.Empty<ResourceType>();
			}

			return resourceTypesElement.Elements(PackageManager.XMLNamespace + "resourceType")
									   .Select(resourceTypeElement =>
												   LoadType<ResourceType>(resourceTypeElement,
																		  resourceTypesByName,
																		  resourceTypesByID))
									   .ToArray();
		}

		public IEnumerable<PlayerType> LoadAllPlayerTypes()
		{
			CheckIfLoading();

			PlayerIconTexture =
				PackageManager.GetTexture2D(XmlHelpers.GetPath(data.Root.Element(PackageManager.XMLNamespace +
																				"playerIconTexturePath")));

			XElement playerTypes = data.Root.Element(PackageManager.XMLNamespace + "playerAITypes");

			if (playerTypes == null) {
				return Enumerable.Empty<PlayerType>();
			}

			return playerTypes.Elements(PackageManager.XMLNamespace + "playerAIType")
							.Select(playerTypeElement =>
										LoadType<PlayerType>(playerTypeElement,
															playerAITypesByName,
															playerAITypesByID))
							.ToArray();
		}

		public void UnLoad() {

			foreach (var unitType in unitTypesByName.Values) {
				unitType.Dispose();
			}

			foreach (var buildingType in buildingTypesByName.Values) {
				buildingType.Dispose();
			}

			foreach (var projectileType in projectileTypesByName.Values) {
				projectileType.Dispose();
			}

			tileTypesByName = null;
			unitTypesByName = null;
			buildingTypesByName = null;
			projectileTypesByName = null;
			resourceTypesByName = null;

			tileTypesByID = null;
			unitTypesByID = null;
			buildingTypesByID = null;
			projectileTypesByID = null;
			resourceTypesByID = null;
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

		XElement GetXmlTypeDescription(string typeName ,string groupName, string itemName) {
			//Load from file
			var typeElements = (from element in data.Root
													.Element(PackageManager.XMLNamespace + groupName)
													.Elements(PackageManager.XMLNamespace + itemName)
										  where GetTypeName(element) == typeName
										  select element).GetEnumerator();

			return GetXmlTypeDescription(typeElements);
		}

		XElement GetXmlTypeDescription(int typeID, string groupName, string itemName) {
			var typeElements = (from element in data.Root
													.Element(PackageManager.XMLNamespace + groupName)
													.Elements(PackageManager.XMLNamespace + itemName)
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

		void CheckIfLoading() {
			if (data == null) {
				throw new InvalidOperationException("Before loading things, you need to call StartLoading");
			}
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
					  string groupName,
					  string itemName,
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
					  string groupName,
					  string itemName,
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

	}
}
