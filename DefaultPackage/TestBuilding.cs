using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;
using WorkQueue = MHUrho.UnitComponents.WorkQueue;

namespace DefaultPackage
{
    public class TestBuildingType : IBuildingTypePlugin
    {
        private UnitType workerType;
        private TileType tileType;

        public TestBuildingType() {

        }

        public bool IsMyType(string typeName) {
            return typeName == "TestBuilding";
        }

        public IBuildingInstancePlugin CreateNewInstance(ILevelManager level, Building building) {
            Unit[] workers = new Unit[2];
            workers[0] = level.SpawnUnit(workerType,
                                         level.Map.GetTileByTopLeftCorner(building.Rectangle.TopLeft() + new IntVector2(0, -1)),
                                         building.Player);
            workers[0].Node.AddComponent(new WorkQueue());
            workers[1] = level.SpawnUnit(workerType,
                                         level.Map.GetTileByTopLeftCorner(building.Rectangle.TopLeft() + new IntVector2(-1, 0)),
                                         building.Player);
            workers[1].Node.AddComponent(new WorkQueue());

            return new TestBuildingInstance(level, building, workers);
        }

        public IBuildingInstancePlugin GetInstanceForLoading() {
            return new TestBuildingInstance();
        }


        public bool CanBuildIn(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, ILevelManager level) {
            bool empty = true;
            int rightTileTypeCount = 0;
            level.Map.ForEachInRectangle(topLeftTileIndex, 
                                         bottomRightTileIndex, 
                                         (tile) => {

                                                        if (tile.Unit != null ||
                                                            tile.PassingUnits.Count != 0 ||
                                                            tile.Building != null) {

                                                            empty = false;

                                                        }

                                                        if (tile.Type == tileType) {
                                                            rightTileTypeCount++;
                                                        }
                                                    });
            return empty && rightTileTypeCount > 5;
        }


        public void PopulateUI(MandKUI mouseAndKeyboardUI) {
            throw new NotImplementedException();
        }

        public void ClearUI(MandKUI mouseAndKeyboardUI) {
            throw new NotImplementedException();
        }

        public void PopulateUI(TouchUI touchUI) {
            throw new NotImplementedException();
        }

        public void ClearUI(TouchUI touchUI) {
            throw new NotImplementedException();
        }

        public void AddSelected(IBuildingInstancePlugin buildingInstance) {
            throw new NotImplementedException();
        }

        public void RemoveSelected(IBuildingInstancePlugin buildingInstance) {
            throw new NotImplementedException();
        }

        public void Initialize(XElement extensionElement, PackageManager packageManager) {
            workerType = PackageManager.Instance
                                       .LoadUnitType(XmlHelpers.GetString(extensionElement,
                                                                          "workerType"));
            tileType = PackageManager.Instance.LoadTileType(XmlHelpers.GetString(extensionElement, "tileType"));
        }
    }

    public class TestBuildingInstance : IBuildingInstancePlugin {

        public Building Building { get; private set; }

        private ILevelManager level;
        private TestWorkerInstance[] workers;

        private int resources;

        private const float timeBetweenResourceSpawns = 5;

        private float timeToNextResource = timeBetweenResourceSpawns;

        public TestBuildingInstance() {

        }

        public TestBuildingInstance(ILevelManager level, Building building, Unit[] workers) {
            this.level = level;

            this.Building = building;
            this.workers = new TestWorkerInstance[workers.Length];

            for (int i = 0; i < workers.Length; i++) {
                this.workers[i] = (TestWorkerInstance)workers[i].Plugin;
                this.workers[i].WorkedBuilding = this;
            }
        }

        public void OnUpdate(float timeStep) {
            timeToNextResource -= timeStep;
            if (timeToNextResource < 0) {
                resources++;
            }

            if (resources > 0) {
                
            }
        }

        public void SaveState(PluginDataWrapper pluginData) {
            
        }

        public void LoadState(ILevelManager level, Building building, PluginDataWrapper pluginData) {
            
        }

        public ITile GetInterfaceTile(TestWorkerInstance testWorker) {
            for (int i = 0; i < workers.Length; i++) {
                if (testWorker == workers[i]) {
                    return level.Map.GetTileByBottomRightCorner(Building.Rectangle.TopLeft() +
                                                                new IntVector2(0, i % 4));
                }
            }
            return null;
        }

        //private void StartWorker(WorkQueue worker) {
         
        //    worker.EnqueueTask(new WorkQueue.DelegatedWorkTask()
        //                           .OnTaskStarted((unit, task) => {
        //                                              unit.GetComponent<WorldWalker>()
        //                                                  .OnMovementFinishedCall((_) => task.Finish())
        //                                                  .OnMovementFailedCall((_) => task.Finish())
        //                                                  .GoTo(new IntVector2(10, 10));
        //                                          }));

        //    worker.EnqueueTask(new WorkQueue.TimedWorkTask(5));

        //    worker.EnqueueTask(new WorkQueue.DelegatedWorkTask()
        //                           .OnTaskStarted((unit, task) => {
        //                                              unit.GetComponent<WorldWalker>()
        //                                                  .OnMovementFinishedCall((_) => task.Finish())
        //                                                  .OnMovementFailedCall((_) => task.Finish())
        //                                                  .GoTo(building.GetExchangeTile(unit));
        //                                          })
        //                       .OnTaskFinished((unit, task) => { StartWorker(worker); }));
        //}

    }
}
