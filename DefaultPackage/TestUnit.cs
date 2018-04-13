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
    public class TestUnitType : UnitTypePluginBase {

        public UnitTypeInitializationData TypeData => new UnitTypeInitializationData();

        private ProjectileType projectileType;

        public override bool IsMyType(string unitTypeName) {
            return unitTypeName == "TestUnit";
        }

        public TestUnitType() {

        }

        

        public override UnitInstancePluginBase CreateNewInstance(ILevelManager level, Unit unit) {
            var unitNode = unit.Node;
            unitNode.AddComponent(new WorldWalker(level));
            unitNode.AddComponent(new UnitSelector(level));
            unitNode.AddComponent(new Shooter(level,
                                                    unitNode.Position +
                                                    new Vector3(0,
                                                                0,
                                                                10),
                                                    projectileType,
                                                    10,
                                                    1,
                                                    1));
            return new TestUnitInstance(level, unit);
        }

        public override UnitInstancePluginBase GetInstanceForLoading() {
            return new TestUnitInstance();
        }


        public override bool CanSpawnAt(ITile centerTile) {
            return true;
        }

        public override void Initialize(XElement extensionElement, PackageManager packageManager) {
            projectileType = PackageManager.Instance
                                           .ActiveGame
                                           .GetProjectileType(XmlHelpers.GetString(extensionElement, 
                                                                                   "projectileType"),
                                                              true);
        }
    }

    public class TestUnitInstance : UnitInstancePluginBase
    {
        private ILevelManager level;
        private Node unitNode;
        private Unit unit;
        private WorldWalker walker;

        public TestUnitInstance() {

        }

        public TestUnitInstance(ILevelManager level, Unit unit) {
            this.level = level;
            this.unitNode = unit.Node;
            this.unit = unit;
            this.walker = unit.GetComponent<WorldWalker>();
            var selector = unitNode.GetComponent<UnitSelector>();
            selector.OrderedToTile += SelectorOrderedToTile;
        }

        private void SelectorOrderedToTile(Unit unit, ITile targetTile, OrderArgs orderArgs) {
            orderArgs.Executed = walker.GoTo(targetTile);
        }

        public override void SaveState(PluginDataWrapper pluginDataStorage) {

        }

        public override void LoadState(ILevelManager level,Unit unit, PluginDataWrapper pluginData) {
            this.level = level;
            this.unit = unit;
            this.unitNode = unit.Node;
            walker = unitNode.GetComponent<WorldWalker>();
            var selector = unitNode.GetComponent<UnitSelector>();
            selector.OrderedToTile += SelectorOrderedToTile;
        }

        public override bool CanGoFromTo(ITile fromTile, ITile toTile) {
            return toTile.Building == null;
        }
    }
}
