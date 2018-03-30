using System;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.UnitComponents;
using MHUrho.Storage;
using Urho;

namespace DefaultPackage
{
    public class TestUnitType : IUnitTypePlugin {
        public bool IsMyUnitType(string unitTypeName) {
            return unitTypeName == "TestUnit";
        }

        public TestUnitType() {

        }

        public IUnitInstancePlugin CreateNewInstance(LevelManager level, Node unitNode, Unit unit) {
            unitNode.AddComponent(new WorldWalker(level));
            unitNode.AddComponent(new UnitSelector(unit, level));
            return new TestUnitInstance(level, unitNode, unit);
        }

        public IUnitInstancePlugin LoadNewInstance(LevelManager level,
                                                   Node unitNode,
                                                   Unit unit,
                                                   PluginDataWrapper pluginDataStorage) {
            return new TestUnitInstance(level, unitNode, unit);
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

        public bool Order(ITile tile) {
            return false;
        }

        

        public void SaveState(PluginDataWrapper pluginDataStorage) {

        }

    }
}
