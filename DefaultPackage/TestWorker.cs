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

        public IUnitInstancePlugin CreateNewInstance(LevelManager level, Unit unit) {
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

        public TestWorkerInstance(LevelManager level, Unit unit) {

        }

        public TestWorkerInstance() {

        }

        public void OnUpdate(float timeStep) {
            if (WorkedBuilding == null) {
                throw new InvalidOperationException("TestWorker has no building");
            }


        }

        public void SaveState(PluginDataWrapper pluginData) {
            var indexedData = pluginData.GetWriterForWrappedIndexedData();
            indexedData.Store(1, WorkedBuilding.Building.ID);
        }

        public void LoadState(LevelManager level, Unit unit, PluginDataWrapper pluginData) {
            var indexedData = pluginData.GetReaderForWrappedIndexedData();
            WorkedBuilding = (TestBuildingInstance)level.GetBuilding(indexedData.Get<int>(1)).Plugin;
        }
    }
}
