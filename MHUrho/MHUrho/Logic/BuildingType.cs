using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;
using Urho.Resources;

namespace MHUrho.Logic
{
    public class BuildingType : IDisposable
    {
        public int ID { get; set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        public Model Model { get; private set; }

        public Image Icon { get; private set; }

        public IBuildingPlugin BuildingLogic { get; private set; }

        private IntVector2 size;

        public StBuildingType Save() {
            return new StBuildingType {
                                          Name = Name,
                                          BuildingTypeID = ID,
                                          PackageID = Package.ID
                                      };
        }

        public Building BuildNewBuilding(int buildingID, Node buildingNode, LevelManager level, IntVector2 topLeftLocation, IPlayer player) {

        }

        public Building LoadBuilding() {

        }

        public bool CanBuildAt(IntVector2 topLeftLocation) {
            return BuildingLogic.CanBuildAt(topLeftLocation);
        }

        public void Dispose() {
            Model?.Dispose();
            Icon?.Dispose();
        }
    }
}
