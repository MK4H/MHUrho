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
using Urho;
using Urho.Resources;

namespace MHUrho.Logic
{
    public class UnitType : IEntityType, IDisposable
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

        private IUnitTypePlugin unitTypeLogic;

        //TODO: More loaded properties

        protected UnitType(string name, Model model, IUnitTypePlugin unitPlugin, Image icon, ResourcePack package) {
            this.Name = name;
            this.Model = model;
            this.unitTypeLogic = unitPlugin;
            this.Package = package;
            this.Icon = icon;
        }

        /// <summary>
        /// Data has to be loaded after constructor by <see cref="Load(XElement, int, ResourcePack)"/>
        /// It is done this way to allow cyclic references during the Load method, so anything 
        /// that references this unitType back can get the reference during the loading of this instance
        /// </summary>
        public UnitType() {

        }

        /// <summary>
        /// Loads the standard data of the unitType from the xml
        /// 
        /// THE STANDARD DATA cannot reference any other types, it would cause infinite cycles
        /// 
        /// After this loading, you should register this type so it can be referenced, and then call
        /// <see cref="UnitType.ParseExtensionData(XElement, ResourcePack)"/>
        /// </summary>
        /// <param name="xml">xml element describing the type, according to <see cref="PackageManager.XMLNamespace"/> schema</param>
        /// <param name="newID">ID of this type in the current game</param>
        /// <param name="package">Package this unitType belongs to</param>
        /// <returns>UnitType with filled standard members</returns>
        public void Load(XElement xml, int newID, ResourcePack package) {
            //TODO: Check for errors
            ID = newID;
            Name = xml.Attribute(NameAttribute).Value;
            Model = LoadModel(xml, package.XmlDirectoryPath);
            Icon = LoadIcon(xml, package.XmlDirectoryPath);
            Package = package;
            unitTypeLogic = 
                XmlHelpers.LoadTypePlugin<IUnitTypePlugin>(xml, 
                                                           AssemblyPathElement,
                                                           package.XmlDirectoryPath,
                                                           Name);

            unitTypeLogic.Initialize(xml.Element(PackageManager.XMLNamespace + "extension"),
                                                package.PackageManager);
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
    }
}
