using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.Helpers;
using Urho;
using Urho.Resources;

namespace MHUrho.Logic
{
    public class BuildingType : IEntityType, IDisposable
    {
        //XML ELEMENTS AND ATTRIBUTES
        private const string NameAttribute = "name";
        private const string ModelPathElement = "modelPath";
        private const string IconPathElement = "iconPath";
        private const string AssemblyPathElement = "assemblyPath";
        private const string SizeElement = "size";

        public int ID { get; set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        public Model Model { get; private set; }

        public Image Icon { get; private set; }

        public IntVector2 Size { get; private set; }

        private IBuildingTypePlugin buildingTypeLogic;

        protected BuildingType(string name,
                               Model model,
                               IBuildingTypePlugin buildingTypeLogic,
                               Image icon,
                               ResourcePack package) {
            this.Name = name;
            this.Model = model;
            this.buildingTypeLogic = buildingTypeLogic;
            this.Icon = icon;
            this.Package = package;
        }

        /// <summary>
        /// Data has to be loaded after constructor by <see cref="Load(XElement, int, ResourcePack)"/>
        /// It is done this way to allow cyclic references during the Load method, so anything 
        /// that references this buildingType back can get the reference during the loading of this instance
        /// </summary>
        public BuildingType() {

        }

        public void Load(XElement xml, int newID, ResourcePack package) {
            ID = newID;
            Name = xml.Attribute(NameAttribute).Value;
            //TODO: Join the implementations from all the 
            Model = LoadModel(xml, package.XmlDirectoryPath);
            Icon = LoadIcon(xml, package.XmlDirectoryPath);
            Package = package;
            Size = XmlHelpers.GetIntVector2(xml, SizeElement);
            buildingTypeLogic = XmlHelpers.LoadTypePlugin<IBuildingTypePlugin>(xml,
                                                                               AssemblyPathElement,
                                                                               package.XmlDirectoryPath,
                                                                               Name);
            buildingTypeLogic.Initialize(xml.Element(PackageManager.XMLNamespace + "extension"),
                                                    package.PackageManager);
        }

        public StEntityType Save() {
            return new StEntityType {
                                          Name = Name,
                                          TypeID = ID,
                                          PackageID = Package.ID
                                      };
        }

        public Building BuildNewBuilding(int buildingID, Node buildingNode, LevelManager level, IntVector2 topLeftLocation, IPlayer player) {
            throw new NotImplementedException();
        }

        public Building LoadBuilding() {
            throw new NotImplementedException();
        }

        public bool CanBuildAt(IntVector2 centerLocation) {
            return buildingTypeLogic.CanBuildAt(centerLocation);
        }

        public IBuildingInstancePlugin GetNewInstancePlugin(Building building, LevelManager level) {
            return buildingTypeLogic.CreateNewInstance(level, building);
        }

        public IBuildingInstancePlugin GetInstancePluginForLoading() {
            return buildingTypeLogic.GetInstanceForLoading();
        }

        public void Dispose() {
            Model?.Dispose();
            Icon?.Dispose();
        }

        private static Model LoadModel(XElement buildingTypeXml, string pathToPackageXmlDir) {
            string modelPath = XmlHelpers.GetFullPath(buildingTypeXml, ModelPathElement, pathToPackageXmlDir); 
            return PackageManager.Instance.ResourceCache.GetModel(modelPath);
        }

        private static Image LoadIcon(XElement buildingTypeXml, string pathToPackageXmlDir) {
            string iconPath = XmlHelpers.GetFullPath(buildingTypeXml, IconPathElement, pathToPackageXmlDir);
            return PackageManager.Instance.ResourceCache.GetImage(iconPath);
        }

    }
}
