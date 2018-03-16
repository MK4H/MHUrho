using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.Plugins;
using Urho;
using Urho.Resources;

namespace MHUrho.Logic
{
    public class UnitType : IIDNameAndPackage, IDisposable
    {

        public int ID { get; set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        public Model Model { get; private set; }

        public Image Icon { get; private set; }

        public IUnitPlugin UnitLogic { get; private set; }
        HashSet<string> passableTileTypes;

        //TODO: More loaded properties

        protected UnitType(string name, Model model, IUnitPlugin unitPlugin, Image icon, ResourcePack package) {
            this.Name = name;
            this.Model = model;
            this.UnitLogic = unitPlugin;
            this.Package = package;
            this.Icon = icon;
        }

        public static UnitType Load(XElement xml, int newID, string pathToPackageXMLDirname, ResourcePack package) {
            //TODO: Check for errors
            string name = xml.Attribute("name").Value;
            var model = LoadModel(xml, pathToPackageXMLDirname);
            var icon = LoadIcon(xml, pathToPackageXMLDirname);
            var unitPluginLogic = LoadUnitPlugin(xml, pathToPackageXMLDirname, name);
        
            UnitType newUnitType = new UnitType(name, model, unitPluginLogic, icon, package) 
                                        {
                                            ID = newID
                                        };

            return newUnitType;
        }

        public StUnitType Save() {
            var storedUnitType = new StUnitType();
            storedUnitType.Name = Name;
            storedUnitType.UnitTypeID = ID;
            storedUnitType.PackageID = Package.ID;

            return storedUnitType;
        }

        public bool CanPass(string tileType)
        {
            return passableTileTypes.Contains(tileType);
        }

        /// <summary>
        /// Creates new instance of this unit type positioned at <paramref name="tile"/>
        /// </summary>
        /// <param name="unitID">identifier unique between units</param>
        /// <param name="unitNode">scene node of the new unit</param>
        /// <param name="tile">tile where the unit will spawn</param>
        /// <param name="player">owner of the unit</param>
        /// <returns>New unit of this type</returns>
        public Unit CreateNewUnit(int unitID, Node unitNode, ITile tile, IPlayer player) {
            var unit = Unit.CreateNew(unitID, unitNode, this, tile, player);
            AddComponents(unitNode);

            return unit;
        }

        /// <summary>
        /// Does first stage of loading a new instance of <see cref="Unit"/> from <paramref name="storedUnit"/> and adds it
        /// to the <paramref name="unitNode"/>
        /// 
        /// Also adds all other components needed by this <see cref="UnitType"/>
        /// Needs to be followed by <see cref="Unit.ConnectReferences"/> and then <see cref="Unit.FinishLoading"/>
        /// </summary>
        /// <param name="unitNode">scene node representing the new unit</param>
        /// <param name="storedUnit"></param>
        /// <returns>Unit in first stage of loading, needs to be followed by <see cref="Unit.ConnectReferences"/> and
        /// <see cref="Unit.FinishLoading"/></returns>
        public Unit LoadUnit(Node unitNode, StUnit storedUnit) {
            var unit = Unit.Load(this, unitNode, storedUnit);
            AddComponents(unitNode);
            return unit;
        }

        public void Dispose() {
            //TODO: Release all disposable resources
            Model.Dispose();
        }

        private void AddComponents(Node unitNode) {
            //TODO: Animated model
            var staticModel = unitNode.CreateComponent<StaticModel>();
            staticModel.Model = Model;
            staticModel.CastShadows = true;


            //TODO: Add needed components
        }

        private static Model LoadModel(XElement unitTypeXml, string pathToPackageXmlDir) {
            //TODO: Check for errors

            string relativeModelPath = unitTypeXml.Element(PackageManager.XMLNamespace + "modelPath").Value.Trim();
            relativeModelPath = FileManager.CorrectRelativePath(relativeModelPath);
            string modelPath = System.IO.Path.Combine(pathToPackageXmlDir, relativeModelPath);

            return PackageManager.Instance.ResourceCache.GetModel(modelPath);
        }

        private static Image LoadIcon(XElement unitTypeXml, string pathToPackageXmlDir) {
            string relativeIconPath = unitTypeXml.Element(PackageManager.XMLNamespace + "iconPath").Value.Trim();
            relativeIconPath = FileManager.CorrectRelativePath(relativeIconPath);
            string iconPath = System.IO.Path.Combine(pathToPackageXmlDir, relativeIconPath);

            //TODO: Find a way to not need RGBA conversion
            return PackageManager.Instance.ResourceCache.GetImage(iconPath).ConvertToRGBA();
        }

        private static IUnitPlugin LoadUnitPlugin(XElement unitTypeXml, string pathToPackageXmlDir, string unitTypeName) {
            string relativeAssemblyPath = unitTypeXml.Element(PackageManager.XMLNamespace + "assemblyPath").Value.Trim();
            //Fix / or \ in the path
            relativeAssemblyPath = FileManager.CorrectRelativePath(relativeAssemblyPath);

            var assembly = Assembly.LoadFile(System.IO.Path.Combine(MyGame.Config.DynamicDirPath, 
                                                                    pathToPackageXmlDir, 
                                                                    relativeAssemblyPath));

            var unitPlugins = from type in assembly.GetTypes()
                              where typeof(IUnitPlugin).IsAssignableFrom(type)
                              select type;
            IUnitPlugin pluginInstance = null;
            foreach (var plugin in unitPlugins) {
                pluginInstance = (IUnitPlugin)Activator.CreateInstance(plugin);
                if (pluginInstance.IsMyUnitType(unitTypeName)) {
                    break;
                }
            }

            if (pluginInstance == null) {
                //TODO: Exception
                throw new Exception("Unit type loading failed, could not load unit plugin");
            }

            return pluginInstance;
        }
    }
}
