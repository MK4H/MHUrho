using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic
{
    public class UnitType : IIDNameAndPackage, IDisposable
    {

        public int ID { get; set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        public Model Model { get; private set; }

        HashSet<string> PassableTileTypes;

        //TODO: More loaded properties

        protected UnitType(string name, Model model, ResourcePack package) {
            this.Name = name;
            this.Model = model;
            this.Package = package;
        }

        public static UnitType Load(XElement xml, int newID, string pathToPackageXMLDirname, ResourcePack package) {
            //TODO: Check for errors
            string name = xml.Attribute("name").Value;
            string relativeModelPath = xml.Element(PackageManager.XMLNamespace + "modelPath").Value.Trim();

            relativeModelPath = FileManager.CorrectRelativePath(relativeModelPath);

            string modelPath = System.IO.Path.Combine(pathToPackageXMLDirname, relativeModelPath);
            var model = PackageManager.Instance.ResourceCache.GetModel(modelPath);


            UnitType newUnitType = new UnitType(name, model, package) 
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
            return PassableTileTypes.Contains(tileType);
        }

        public void Dispose() {
            //TODO: Release all disposable resources
            Model.Dispose();
        }

    }
}
