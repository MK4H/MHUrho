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
        private const string IDAttributeName = "ID";
        private const string NameAttributeName = "name";
        private const string ModelPathElementName = "modelPath";
        private const string MaterialPathElementName = "materialPath";
        private const string IconPathElementName = "iconPath";
        private const string AssemblyPathElementName = "assemblyPath";

        public int ID { get; set; }

        public string Name { get; private set; }

        public GamePack Package { get; private set; }

        public Model Model { get; private set; }

        public Material Material { get; private set; }

        public Image Icon { get; private set; }

        public object Plugin => unitTypeLogic;

        private UnitTypePluginBase unitTypeLogic;

        //TODO: More loaded properties

        /// <summary>
        /// Data has to be loaded after constructor by <see cref="Load(XElement, int, GamePack)"/>
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
        /// <see cref="UnitType.ParseExtensionData(XElement, GamePack)"/>
        /// </summary>
        /// <param name="xml">xml element describing the type, according to <see cref="PackageManager.XMLNamespace"/> schema</param>
        /// <param name="newID">ID of this type in the current game</param>
        /// <param name="package">Package this unitType belongs to</param>
        /// <returns>UnitType with filled standard members</returns>
        public void Load(XElement xml, GamePack package) {
            //TODO: Check for errors
            ID = xml.GetIntFromAttribute(IDAttributeName);
            Name = xml.Attribute(NameAttributeName).Value;
            Package = package;

            unitTypeLogic =
                XmlHelpers.LoadTypePlugin<UnitTypePluginBase>(xml,
                                                           AssemblyPathElementName,
                                                           package.XmlDirectoryPath,
                                                           Name);

            var data = unitTypeLogic.TypeData;

            Model = LoadModel(xml, package.XmlDirectoryPath);
            Material = LoadMaterial(xml, package.XmlDirectoryPath);
            Icon = LoadIcon(xml, package.XmlDirectoryPath);
            
            unitTypeLogic.Initialize(xml.Element(PackageManager.XMLNamespace + "extension"),
                                     package.PackageManager);
        }


        public bool CanSpawnAt(ITile tile) {
            return unitTypeLogic.CanSpawnAt(tile);
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
                                  ILevelManager level, 
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
        public Unit LoadUnit(ILevelManager level, Node unitNode, StUnit storedUnit) {
            var unit = Unit.Load(level, this, unitNode, storedUnit);
            AddComponents(unitNode);
            return unit;
        }

        public UnitInstancePluginBase GetNewInstancePlugin(Unit unit, ILevelManager level) {
            return unitTypeLogic.CreateNewInstance(level, unit);
        }

        public UnitInstancePluginBase GetInstancePluginForLoading() {
            return unitTypeLogic.GetInstanceForLoading();
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
            staticModel.Material = Material;
            staticModel.CastShadows = true;
            

            //TODO: Add needed components
        }

        private static Model LoadModel(XElement unitTypeXml, string pathToPackageXmlDir) {
            //TODO: Check for errors

            string modelPath = XmlHelpers.GetFullPath(unitTypeXml, ModelPathElementName, pathToPackageXmlDir);

            return PackageManager.Instance.ResourceCache.GetModel(modelPath);
        }

        private static Material LoadMaterial(XElement unitTypeXml, string pathToPackageXmlDir) {
            string materialPath = XmlHelpers.GetFullPath(unitTypeXml, MaterialPathElementName, pathToPackageXmlDir);

            return PackageManager.Instance.ResourceCache.GetMaterial(materialPath);
        }

        private static Image LoadIcon(XElement unitTypeXml, string pathToPackageXmlDir) {
            string iconPath = XmlHelpers.GetFullPath(unitTypeXml, IconPathElementName, pathToPackageXmlDir);

            //TODO: Find a way to not need RGBA conversion
            return PackageManager.Instance.ResourceCache.GetImage(iconPath).ConvertToRGBA();
        }


    }
}
