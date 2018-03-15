using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using MHUrho.Logic;
using Urho;
using Urho.Resources;

namespace MHUrho.Packaging {
    public class ResourcePack {
        private const string defaultThumbnailPath = "Textures/xamarin.png";

        public delegate int GenerateID();

        public string Name { get; private set; }
        
        public int ID { get; private set; }

        public string Description { get; private set; }

        public Image Thumbnail { get; private set; }

        public bool FullyLoaded { get; private set; }

        public bool IsActive => ID != 0;

        private readonly string pathToXml;

        private Dictionary<string, TileType> tileTypes;
        private Dictionary<string, UnitType> unitTypes;

        private XDocument data;

        /// <summary>
        /// Loads data for initial resource pack managment, so the user can choose which resource packs to 
        /// use, which to download, which to delete and so on
        /// 
        /// Before using this resource pack in game, you need to call LoadAll(...)
        /// </summary>
        /// <param name="cache">ResourceCache to store the thumbnail in</param>
        /// <param name="name">Name of the resource pack</param>
        /// <param name="id">Unique id of this pack in the currently active game</param>
        /// <param name="pathToXml">Path to the resource pack XML description</param>
        /// <param name="description">Human readable description of the resource pack contents for the user</param>
        /// <param name="pathToThumbnail">Path to thumbnail to display</param>
        /// <returns>Initialized resource pack</returns>
        public static ResourcePack InitialLoad( string name,
                                                string pathToXml, 
                                                string description, 
                                                string pathToThumbnail) {
            pathToXml = FileManager.CorrectRelativePath(pathToXml);
            pathToThumbnail = FileManager.CorrectRelativePath(pathToThumbnail);
            var thumbnail = PackageManager.Instance.ResourceCache.GetImage(pathToThumbnail ?? defaultThumbnailPath);

            return new ResourcePack(name, pathToXml, description ?? "No description", thumbnail);
        }

        protected ResourcePack(string name, string pathToXml, string description, Image thumbnail) {
            this.Name = name;
            this.pathToXml = pathToXml;
            this.Description = description;
            this.Thumbnail = thumbnail;
            this.FullyLoaded = false;

            tileTypes = new Dictionary<string, TileType>();
            unitTypes = new Dictionary<string, UnitType>();
        }

        public void StartLoading(XmlSchemaSet schemas) {
            ResetIDs(tileTypes.Values);
            ResetIDs(unitTypes.Values);

            data = XDocument.Load(MyGame.Config.OpenDynamicFile(pathToXml, System.IO.FileMode.Open, System.IO.FileAccess.Read));
            //TODO: Handler and signal that resource pack is in invalid state
            data.Validate(schemas, null);
        }

        public void FinishLoading() {
            bool deleted = RemoveUnused(tileTypes);
            deleted = RemoveUnused(unitTypes) || deleted;
            data = null;

            FullyLoaded = !deleted;
        }

        public TileType GetTileType(string name) {
            if (name == null) {
                throw new ArgumentNullException("Name of the tileType cannot be null");
            }

            return tileTypes.TryGetValue(name,out TileType value) ? value : null;
        }

        public TileType LoadTileType(string name, int newID) {
            if (data == null) {
                throw new InvalidOperationException("Before loading things, you need to call StartLoading");
            }

            if (name == null) {
                throw new ArgumentNullException("Name of the tileType cannot be null");
            }

            TileType tileType;
            if (!tileTypes.TryGetValue(name, out tileType)) {
                //Load from file
                var tileTypeElements = (from element in data
                                                       .Root.Element(PackageManager.XMLNamespace + "tileTypes")
                                                       .Elements(PackageManager.XMLNamespace + "tileType")
                                       where element.Attribute("name").Value == name
                                       select element).GetEnumerator();

                if (!tileTypeElements.MoveNext()) {
                    throw new ArgumentException("TileType of that name does not exist in this package");
                }

                var tileTypeElement = tileTypeElements.Current;

                if (tileTypeElements.MoveNext()) {
                    //TODO: Exception
                    throw new Exception("Duplicate tileType names");
                }

                tileTypeElements.Dispose();

                tileType = TileType.Load(tileTypeElement, newID, System.IO.Path.GetDirectoryName(pathToXml), this);
                tileTypes.Add(name, tileType);
            }
            else {
                //Just change ID
                tileType.ID = newID;
            }

            return tileType;
        }

        public UnitType LoadUnitType(string name, int newID) {
            if (data == null) {
                throw new InvalidOperationException("Before loading things, you need to call StartLoading");
            }

            if (name == null) {
                throw new ArgumentNullException("Name of the unit type cannot be null");
            }

            UnitType unitType;
            if (!unitTypes.TryGetValue(name, out unitType)) {
                //Load from file
                var unitTypeElements = (from element in data
                                                        .Root.Element(PackageManager.XMLNamespace + "unitTypes")
                                                        .Elements(PackageManager.XMLNamespace + "unitType")
                                        where element.Attribute("name").Value == name
                                        select element).GetEnumerator();

                unitTypeElements.MoveNext();
                if (!unitTypeElements.MoveNext()) {
                    throw new ArgumentException("unit type of that name does not exist in this package");
                }

                var unitTypeElement = unitTypeElements.Current;

                if (unitTypeElements.MoveNext()) {
                    //TODO: Exception
                    throw new Exception("Duplicate unit type names");
                }

                unitTypeElements.Dispose();

                unitType = UnitType.Load(unitTypeElement, newID, System.IO.Path.GetDirectoryName(pathToXml), this);
                unitTypes.Add(name, unitType);
            }
            else {
                //Just change ID
                unitType.ID = newID;
            }

            return unitType;
        }

        //TODO: CONVERT THE TWO PRECEDING METHODS TO THIS
        //public T LoadType<T>(string name, int newID) where T:IIDNameAndPackage {
        //    Dictionary<string, T> types;
        //    string xmlGroupName;
        //    string xmlItemName;
        //    Func<XElement, int, string, ResourcePack, T> loadFunc;

        //    if (typeof(T) == typeof(UnitType)) {
        //        types = (Dictionary<string, T>)(object)unitTypes;
        //        xmlGroupName = "unitTypes";
        //        xmlItemName = "unitType";
        //        loadFunc = UnitType.Load;

        //    }
        //    else if (typeof(T) == typeof(TileType)) {
        //        types = (Dictionary<string, T>)(object)tileTypes;
        //        xmlGroupName = "tileTypes";
        //        xmlItemName = "tileType";
        //    }
        //    else {
        //        throw new ArgumentException("Unknown type argument", nameof(T));
        //    }


        //    if (data == null) {
        //        throw new InvalidOperationException("Before loading things, you need to call StartLoading");
        //    }

        //    if (name == null) {
        //        throw new ArgumentNullException($"Name of the {xmlItemName} cannot be null");
        //    }



        //    T type;
        //    if (!types.TryGetValue(name, out type)) {
        //        //Load from file
        //        var typeElements = (from element in data
        //                                                .Root.Element(PackageManager.XMLNamespace + "tileTypes")
        //                                                .Elements(PackageManager.XMLNamespace + "tileType")
        //                                where element.Attribute("name").Value == name
        //                                select element).GetEnumerator();

        //        typeElements.MoveNext();
        //        if (!typeElements.MoveNext()) {
        //            throw new ArgumentException("TileType of that name does not exist in this package");
        //        }

        //        var typeElement = typeElements.Current;

        //        if (typeElements.MoveNext()) {
        //            //TODO: Exception
        //            throw new Exception("Duplicate tileType names");
        //        }

        //        typeElements.Dispose();

        //        type = TileType.Load(typeElement, newID, System.IO.Path.GetDirectoryName(pathToXml), this);
        //        tileTypes.Add(name, tileType);
        //    }
        //    else {
        //        //Just change ID
        //        type.ID = newID;
        //    }

        //    return type;
        //}

        public IEnumerable<TileType> LoadAllTileTypes(GenerateID generateId) {
            if (data == null) {
                throw new InvalidOperationException("Before loading things, you need to call StartLoading");
            }

            List<TileType> loadedTileTypes = new List<TileType>();

            var tileTypeElements = from elements in data.Root.Element(PackageManager.XMLNamespace + "tileTypes").Elements(PackageManager.XMLNamespace + "tileType") select elements;

            foreach (var tileTypeElement in tileTypeElements) {
                string name = tileTypeElement.Attribute("name").Value;

                TileType loadedTileType;
                if (tileTypes.TryGetValue(name, out loadedTileType)) {
                    loadedTileType.ID = generateId();
                    loadedTileTypes.Add(loadedTileType);
                    continue;
                }

                loadedTileType = TileType.Load(tileTypeElement, generateId(), System.IO.Path.GetDirectoryName(pathToXml), this);

                tileTypes.Add(loadedTileType.Name, loadedTileType);
                loadedTileTypes.Add(loadedTileType);
            }

            return loadedTileTypes;
        }

        public IEnumerable<UnitType> LoadAllUnitTypes(GenerateID generateID) {
            //TODO: make this and LoadAllTileTypes into one method, this is just lazy
            if (data == null) {
                throw new InvalidOperationException("Before loading things, you need to call StartLoading");
            }

            List<UnitType> loadedUnitTypes = new List<UnitType>();

            var unitTypeElements = from elements in data.Root.Element(PackageManager.XMLNamespace + "unitTypes").Elements(PackageManager.XMLNamespace + "unitType") select elements;

            foreach (var unitTypeElement in unitTypeElements) {
                string name = unitTypeElement.Attribute("name").Value;

                UnitType loadedUnitType;
                if (unitTypes.TryGetValue(name, out loadedUnitType)) {
                    //Was already loaded for previous game, generate new ID for this game
                    loadedUnitType.ID = generateID();
                }
                else {
                    //Load it fresh from the xml
                    loadedUnitType = UnitType.Load(unitTypeElement, generateID(), System.IO.Path.GetDirectoryName(pathToXml), this);

                    unitTypes.Add(loadedUnitType.Name, loadedUnitType);
                }
               
                loadedUnitTypes.Add(loadedUnitType);
            }

            return loadedUnitTypes;
        }

        /// <summary>
        /// Provides "atomic" change of ID and addition to dictionary with this ID
        /// </summary>
        /// <param name="newID"></param>
        /// <param name="dictionary"></param>
        public void AddToByID(int newID, Dictionary<int, ResourcePack> dictionary) {
            this.ID = newID;
            dictionary.Add(newID, this);
        }

        public void UnLoad(Dictionary<int, TileType> activeTileTypes, Dictionary<int, UnitType> activeUnitTypes) {

            foreach (var unitType in unitTypes) {
                unitType.Value.Dispose();
            }

            tileTypes = null;
            unitTypes = null;
        }

        private void ResetIDs<T>(IEnumerable<T> enumerable)
            where T: IIDNameAndPackage {

            foreach (var item in enumerable) {
                item.ID = 0;
            }
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
    }
}
