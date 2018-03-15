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

namespace MHUrho.Logic
{
    public class UnitType : IIDNameAndPackage, IDisposable
    {

        public int ID { get; set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        public Model Model { get; private set; }

        public IUnitPlugin UnitLogic { get; private set; }
        HashSet<string> passableTileTypes;

        //TODO: More loaded properties

        protected UnitType(string name, Model model, IUnitPlugin unitPlugin, ResourcePack package) {
            this.Name = name;
            this.Model = model;
            this.UnitLogic = unitPlugin;
            this.Package = package;
        }

        public static UnitType Load(XElement xml, int newID, string pathToPackageXMLDirname, ResourcePack package) {
            //TODO: Check for errors
            string name = xml.Attribute("name").Value;
            string relativeModelPath = xml.Element(PackageManager.XMLNamespace + "modelPath").Value.Trim();
            string relativeAssemblyPath = xml.Element(PackageManager.XMLNamespace + "assemblyPath").Value.Trim();

            relativeModelPath = FileManager.CorrectRelativePath(relativeModelPath);
            relativeAssemblyPath = FileManager.CorrectRelativePath(relativeAssemblyPath);

            string modelPath = System.IO.Path.Combine(pathToPackageXMLDirname, relativeModelPath);
            var model = PackageManager.Instance.ResourceCache.GetModel(modelPath);

            var assembly = Assembly.LoadFile(System.IO.Path.Combine(pathToPackageXMLDirname, relativeAssemblyPath));

            var unitPlugins = from type in assembly.GetTypes()
                              where typeof(IUnitPlugin).IsAssignableFrom(type)
                              select type;
            IUnitPlugin pluginInstance = null;
            foreach (var plugin in unitPlugins) {
                pluginInstance = (IUnitPlugin)Activator.CreateInstance(plugin);
                if (pluginInstance.IsMyUnitType(name)) {
                    break;
                }
            }

            if (pluginInstance == null) {
                //TODO: Exception
                throw new Exception("Unit type loading failed, could not load unit plugin");
            }

            UnitType newUnitType = new UnitType(name, model, pluginInstance, package) 
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

        public Unit GetNewUnit(Node unitNode, ITile tile, IPlayer player) {
            unitNode.AddComponent(new Unit(this, tile, player));
            unitNode.Position = tile.Center3;
            return unitNode.GetComponent<Unit>();
        }

        public Unit LoadUnit(Node unitNode, StUnit storedUnit) {
            return Logic.Unit.Load(this, unitNode, storedUnit);
        }

        public void Dispose() {
            //TODO: Release all disposable resources
            Model.Dispose();
        }

    }
}
