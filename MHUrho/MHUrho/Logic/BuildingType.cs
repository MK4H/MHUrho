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
    public class BuildingType : IIDNameAndPackage, IDisposable
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

        public static BuildingType Load(XElement xml, int newID, string pathToPackageXmlDir, ResourcePack package) {
            string name = xml.Attribute(NameAttribute).Value;
            //TODO: Join the implementations from all the 
            var model = LoadModel(xml, pathToPackageXmlDir);
            var icon = LoadIcon(xml, pathToPackageXmlDir);
            var size = XmlHelpers.GetIntVector2(xml, SizeElement);
            var buildingTypeLogic = XmlHelpers.LoadTypePlugin<IBuildingTypePlugin>(xml, AssemblyPathElement, pathToPackageXmlDir, name);

            var newBuildingType = new BuildingType(name, model, buildingTypeLogic, icon, package) {
                                                                                                      ID = newID
                                                                                                  };

            return newBuildingType;
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

        public bool CanBuildAt(IntVector2 topLeftLocation) {
            return buildingTypeLogic.CanBuildAt(topLeftLocation);
        }

        public IBuildingInstancePlugin GetNewInstancePlugin(Building building, LevelManager level) {
            return buildingTypeLogic.CreateNewInstance(level, building.Node, building);
        }

        public IBuildingInstancePlugin LoadInstancePlugin(Building building, LevelManager level, PluginData pluginData) {
            return buildingTypeLogic.LoadNewInstance(level, building.Node, building, new PluginDataWrapper(pluginData));
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
