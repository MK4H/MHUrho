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
        /// <summary>
        /// Path to the schema for Resource Pack Directory xml files
        /// </summary>
        private static readonly string ResPacDirSchemaPath = Path.Combine("Data","Schemas","ResourcePack.xsd");

        private readonly ResourceCache cache;

        private readonly ConfigManager configManager;

        private readonly Dictionary<string, ResourcePack> availablePacks = new Dictionary<string, ResourcePack>();

        private Dictionary<int, ResourcePack> activePackages;

        private Dictionary<int, TileType> activeTileTypes;

        private Dictionary<int, UnitType> activeUnitTypes;

        //private Dictionary<int, BuildingType> activeTileTypes;

        private readonly Random rng;

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

        public void LoadForLevel(StPackages storedPackages) {
            //Remap everything from LevelLocal IDs to Global names so we can check if there are already things loaded
            Dictionary<string, TileType> loadedTileTypes = RemapToFullName(activeTileTypes);
            Dictionary<string, UnitType> loadedUnitTypes = RemapToFullName(activeUnitTypes);

            GetNewActiveDictionaries();

            foreach (var package in storedPackages.Packages) {
                if (!availablePacks.ContainsKey(package.Name)) {
                    throw new Exception($"Package {package.Name} not available");
                }

                activePackages.Add(
                    package.ID,
                    loadedPackages.TryGetValue(package.Name, out ResourcePack loadedPackage)
                        ? loadedPackage
                        : availablePacks[package.Name]);
            }

            foreach (var tileType in storedPackages.TileTypes) {
                
                activeTileTypes.Add(
                    tileType.TileTypeID,
                    loadedTileTypes.TryGetValue(tileType.))
            }

        }

        public PackageManager(ResourceCache cache, ConfigManager config)
        {
            this.cache = cache;
            this.configManager = config;
            this.rng = new Random();

            var schema = new XmlSchemaSet();
            try
            {

                schema.Add("http://www.MobileHold.cz/ResourcePack.xsd", XmlReader.Create(config.GetStaticFileRO(ResPacDirSchemaPath)));
            }
            catch (IOException e)
            {
                Log.Write(LogLevel.Error,string.Format("Error loading ResroucePack schema: {0}",e));
                if (Debugger.IsAttached) Debugger.Break();
                //Reading of static file of this app failed, something is horribly wrong, die
                //TODO: Error reading static data of app
            }
           
            foreach (var path in config.PackagePaths)
            {
                ParseResourcePackDir(path, schema);
            }
        }


        /// <summary>
        /// Pulls data about the resource packs contained in this directory from XML file
        /// </summary>
        /// <param name="path">Path to the XML file of Resource pack directory</param>
        /// <param name="schema">Schema for the resource pack directory type of XML files</param>
        /// <returns>True if successfuly read, False if there was an error while loading</returns>
        void ParseResourcePackDir(string path, XmlSchemaSet schema)
        {

            IEnumerable<ResourcePack> loadedPacks = null;

            try
            {
                XDocument doc = XDocument.Load(configManager.GetDynamicFile(path));
                doc.Validate(schema, null);

                loadedPacks = from packages in doc.Root.Elements("resourcePack")
                    select ResourcePack.InitialLoad(
                        cache,
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
                byName.Add(string.Concat(byID.Value.Package.Name,"/",byID.Value.Name), byID.Value);
            }

            return byName;
        }

        private void GetNewActiveDictionaries() {
            activePackages = new Dictionary<int, ResourcePack>();
            activeTileTypes = new Dictionary<int, TileType>();
            activeUnitTypes = new Dictionary<int, UnitType>();
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
    }
}
