using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.Plugins;
using MHUrho.UnitComponents;
using Urho;
using Urho.Resources;

namespace MHUrho.Logic
{
    public class UnitType : IIDNameAndPackage, IDisposable
    {
        //XML ELEMENTS AND ATTRIBUTES
        private const string NameAttribute = "name";
        private const string ModelPathElement = "modelPath";
        private const string IconPathElement = "iconPath";
        private const string AssemblyPathElement = "assemblyPath";

        public int ID { get; set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        public Model Model { get; private set; }

        public Image Icon { get; private set; }

        HashSet<TileType> passableTileTypes;

        private readonly List<DefaultComponent> components;

        private IUnitTypePlugin unitTypeLogic;

        //TODO: More loaded properties

        protected UnitType(string name, Model model, IUnitTypePlugin unitPlugin, Image icon, ResourcePack package) {
            this.Name = name;
            this.Model = model;
            this.unitTypeLogic = unitPlugin;
            this.Package = package;
            this.Icon = icon;
            components = new List<DefaultComponent>();
        }

        public static UnitType Load(XElement xml, int newID, string pathToPackageXMLDirname, ResourcePack package) {
            //TODO: Check for errors
            string name = xml.Attribute(NameAttribute).Value;
            var model = LoadModel(xml, pathToPackageXMLDirname);
            var icon = LoadIcon(xml, pathToPackageXMLDirname);
            var unitPluginLogic = 
                XmlHelpers.LoadTypePlugin<IUnitTypePlugin>(xml, 
                                                           AssemblyPathElement,
                                                           pathToPackageXMLDirname,
                                                           name);
        
            UnitType newUnitType = new UnitType(name, model, unitPluginLogic, icon, package) 
                                        {
                                            ID = newID
                                        };

            return newUnitType;
        }

        public StEntityType Save() {
            var storedUnitType = new StEntityType {
                Name = Name,
                TypeID = ID,
                PackageID = Package.ID
            };

            return storedUnitType;
        }

        public bool CanPass(TileType tileType)
        {
            return passableTileTypes.Contains(tileType);
        }

        /// <summary>
        /// Creates new instance of this unit type positioned at <paramref name="tile"/>
        /// </summary>
        /// <param name="unitID">identifier unique between units</param>
        /// <param name="unitNode">scene node of the new unit</param>
        /// <param name="level">Level where the unit is being created</param>
        /// <param name="tile">tile where the unit will spawn</param>
        /// <param name="player">owner of the unit</param>
        /// <returns>New unit of this type</returns>
        public Unit CreateNewUnit(int unitID, 
                                  Node unitNode, 
                                  LevelManager level, 
                                  ITile tile, 
                                  IPlayer player) {
            var unit = Unit.CreateNew(unitID, unitNode, this, level, tile, player);
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
        /// <param name="level"></param>
        /// <param name="unitNode">scene node representing the new unit</param>
        /// <param name="storedUnit"></param>
        /// <returns>Unit in first stage of loading, needs to be followed by <see cref="Unit.ConnectReferences"/> and
        /// <see cref="Unit.FinishLoading"/></returns>
        public Unit LoadUnit(LevelManager level, Node unitNode, StUnit storedUnit) {
            var unit = Unit.Load(level, this, unitNode, storedUnit);
            AddComponents(unitNode);
            return unit;
        }

        public IUnitInstancePlugin GetNewInstancePlugin(Unit unit, LevelManager level) {
            return unitTypeLogic.CreateNewInstance(level, unit.Node, unit);
        }

        public IUnitInstancePlugin LoadInstancePlugin(Unit unit,
                                                      LevelManager level,
                                                      PluginData pluginData) {
            return unitTypeLogic.LoadNewInstance(level, unit.Node, unit, new PluginDataWrapper(pluginData));
        }

        public void Dispose() {
            //TODO: Release all disposable resources
            Model.Dispose();
        }

        /// <summary>
        /// Adds components according to the XML file
        /// </summary>
        /// <param name="unitNode"></param>
        private void AddComponents(Node unitNode) {
            //TODO: READ FROM XML
            //TODO: Animated model
            var staticModel = unitNode.CreateComponent<StaticModel>();
            staticModel.Model = Model;
            staticModel.CastShadows = true;


            //TODO: Add needed components
        }

        private static Model LoadModel(XElement unitTypeXml, string pathToPackageXmlDir) {
            //TODO: Check for errors

            string modelPath = XmlHelpers.GetFullPath(unitTypeXml, ModelPathElement, pathToPackageXmlDir);

            return PackageManager.Instance.ResourceCache.GetModel(modelPath);
        }

        private static Image LoadIcon(XElement unitTypeXml, string pathToPackageXmlDir) {
            string iconPath = XmlHelpers.GetFullPath(unitTypeXml, IconPathElement, pathToPackageXmlDir);

            //TODO: Find a way to not need RGBA conversion
            return PackageManager.Instance.ResourceCache.GetImage(iconPath).ConvertToRGBA();
        }

        private static void LoadComponents(XElement unitTypeXml, List<DefaultComponent> components) {
            unitTypeXml.Element(PackageManager.XMLNamespace + "components")
        }
    }
}
