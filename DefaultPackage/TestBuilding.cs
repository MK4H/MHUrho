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
using Urho;

namespace DefaultPackage
{
    public class TestBuildingType : IBuildingTypePlugin
    {
        private UnitType workerType;

        public TestBuildingType() {

        }

        public bool IsMyType(string typeName) {
            return typeName == "TestBuilding";
        }

        public IBuildingInstancePlugin CreateNewInstance(LevelManager level, Node buildingNode, Building building) {
            Unit[] workers = new Unit[2];
            workers[0] = level.SpawnUnit(workerType, 
                                         level.Map.GetTile(building.Rectangle.TopLeft() + new IntVector2(1, 0)),
                                         building.Player);
            workers[0].Node.AddComponent(new BuildingWorker(building));
            workers[1] = level.SpawnUnit(workerType,
                                         level.Map.GetTile(building.Rectangle.TopLeft() + new IntVector2(0, 1)),
                                         building.Player);
            workers[1].Node.AddComponent(new BuildingWorker(building));

            return new TestBuildingInstance(level, buildingNode, building, workers);
        }

        public IBuildingInstancePlugin LoadNewInstance(LevelManager level, Node buildingNode, Building building,
                                                       PluginDataWrapper pluginData) {
            return new TestBuildingInstance(level, buildingNode, building,new Unit[0]);
        }

        public bool CanBuildAt(IntVector2 topLeftLocation) {
            throw new NotImplementedException();
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
        }
    }

    public class TestBuildingInstance : IBuildingInstancePlugin {
        private LevelManager level;
        private Node buildingNode;
        private Building building;
        private Unit[] workers;

        private int resources;

        private const float timeBetweenResourceSpawns = 5;

        private float timeToNextResource = timeBetweenResourceSpawns;

        public TestBuildingInstance(LevelManager level, Node buildingNode, Building building, Unit[] workers) {
            this.level = level;
            this.buildingNode = buildingNode;
            this.building = building;
            this.workers = workers;

            foreach (var worker in workers) {
                StartWorker(worker.GetComponent<BuildingWorker>());
            }
        }

        public ITile GetExchangeTile(Unit unit) {
            return level.Map.GetTile(building.Location + new IntVector2(1, 0));
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

        private void StartWorker(BuildingWorker worker) {
         
            worker.EnqueueTask(new BuildingWorker.DelegatedWorkTask()
                                   .OnTaskStarted((unit, task) => {
                                                      unit.GetComponent<WorldWalker>()
                                                          .OnMovementFinishedCall((_) => task.Finish())
                                                          .OnMovementFailedCall((_) => task.Finish())
                                                          .GoTo(new IntVector2(10, 10));
                                                  }));

            worker.EnqueueTask(new BuildingWorker.TimedWorkTask(5));

            worker.EnqueueTask(new BuildingWorker.DelegatedWorkTask()
                                   .OnTaskStarted((unit, task) => {
                                                      unit.GetComponent<WorldWalker>()
                                                          .OnMovementFinishedCall((_) => task.Finish())
                                                          .OnMovementFailedCall((_) => task.Finish())
                                                          .GoTo(building.GetExchangeTile(unit));
                                                  })
                               .OnTaskFinished((unit, task) => { StartWorker(worker); }));
        }

    }
}
