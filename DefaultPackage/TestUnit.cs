using System;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.UnitComponents;
using MHUrho.Storage;
using Urho;

namespace DefaultPackage
{
    public class TestUnit : IUnitPlugin
    {
        private LevelManager level;
        private Node unitNode;
        private Unit unit;
        

        public TestUnit() {

        }

        private TestUnit(LevelManager level, Node unitNode, Unit unit) {
            this.level = level;
            this.unitNode = unitNode;
            this.unit = unit;
        }

        public bool IsMyUnitType(string unitTypeName) {
            return unitTypeName == "TestUnit";
        }

        public void OnUpdate(float timeStep) {
            
        }

        public bool Order(ITile tile) {
            return false;
        }

        public IUnitPlugin CreateNewInstance(LevelManager level, Node unitNode, Unit unit) {
            unitNode.AddComponent(new WorldWalker(level));
            unitNode.AddComponent(new UnitSelector(unit, level));
            return new TestUnit(level, unitNode, unit);
        }

        public IUnitPlugin LoadNewInstance(LevelManager level, 
                                           Node unitNode, 
                                           Unit unit,
                                           PluginDataWrapper pluginDataStorage) {
            return new TestUnit(level, unitNode, unit);
        }

        public void SaveState(PluginDataWrapper pluginDataStorage) {

        }

    }
}
