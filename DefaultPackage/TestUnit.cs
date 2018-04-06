using System;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.UnitComponents;
using MHUrho.Storage;
using Urho;

namespace DefaultPackage
{
    public class TestUnitType : IUnitTypePlugin {

        private ProjectileType projectileType;

        public bool IsMyType(string unitTypeName) {
            return unitTypeName == "TestUnit";
        }

        public TestUnitType() {

        }

        public IUnitInstancePlugin CreateNewInstance(LevelManager level, Node unitNode, Unit unit) {
            unitNode.AddComponent(new WorldWalker(level));
            unitNode.AddComponent(new UnitSelector(level));
            unitNode.AddComponent(new DirectShooter(level.Map,
                                                    unitNode.Position +
                                                    new Vector3(0,
                                                                0,
                                                                10),
                                                    projectileType,
                                                    10,
                                                    1,
                                                    1));
            return new TestUnitInstance(level, unitNode, unit);
        }

        public IUnitInstancePlugin LoadNewInstance(LevelManager level,
                                                   Node unitNode,
                                                   Unit unit,
                                                   PluginDataWrapper pluginDataStorage) {
            return new TestUnitInstance(level, unitNode, unit);
        }

        public void Initialize(XElement extensionElement, PackageManager packageManager) {
            projectileType = PackageManager.Instance
                                           .LoadProjectileType(XmlHelpers.GetString(extensionElement, 
                                                                                    "projectileType"));
        }
    }

    public class TestUnitInstance : IUnitInstancePlugin
    {
        private LevelManager level;
        private Node unitNode;
        private Unit unit;
        

        public TestUnitInstance(LevelManager level, Node unitNode, Unit unit) {
            this.level = level;
            this.unitNode = unitNode;
            this.unit = unit;
        }

        

        public void OnUpdate(float timeStep) {
            
        }

        public void SaveState(PluginDataWrapper pluginDataStorage) {

        }

    }
}
