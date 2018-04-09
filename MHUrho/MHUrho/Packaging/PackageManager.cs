using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using Urho.IO;
using Urho.Resources;
using Path = System.IO.Path;

namespace MHUrho.Packaging
{
    /// <summary>
    /// ResourceCache wrapper providing loading, unloading and downloading of ResourcePacks
    /// </summary>
    public class PackageManager {
        public static XNamespace XMLNamespace = "http://www.MobileHold.cz/ResourcePack.xsd";

        public static PackageManager Instance { get; private set; }

        public ResourceCache ResourceCache { get; private set; }

        /// <summary>
        /// Path to the schema for Resource Pack Directory xml files
        /// </summary>
        private static readonly string ResPacDirSchemaPath = Path.Combine("Data","Schemas","ResourcePack.xsd");

        public int TileTypeCount => activeTileTypes.Count;

        public IEnumerable<TileType> TileTypes => activeTileTypes.Values;

        public int UnitTypeCount => activeUnitTypes.Count;

        public IEnumerable<UnitType> UnitTypes => activeUnitTypes.Values;

        public int BuildingTypeCount => activeBuildingTypes.Count;

        public IEnumerable<BuildingType> BuildingTypes => activeBuildingTypes.Values;

        public int ProjectileTypeCount => activeProjectileTypes.Count;

        public IEnumerable<ProjectileType> ProjectileTypes => activeProjectileTypes.Values;

        public TileType DefaultTileType { get; private set; }

        private readonly XmlSchemaSet schemas;

        private readonly Dictionary<string, ResourcePack> availablePacks = new Dictionary<string, ResourcePack>();

        private Dictionary<int, ResourcePack> activePackages;

        private Dictionary<int, ResourcePack> loadingPackages;

        private Dictionary<int, TileType> activeTileTypes;

        private Dictionary<int, UnitType> activeUnitTypes;

        private Dictionary<int, BuildingType> activeBuildingTypes;

        private Dictionary<int, ProjectileType> activeProjectileTypes;

        private readonly Random rng;

        public static void CreateInstance(ResourceCache resourceCache) {
            Instance = new PackageManager(resourceCache);
            try {

                Instance.schemas.Add(XMLNamespace.NamespaceName, XmlReader.Create(MyGame.Config.OpenStaticFileRO(ResPacDirSchemaPath)));
            }
            catch (IOException e) {
                Log.Write(LogLevel.Error, string.Format("Error loading ResourcePack schema: {0}", e));
                if (Debugger.IsAttached) Debugger.Break();
                //Reading of static file of this app failed, something is horribly wrong, die
                //TODO: Error reading static data of app
            }

            foreach (var path in MyGame.Config.PackagePaths) {
                Instance.ParseResourcePackDir(path);
            }
        }

        public StPackages Save() {
            var storedPackages = new StPackages();
            var storedActivePackages = storedPackages.Packages;
            foreach (var activePackage in activePackages) {
                storedActivePackages.Add(new StPackage {ID = activePackage.Key, Name = activePackage.Value.Name});
            }

            foreach (var activeTileType in activeTileTypes) {
                storedPackages.TileTypes.Add(activeTileType.Value.Save());
            }

            foreach (var activeUnitType in activeUnitTypes) {
                storedPackages.UnitTypes.Add(activeUnitType.Value.Save());
            }

            foreach (var activeBuildingType in activeBuildingTypes) {
                storedPackages.BuildingTypes.Add(activeBuildingType.Value.Save());
            }

            foreach (var activeProjectileType in activeProjectileTypes) {
                storedPackages.ProjectileTypes.Add(activeProjectileType.Value.Save());
            }

            return storedPackages;
        }

        public void LoadPackages(StPackages storedPackages) {
            //Remap everything from LevelLocal IDs to Global names so we can check if there are already things loaded
            Dictionary<string, TileType> loadedTileTypes = RemapToFullName(activeTileTypes);
            Dictionary<string, UnitType> loadedUnitTypes = RemapToFullName(activeUnitTypes);
            Dictionary<string, BuildingType> loadedBuildingTypes = RemapToFullName(activeBuildingTypes);
            Dictionary<string, ProjectileType> loadedProjectileTypes = RemapToFullName(activeProjectileTypes);

            ClearActiveTypes();

            //Load the packages for this level, if already loaded just remap the ID
            loadingPackages = GetActivePackages(storedPackages.Packages, activePackages);

            //Load the items from packages
            {
                StartGroupLoadingPackages(loadingPackages.Values);

                LoadTileTypes(storedPackages.TileTypes, loadedTileTypes);
                LoadUnitTypes(storedPackages.UnitTypes, loadedUnitTypes);
                LoadBuildingTypes(storedPackages.BuildingTypes, loadedBuildingTypes);
                LoadProjectileTypes(storedPackages.ProjectileTypes, loadedProjectileTypes);

                FinishGroupLoadingPackages();
            }
        }

        /// <summary>
        /// Loads the packages and the default package and gives them new IDs
        /// </summary>
        /// <param name="packages"></param>
        public void LoadWholePackages(IEnumerable<string> packages) {
            ClearActiveTypes();
            loadingPackages = new Dictionary<int, ResourcePack>();

            //Packages from method argument
            List<ResourcePack> givenPackages = new List<ResourcePack>();
            
            //Get default pack
            int defaultPackageID = GetNewID(loadingPackages);
            givenPackages.Add(GetAndMovePackage("Default", defaultPackageID, activePackages, loadingPackages));

            foreach (var package in packages) {
                givenPackages.Add(GetAndMovePackage(package, GetNewID(loadingPackages), activePackages, loadingPackages));
            }

            StartGroupLoadingPackages(loadingPackages.Values);

            //If it was not loaded, load it with new ID, else just leave it with old ID
            if ((DefaultTileType = loadingPackages[defaultPackageID].GetTileType("Default")) == null) {
                DefaultTileType = loadingPackages[defaultPackageID].LoadTileType("Default", GetNewID(activeTileTypes));
            }
 
            foreach (var package in givenPackages) {
                AddToActive(package.LoadAllTileTypes(() => GetNewID(activeTileTypes)), activeTileTypes);
                AddToActive(package.LoadAllUnitTypes(() => GetNewID(activeUnitTypes)), activeUnitTypes);
                AddToActive(package.LoadAllBuildingTypes(() => GetNewID(activeBuildingTypes)), activeBuildingTypes);
                AddToActive(package.LoadAllProjectileTypes(() => GetNewID(activeProjectileTypes)), activeProjectileTypes);

            }

            FinishGroupLoadingPackages();

        }

        protected PackageManager(ResourceCache resourceCache)
        {
            this.rng = new Random();
            this.ResourceCache = resourceCache;

            schemas = new XmlSchemaSet();
            activePackages = new Dictionary<int, ResourcePack>();
            activeTileTypes = new Dictionary<int, TileType>();
            activeUnitTypes = new Dictionary<int, UnitType>();
            activeBuildingTypes = new Dictionary<int, BuildingType>();
            activeProjectileTypes = new Dictionary<int, ProjectileType>();
        }

        public TileType GetTileType(int ID) {
            //TODO: React if it does not exist
            return activeTileTypes[ID];
        }

        public TileType LoadTileType(string fullName) {
            GetPackageAndItemName(fullName, out string packageName, out string tileTypeName);

            var resourcePack = availablePacks[packageName];
            StartLoadingPackage(resourcePack);

            TileType tileType;

            if ((tileType = resourcePack.GetTileType(tileTypeName)) != null) {
                if (tileType.ID == 0) {
                    tileType.ID = GetNewID(activeTileTypes);
                    activeTileTypes.Add(tileType.ID, tileType);
                }
            }
            else {
                tileType = resourcePack.LoadTileType(tileTypeName, GetNewID(activeTileTypes));
                activeTileTypes.Add(tileType.ID, tileType);
            }

            EndLoadingPackage(resourcePack);
            return tileType;
        }

        public UnitType GetUnitType(int ID) {
            //TODO: React if it does not exist
            return activeUnitTypes[ID];
        }

        public UnitType LoadUnitType(string fullName) {
            GetPackageAndItemName(fullName, out string packageName, out string unitTypeName);

            var resourcePack = availablePacks[packageName];
            StartLoadingPackage(resourcePack);

            UnitType unitType;

            if ((unitType = resourcePack.GetUnitType(unitTypeName)) != null) {
                if (unitType.ID == 0) {
                    unitType.ID = GetNewID(activeUnitTypes);
                    activeUnitTypes.Add(unitType.ID, unitType);
                }
            }
            else {
                unitType = resourcePack.LoadUnitType(unitTypeName, GetNewID(activeUnitTypes));
                activeUnitTypes.Add(unitType.ID, unitType);
            }

            EndLoadingPackage(resourcePack);
            return unitType;
        }

        public BuildingType GetBuildingType(int ID) {
            //TODO: React if it does not exist
            return activeBuildingTypes[ID];
        }

        public BuildingType LoadBuildingType(string fullName) {
            GetPackageAndItemName(fullName, out string packageName, out string buildingTypeName);

            var resourcePack = availablePacks[packageName];
            StartLoadingPackage(resourcePack);

            BuildingType buildingType;

            if ((buildingType = resourcePack.GetBuildingType(buildingTypeName)) != null) {
                if (buildingType.ID == 0) {
                    buildingType.ID = GetNewID(activeBuildingTypes);
                    activeBuildingTypes.Add(buildingType.ID, buildingType);
                }
            }
            else {
                buildingType = resourcePack.LoadBuildingType(buildingTypeName, GetNewID(activeBuildingTypes));
                activeBuildingTypes.Add(buildingType.ID, buildingType);
            }

            EndLoadingPackage(resourcePack);
            return buildingType;
        }

        public ProjectileType GetProjectileType(int ID) {
            //TODO: React if it does not exist
            return activeProjectileTypes[ID];
        }

        public ProjectileType LoadProjectileType(string fullName) {
            GetPackageAndItemName(fullName, out string packageName, out string projectileTypeName);

            var resourcePack = availablePacks[packageName];
            StartLoadingPackage(resourcePack);

            ProjectileType projectileType;

            if ((projectileType = resourcePack.GetProjectileType(projectileTypeName)) != null) {
                if (projectileType.ID == 0) {
                    projectileType.ID = GetNewID(activeProjectileTypes);
                    activeProjectileTypes.Add(projectileType.ID, projectileType);
                }
            }
            else {
                projectileType = resourcePack.LoadProjectileType(projectileTypeName, GetNewID(activeProjectileTypes));
                activeProjectileTypes.Add(projectileType.ID, projectileType);
            }

            EndLoadingPackage(resourcePack);
            return projectileType;
        }

        public ResourcePack GetResourcePack(int ID) {
            //TODO: React if it does not exist
            return activePackages[ID];
        }

        /// <summary>
        /// Returns resource pack of the given name 
        /// </summary>
        /// <param name="name">Name of the resourcepack to return</param>
        /// <param name="onlyActive">Search only the packages loaded for the current level</param>
        /// <returns>Resource pack of the given name or null if none exists</returns>
        public ResourcePack GetResourcePack(string name, bool onlyActive = true) {
            if (!availablePacks.TryGetValue(name, out ResourcePack value)) {
                return null;
            }
            if (onlyActive) {
                return value.IsActive ? value : null;
            }
            return value;
        }
        
        /// <summary>
        /// Pulls data about the resource packs contained in this directory from XML file
        /// </summary>
        /// <param name="path">Path to the XML file of Resource pack directory</param>
        /// <param name="schema">Schema for the resource pack directory type of XML files</param>
        /// <returns>True if successfuly read, False if there was an error while loading</returns>
        private void ParseResourcePackDir(string path)
        {

            IEnumerable<ResourcePack> loadedPacks = null;

            try
            {
                XDocument doc = XDocument.Load(MyGame.Config.OpenDynamicFile(path, System.IO.FileMode.Open, FileAccess.Read));
                doc.Validate(schemas, null);

                string directoryPath = Path.GetDirectoryName(path);

                loadedPacks = from packages in doc.Root.Elements(XMLNamespace + "resourcePack")
                              select ResourcePack.InitialLoad(packages.Attribute("name").Value,
                                                    //PathtoXml is relative to ResourcePackDir.xml directory path
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

        /// <summary>
        /// Creates a map with key being full name of the T parameter
        /// Full name means package-name/item-name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mappedByID">Dictionary of items mapped by their ID</param>
        /// <returns>Dictionary of all items from <paramref name="mappedByID"/> mapped by their full name</returns>
        private static Dictionary<string, T> RemapToFullName<T>(IDictionary<int, T> mappedByID) 
            where T : IIDNameAndPackage
        {
            Dictionary<string, T> byName = new Dictionary<string, T>();
            foreach (var byID in mappedByID) {
                byName.Add(GetFullName(byID.Value.Package.Name, byID.Value.Name), byID.Value);
            }

            return byName;
        }

        private const int MaxTries = 10000;

        private int GetNewID<T>(IDictionary<int, T> dictionary) {
            int id, i = 0;
            while (dictionary.ContainsKey(id = rng.Next())) {
                i++;
                if (i > MaxTries) {
                    //TODO: Exception
                    throw new Exception("Could not find free ID");
                }
            }

            return id;
        }

        private string GetFullName(int packageID, string name) {
            return string.Concat(loadingPackages?[packageID].Name ?? activePackages[packageID].Name, "/", name);
        }

        private static string GetFullName(string packageName, string name) {
            return string.Concat(packageName, "/", name);
        }

        private static void GetPackageAndItemName(string fullName, out string packageName, out string itemName) {
            if (!TryGetPackageAndItemName(fullName, out packageName, out itemName)) {
                throw new ArgumentException($"fullName does not have the right format",nameof(fullName));
            }
        }

        private static bool TryGetPackageAndItemName(string fullName, out string packageName, out string itemName) {
            packageName = null;
            itemName = null;

            var nameParts = fullName.Split('/');

            if (nameParts.Length != 2 || nameParts[0].Length == 0 || nameParts[1].Length == 0) {
                return false;
            }

            packageName = nameParts[0];
            itemName = nameParts[1];
            return true;
        }

        /// <summary>
        /// Creates Dictionary of packages needed for currently the level being loaded,
        /// moves already loaded packages from loadedPackages to returned Dictionary
        /// 
        /// Leaves only unused packages in loadedPackages
        /// </summary>
        /// <param name="neededPackages">packages needed for the new level</param>
        /// <param name="loadedPackages">Packages loaded for previous level, after return unused packages in the new level</param>
        /// <returns>Loaded packages for the new level</returns>
        /// <exception cref="Exception">Level needs package that is not available on this machine</exception>
        private Dictionary<int, ResourcePack> GetActivePackages(    IEnumerable<StPackage> neededPackages,
                                                                    Dictionary<int, ResourcePack> loadedPackages) {


            Dictionary<int, ResourcePack> newActivePackages = new Dictionary<int, ResourcePack>();
            foreach (var storedPackage in neededPackages) {
                GetAndMovePackage(storedPackage.Name, storedPackage.ID, loadedPackages, newActivePackages);
            }

            return newActivePackages;
        }

        /// <summary>
        /// Gets package with <paramref name="name"/>, removes it from <paramref name="currentlyLoaded"/> if it was loaded
        /// and adds it to <paramref name="newLoaded"/>
        /// </summary>
        /// <param name="name">Name of the package to get</param>
        /// <param name="ID">New ID of the package</param>
        /// <param name="currentlyLoaded">packages loaded for previous levels that are loaded</param>
        /// <param name="newLoaded">New loaded packages for current level</param>
        private ResourcePack GetAndMovePackage(    string name, 
                                    int ID, 
                                    Dictionary<int,ResourcePack> currentlyLoaded, 
                                    Dictionary<int, ResourcePack> newLoaded) {
            if (!availablePacks.ContainsKey(name)) {
                throw new Exception($"Package {name} not available");
            }

            ResourcePack package = availablePacks[name];
            //Delete package from previously active
            currentlyLoaded.Remove(package.ID);
            package.AddToByID(ID, newLoaded);
            ResourceCache.AddResourceDir(package.XmlDirectoryPath, 1);
            return package;
        }


        private void UnloadPackages(IEnumerable<ResourcePack> packages) {
            foreach (var package in packages) {
                ResourceCache.RemoveResourceDir(package.XmlDirectoryPath);
                package.UnLoad(activeTileTypes, activeUnitTypes);
            }
        }

        private void StartGroupLoadingPackages(IEnumerable<ResourcePack> packages) {
            foreach (var package in packages) {
                package.StartLoading(schemas);
            }
        }

        private void FinishGroupLoadingPackages() {
            foreach (var package in loadingPackages.Values) {
                package.FinishLoading();
            }

            //Unloads the packages that were left in previously loaded packages and not moved
            // to new loaded packages
            UnloadPackages(activePackages.Values);
            activePackages = loadingPackages;
            loadingPackages = null;
        }

        private void StartLoadingPackage(ResourcePack package) {
            if (loadingPackages == null && !activePackages.ContainsValue(package)) {
                 package.AddToByID(GetNewID(activePackages), activePackages);
                ResourceCache.AddResourceDir(package.XmlDirectoryPath, 1);
            }
            else if (loadingPackages != null && !loadingPackages.ContainsValue(package)) {
                package.AddToByID(GetNewID(loadingPackages), loadingPackages);
                ResourceCache.AddResourceDir(package.XmlDirectoryPath, 1);
            }
        }

        private void EndLoadingPackage(ResourcePack package) {
            if (loadingPackages == null) {
                package.FinishLoading();
            }
        }

        private static void AddToActive<T>(IEnumerable<T> loadedTypes, IDictionary<int, T> activeTypesDictionary)
            where T : IIDNameAndPackage
        {
            foreach (var loadedType in loadedTypes) {
                if (activeTypesDictionary.TryGetValue(loadedType.ID, out var activeType)) {
                    if (activeType.Equals(loadedType)) {
                        //Type was loaded in advance, referenced by other already loaded type
                        continue;
                    }
                    throw new InvalidOperationException("Type was given already used ID");
                }
                else {
                    activeTypesDictionary.Add(loadedType.ID, loadedType);
                }
            }
        }

        /// <summary>
        /// Clears <see cref="activeTileTypes"/>, <see cref="activeUnitTypes"/>, ...
        /// and all their IDs
        /// </summary>
        private void ClearActiveTypes() {
            activeTileTypes = new Dictionary<int, TileType>();
            activeUnitTypes = new Dictionary<int, UnitType>();
            activeBuildingTypes = new Dictionary<int, BuildingType>();
            activeProjectileTypes = new Dictionary<int, ProjectileType>();

            foreach (var package in activePackages.Values) {
                package.ClearIDs();
            }
        }

        private void LoadTileTypes(IEnumerable<StEntityType> storedTileTypes, IDictionary<string, TileType> loadedTileTypes) {
            foreach (var storedTileType in storedTileTypes) {

                TileType tileType;
                //If already loaded, just get it
                if (!loadedTileTypes.TryGetValue(GetFullName(storedTileType.PackageID, storedTileType.Name),
                                                 out tileType)) {
                    //Was not loaded, load it from package
                    tileType = loadingPackages[storedTileType.PackageID].LoadTileType(storedTileType.Name, storedTileType.TypeID);
                }

                //If the type was loaded by some referencing type before this stored loading, just assign it the correct ID
                if (tileType.ID != 0) {
                    activeTileTypes.Remove(tileType.ID);
                }

                tileType.ID = storedTileType.TypeID;
                activeTileTypes.Add(tileType.ID, tileType);
            }
        }

        //TODO: Refactor this and the above to be the same generic method
        private void LoadUnitTypes(IEnumerable<StEntityType> storedUnitTypes, Dictionary<string, UnitType> loadedUnitTypes) {
            foreach (var storedUnitType in storedUnitTypes) {
                //If already loaded, just get it
                if (!loadedUnitTypes.TryGetValue(GetFullName(storedUnitType.PackageID, storedUnitType.Name),
                                                 out UnitType unitType)) {
                    //Was not loaded, load it from package
                    unitType = loadingPackages[storedUnitType.PackageID].LoadUnitType(storedUnitType.Name, storedUnitType.TypeID);
                }

                //If the type was loaded by some referencing type before this stored loading, just assign it the correct ID
                if (unitType.ID != 0) {
                    activeUnitTypes.Remove(unitType.ID);
                }

                unitType.ID = storedUnitType.TypeID;
                activeUnitTypes.Add(unitType.ID, unitType);
            }
        }

        private void LoadBuildingTypes(IEnumerable<StEntityType> storedBuildingTypes,
                                       Dictionary<string, BuildingType> loadedBuildingTypes) {
            foreach (var storedBuildingType in storedBuildingTypes) {
                if (!loadedBuildingTypes.TryGetValue(GetFullName(storedBuildingType.PackageID, storedBuildingType.Name),
                                                     out BuildingType buildingType)) {
                    buildingType = loadingPackages[storedBuildingType.PackageID].LoadBuildingType(storedBuildingType.Name, storedBuildingType.TypeID);
                }

                //If the type was loaded by some referencing type before this stored loading, just assign it the correct ID
                if (buildingType.ID != 0) {
                    activeBuildingTypes.Remove(buildingType.ID);
                }

                buildingType.ID = storedBuildingType.TypeID;
                activeBuildingTypes.Add(buildingType.ID, buildingType);
            }
        }

        private void LoadProjectileTypes(IEnumerable<StEntityType> storedProjectileTypes,
                                       Dictionary<string, ProjectileType> loadedProjectileTypes) {
            foreach (var storedProjectileType in storedProjectileTypes) {
                if (!loadedProjectileTypes.TryGetValue(GetFullName(storedProjectileType.PackageID, storedProjectileType.Name),
                                                     out ProjectileType projectileType)) {
                    projectileType = loadingPackages[storedProjectileType.PackageID]
                        .LoadProjectileType(storedProjectileType.Name, storedProjectileType.TypeID);
                }

                //If the type was loaded by some referencing type before this stored loading, just assign it the correct ID
                if (projectileType.ID != 0) {
                    activeProjectileTypes.Remove(projectileType.ID);
                }

                projectileType.ID = storedProjectileType.TypeID;
                activeProjectileTypes.Add(projectileType.ID, projectileType);
            }
        }
    }
}
