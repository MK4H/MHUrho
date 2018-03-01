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
    public class PackageManager
    {
        public static PackageManager Instance { get; private set; }

        public ResourceCache ResourceCache { get; private set; }

        /// <summary>
        /// Path to the schema for Resource Pack Directory xml files
        /// </summary>
        private static readonly string ResPacDirSchemaPath = Path.Combine("Data","Schemas","ResourcePack.xsd");

        public int TileTypeCount => activeTileTypes.Count;

        public IEnumerable<TileType> TileTypes => activeTileTypes.Values;

        private readonly XmlSchemaSet schemas;

        private readonly Dictionary<string, ResourcePack> availablePacks = new Dictionary<string, ResourcePack>();

        private Dictionary<int, ResourcePack> activePackages;

        private Dictionary<int, TileType> activeTileTypes;

        private Dictionary<int, UnitType> activeUnitTypes;

        //private Dictionary<int, BuildingType> activeTileTypes;

        private readonly Random rng;

        public static void CreateInstance(ResourceCache resourceCache) {
            Instance = new PackageManager(resourceCache);
        }

        public StPackages Save() {
            var storedPackages = new StPackages();
            var storedActivePackages = storedPackages.Packages;
            foreach (var activePackage in activePackages) {
                storedActivePackages.Add(new StPackage {ID = activePackage.Key, Name = activePackage.Value.Name});
            }

            var storedActiveTileTypes = storedPackages.TileTypes;
            foreach (var activeTileType in activeTileTypes) {
                storedActiveTileTypes.Add(activeTileType.Value.Save());
            }

            var storedActiveUnitTypes = storedPackages.UnitTypes;
            foreach (var activeUnitType in activeUnitTypes) {
                storedActiveUnitTypes.Add(activeUnitType.Value.Save());
            }

            return storedPackages;
        }

        public void LoadPackages(StPackages storedPackages) {
            //Remap everything from LevelLocal IDs to Global names so we can check if there are already things loaded
            Dictionary<string, TileType> loadedTileTypes = RemapToFullName(activeTileTypes);
            Dictionary<string, UnitType> loadedUnitTypes = RemapToFullName(activeUnitTypes);

            //Load the packages for this level, if already loaded just remap the ID
            Dictionary<int, ResourcePack>
                newActivePackages = GetActivePackages(storedPackages.Packages, activePackages);

            //Unload the packages that were not remapped for this leve
            UnloadUnusedPackages(activePackages);

            //Load the items from packages
            {
                StartLoadingPackages(newActivePackages.Values);

                foreach (var storedTileType in storedPackages.TileTypes) {

                    TileType tileType;
                    //If already loaded, just get it
                    if (!loadedTileTypes.TryGetValue(GetFullName(storedTileType.PackageID, storedTileType.Name),
                                                    out tileType)) {
                        //Was not loaded, load it from package
                        tileType = activePackages[storedTileType.PackageID].GetTileType(storedTileType.Name, storedTileType.TileTypeID);
                    }
                    activeTileTypes.Add( storedTileType.TileTypeID, tileType);
                }


                FinishLoadingPackages(newActivePackages.Values);
            }
            
        }


        protected PackageManager(ResourceCache resourceCache)
        {
            this.rng = new Random();
            this.ResourceCache = resourceCache;

            schemas = new XmlSchemaSet();
            try
            {

                schemas.Add("http://www.MobileHold.cz/ResourcePack.xsd", XmlReader.Create(MyGame.Config.GetStaticFileRO(ResPacDirSchemaPath)));
            }
            catch (IOException e)
            {
                Log.Write(LogLevel.Error,string.Format("Error loading ResroucePack schema: {0}",e));
                if (Debugger.IsAttached) Debugger.Break();
                //Reading of static file of this app failed, something is horribly wrong, die
                //TODO: Error reading static data of app
            }
           
            foreach (var path in MyGame.Config.PackagePaths)
            {
                ParseResourcePackDir(path);
            }
        }


        public TileType GetTileType(int ID) {
            //TODO: React if it does not exist
            return activeTileTypes[ID];
        }

        public UnitType GetUnitType(int ID) {
            //TODO: React if it does not exist
            return activeUnitTypes[ID];
        }

        public ResourcePack GetResourcePack(int ID) {
            //TODO: React if it does not exist
            return activePackages[ID];
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
                XDocument doc = XDocument.Load(MyGame.Config.GetDynamicFile(path));
                doc.Validate(schemas, null);

                loadedPacks = from packages in doc.Root.Elements("resourcePack")
                    select ResourcePack.InitialLoad(
                        packages.Attribute("name")?.Value,
                        packages.Element("path")?.Value,
                        packages.Element("description")?.Value,
                        packages.Element("thumbnailPath")?.Value);
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

        private int GetID<T>(IDictionary<int, T> dictionary) {
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
            return string.Concat(activePackages[packageID].Name, "/", name);
        }

        private static string GetFullName(string packageName, string name) {
            return string.Concat(packageName, "/", name);
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
                if (!availablePacks.ContainsKey(storedPackage.Name)) {
                    throw new Exception($"Package {storedPackage.Name} not available");
                }


                ResourcePack package = availablePacks[storedPackage.Name];
                //Delete package from previously active
                loadedPackages.Remove(package.ID);
                package.AddToByID(storedPackage.ID, newActivePackages);

            }

            return newActivePackages;
        }

        private void UnloadUnusedPackages(Dictionary<int, ResourcePack> toUnload) {
            foreach (var package in toUnload) {
                package.Value.UnLoad(activeTileTypes, activeUnitTypes);
            }
        }

        private void StartLoadingPackages(IEnumerable<ResourcePack> packages) {
            foreach (var package in packages) {
                package.StartLoading(schemas);
            }
        }

        private void FinishLoadingPackages(IEnumerable<ResourcePack> packages) {
            foreach (var package in packages) {
                package.FinishLoading();
            }
        }

    }
}
