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
        private const string MaterialPathElement = "materialPath";
        private const string IconPathElement = "iconPath";
        private const string AssemblyPathElement = "assemblyPath";
        private const string SizeElement = "size";

        public int ID { get; set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        public Model Model { get; private set; }

        public Material Material { get; private set; }

        public Image Icon { get; private set; }

        public IntVector2 Size { get; private set; }

        private IBuildingTypePlugin buildingTypeLogic;


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
            Material = LoadMaterial(xml, package.XmlDirectoryPath);
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

        public Building BuildNewBuilding(int buildingID, 
                                         Node buildingNode, 
                                         ILevelManager level, 
                                         IntVector2 topLeft, 
                                         IPlayer player) {
            buildingNode.Scale = new Vector3(Size.X, 3, Size.Y);
            var building = Building.BuildAt(buildingID, topLeft, this, buildingNode, player, level);

            //TODO: Probably add animatedModel before creating building instance, and pass the model to the BuildAt method to control the animations from plugin
            AddComponents(buildingNode);

            return building;
        }

        public Building LoadBuilding() {
            throw new NotImplementedException();
        }


        public bool CanBuildIn(IntVector2 topLeft, IntVector2 bottomRight, ILevelManager level) {
            return buildingTypeLogic.CanBuildIn(topLeft, bottomRight, level);
        }

        public bool CanBuildIn(IntRect buildingTilesRectangle, ILevelManager level) {
            return CanBuildIn(buildingTilesRectangle.TopLeft(), buildingTilesRectangle.BottomRight(), level);
        }
 
        public IBuildingInstancePlugin GetNewInstancePlugin(Building building, ILevelManager level) {
            return buildingTypeLogic.CreateNewInstance(level, building);
        }

        public IBuildingInstancePlugin GetInstancePluginForLoading() {
            return buildingTypeLogic.GetInstanceForLoading();
        }

        public IntRect GetBuildingTilesRectangle(IntVector2 topLeft) {
            return new IntRect(topLeft.X,
                               topLeft.Y,
                               topLeft.X + Size.X - 1,
                               topLeft.Y + Size.Y - 1);
        }


        public void Dispose() {
            Model?.Dispose();
            Icon?.Dispose();
        }

        private static Model LoadModel(XElement buildingTypeXml, string pathToPackageXmlDir) {
            string modelPath = XmlHelpers.GetFullPath(buildingTypeXml, ModelPathElement, pathToPackageXmlDir); 
            return PackageManager.Instance.ResourceCache.GetModel(modelPath);
        }

        private static Material LoadMaterial(XElement buildingTypeXml, string pathToPackageXmlDir) {
            string materialPath = XmlHelpers.GetFullPath(buildingTypeXml, MaterialPathElement, pathToPackageXmlDir);
            return PackageManager.Instance.ResourceCache.GetMaterial(materialPath);
        }

        private static Image LoadIcon(XElement buildingTypeXml, string pathToPackageXmlDir) {
            string iconPath = XmlHelpers.GetFullPath(buildingTypeXml, IconPathElement, pathToPackageXmlDir);
            return PackageManager.Instance.ResourceCache.GetImage(iconPath);
        }

        private void AddComponents(Node buildingNode) {
            var staticModel = buildingNode.CreateComponent<StaticModel>();
            staticModel.Model = Model;
            staticModel.Material = Material;
            staticModel.CastShadows = true;
        }

    }
}
