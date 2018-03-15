using System;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.UnitComponents;
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
            unitNode.AddComponent(new WorldWalker(level));
            unitNode.AddComponent(new UnitSelector(unit, level));
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
            return new TestUnit(level, unitNode, unit);
        }

    }
}
