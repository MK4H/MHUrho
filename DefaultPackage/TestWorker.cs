using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace DefaultPackage
{
    public class TestWorkerType : IUnitTypePlugin {
        public UnitTypeInitializationData TypeData => new UnitTypeInitializationData();

        public bool IsMyType(string typeName) {
            return typeName == "TestWorker";
        }

        public IUnitInstancePlugin CreateNewInstance(ILevelManager level, Unit unit) {
            var unitNode = unit.Node;
            unitNode.AddComponent(new WorldWalker(level));
            return new TestWorkerInstance(level, unit);
        }

        public IUnitInstancePlugin GetInstanceForLoading() {
            return new TestWorkerInstance();
        }


        public bool CanSpawnAt(ITile centerTile) {
            return centerTile.Type != PackageManager.Instance.DefaultTileType &&
                   centerTile.Building == null;
        }

        public void Initialize(XElement extensionElement, PackageManager packageManager) {
            
        }
    }

    public class TestWorkerInstance : IUnitInstancePlugin {
        public TestBuildingInstance WorkedBuilding { get; set; }

        private WorldWalker walker;
        private bool homeGoing = false;

        public TestWorkerInstance(ILevelManager level, Unit unit) {
            walker = unit.Node.GetComponent<WorldWalker>();
        }

        public TestWorkerInstance() {

        }

        public void OnUpdate(float timeStep) {
            if (WorkedBuilding == null) {
                throw new InvalidOperationException("TestWorker has no building");
            }

            if (homeGoing && walker.MovementFinished) {
                walker.GoTo(new IntVector2(20,20));
                homeGoing = !homeGoing;
            }
            else if (!homeGoing && walker.MovementFinished) {
                walker.GoTo(WorkedBuilding.GetInterfaceTile(this));
                homeGoing = !homeGoing;
            }
        }

        public void SaveState(PluginDataWrapper pluginData) {
            var indexedData = pluginData.GetWriterForWrappedIndexedData();
            indexedData.Store(1, WorkedBuilding.Building.ID);
            indexedData.Store(2, homeGoing);
        }

        public void LoadState(ILevelManager level, Unit unit, PluginDataWrapper pluginData) {
            var indexedData = pluginData.GetReaderForWrappedIndexedData();
            WorkedBuilding = (TestBuildingInstance)level.GetBuilding(indexedData.Get<int>(1)).Plugin;
            homeGoing = indexedData.Get<bool>(2);
            walker = unit.GetComponent<WorldWalker>();
        }

        public bool CanGoFromTo(ITile fromTile, ITile toTile) {
            return toTile.Building == null;
        }
    }
}
