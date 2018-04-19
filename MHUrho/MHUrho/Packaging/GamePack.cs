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

namespace MHUrho.Packaging {
	public class GamePack {
		private const string defaultThumbnailPath = "Textures/xamarin.png";

		private const string TileTypeGroupName = "tileTypes";
		private const string TileTypeItemName = "tileType";
		private const string UnitTypeGroupName = "unitTypes";
		private const string UnitTypeItemName = "unitType";
		private const string BuildingTypeGroupName = "buildingTypes";
		private const string BuildingTypeItemName = "buildingType";
		private const string ProjectileTypeGroupName = "projectileTypes";
		private const string ProjectileTypeItemName = "projectileType";
		private const string ResourceTypeGroupName = "resourceTypes";
		private const string ResourceTypeItemName = "resourceType";

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


		private readonly string pathToXml;

	   
		private Dictionary<string, TileType> tileTypesByName;
		private Dictionary<string, UnitType> unitTypesByName;
		private Dictionary<string, BuildingType> buildingTypesByName;
		private Dictionary<string, ProjectileType> projectileTypesByName;
		private Dictionary<string, ResourceType> resourceTypesByName;

		private Dictionary<int, TileType> tileTypesByID;
		private Dictionary<int, UnitType> unitTypesByID;
		private Dictionary<int, BuildingType> buildingTypesByID;
		private Dictionary<int, ProjectileType> projectileTypesByID;
		private Dictionary<int, ResourceType> resourceTypesByID;

		private XDocument data;

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
			var thumbnail = PackageManager.Instance.ResourceCache.GetImage(pathToThumbnail ?? defaultThumbnailPath);

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
			data = XDocument.Load(MyGame.Config.OpenDynamicFile(pathToXml, System.IO.FileMode.Open, System.IO.FileAccess.Read));
			//TODO: Handler and signal that resource pack is in invalid state
			data.Validate(schemas, null);

			tileTypesByName = new Dictionary<string, TileType>();
			unitTypesByName = new Dictionary<string, UnitType>();
			buildingTypesByName = new Dictionary<string, BuildingType>();
			projectileTypesByName = new Dictionary<string, ProjectileType>();
			resourceTypesByName = new Dictionary<string, ResourceType>();

			tileTypesByID = new Dictionary<int, TileType>();
			unitTypesByID = new Dictionary<int, UnitType>();
			buildingTypesByID = new Dictionary<int, BuildingType>();
			projectileTypesByID = new Dictionary<int, ProjectileType>();
			resourceTypesByID = new Dictionary<int, ResourceType>();

		}

		public void FinishLoading() {
			bool deleted = RemoveUnused(tileTypesByName);
			deleted = RemoveUnused(unitTypesByName) || deleted;
			data = null;

			FullyLoaded = !deleted;
		}

		public TileType GetTileType(string name, bool load = false) {
			if (name == null) {
				throw new ArgumentNullException("Name of the tileType cannot be null");
			}

			var found = tileTypesByName.TryGetValue(name, out TileType value);

			if (load && !found) {
				return LoadType(name, TileTypeGroupName, TileTypeItemName, tileTypesByName, tileTypesByID);
			}
			else {
				return value;
			}
		}

		public TileType GetTileType(int ID, bool load = false) {
			var found = tileTypesByID.TryGetValue(ID, out TileType value);

			if (load && !found) {
				return LoadType(ID, TileTypeGroupName, TileTypeItemName, tileTypesByName, tileTypesByID);
			}
			else {
				return value;
			}
		}

		public UnitType GetUnitType(string name, bool load = false) {
			if (name == null) {
				throw new ArgumentNullException("Name of the unitType cannot be null");
			}

			var found = unitTypesByName.TryGetValue(name, out UnitType value);

			if (load && !found) {
				return LoadType(name, UnitTypeGroupName, UnitTypeItemName, unitTypesByName, unitTypesByID);
			}
			else {
				return value;
			}
		}

		public UnitType GetUnitType(int ID, bool load = false) {
			var found = unitTypesByID.TryGetValue(ID, out UnitType value);

			if (load && !found) {
				return LoadType(ID, UnitTypeGroupName, UnitTypeItemName, unitTypesByName, unitTypesByID);
			}
			else {
				return value;
			}
		}

		public BuildingType GetBuildingType(string name, bool load = false) {
			if (name == null) {
				throw new ArgumentNullException("Name of the buildingType cannot be null");
			}

			var found =  buildingTypesByName.TryGetValue(name, out BuildingType value);

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
			var found = buildingTypesByID.TryGetValue(ID, out BuildingType value);

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
				throw new ArgumentNullException("Name of the projectileType cannot be null");
			}

			var found = projectileTypesByName.TryGetValue(name, out ProjectileType value);

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
			var found = projectileTypesByID.TryGetValue(ID, out ProjectileType value);

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
				throw new ArgumentNullException("Name of the ResourceType cannot be null");
			}

			var found = resourceTypesByName.TryGetValue(name, out ResourceType value);

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
			var found = resourceTypesByID.TryGetValue(ID, out ResourceType value);

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

		public void Load(XmlSchemaSet schemas) {
			StartLoading(schemas);

			LoadAllTileTypes();
			LoadAllUnitTypes();
			LoadAllBuildingTypes();
			LoadAllProjectileTypes();
			LoadAllResourceTypes();
		}


		public IEnumerable<TileType> LoadAllTileTypes() {
			CheckIfLoading();

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

			var resourceTypesElement = data.Root.Element(PackageManager.XMLNamespace + "resourceTypes");

			if (resourceTypesElement == null) {
				return Enumerable.Empty<ResourceType>();
			}

			return resourceTypesElement.Elements(PackageManager.XMLNamespace + "projectileType")
									   .Select(resourceTypeElement =>
												   LoadType<ResourceType>(resourceTypeElement,
																		  resourceTypesByName,
																		  resourceTypesByID))
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
		private bool RemoveUnused<T>(IDictionary<string,T> dictionary)
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

		private XElement GetXmlTypeDescription(string typeName ,string groupName, string itemName) {
			//Load from file
			var typeElements = (from element in data.Root
													.Element(PackageManager.XMLNamespace + groupName)
													.Elements(PackageManager.XMLNamespace + itemName)
										  where GetTypeName(element) == typeName
										  select element).GetEnumerator();

			return GetXmlTypeDescription(typeElements);
		}

		private XElement GetXmlTypeDescription(int typeID, string groupName, string itemName) {
			var typeElements = (from element in data.Root
													.Element(PackageManager.XMLNamespace + groupName)
													.Elements(PackageManager.XMLNamespace + itemName)
								where GetTypeID(element) == typeID
								select element).GetEnumerator();
			return GetXmlTypeDescription(typeElements);
		}

		private XElement GetXmlTypeDescription(IEnumerator<XElement> typeElements) {
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

		private void CheckIfLoading() {
			if (data == null) {
				throw new InvalidOperationException("Before loading things, you need to call StartLoading");
			}
		}

		

		private static string GetTypeName(XElement typeElement) {
			return typeElement.Attribute("name").Value;
		}

		private static int GetTypeID(XElement typeElement) {
			return XmlHelpers.GetIntAttribute(typeElement, "ID");
		}

		private T LoadType<T>(XElement typeElement, IDictionary<string, T> typesByName, IDictionary<int, T> typesByID)
			where T : IEntityType, new() {
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

		private T LoadType<T>(int ID,
							  string groupName,
							  string itemName,
							  IDictionary<string, T> typesByName,
							  IDictionary<int, T> typesByID)
			where T : IEntityType, new()
		{
			CheckIfLoading();

			var typeElement = GetXmlTypeDescription(ID,
													groupName,
													itemName);

			return LoadType(typeElement, typesByName, typesByID);
		}

		private T LoadType<T>(string name,
							  string groupName,
							  string itemName,
							  IDictionary<string, T> typesByName,
							  IDictionary<int, T> typesByID) 
			where T: IEntityType, new()
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
