using System.Collections.Generic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {
    public interface IPlayer {
        
        int ID { get; }

        IReadOnlyList<Unit> GetUnitsOfType(UnitType type);

        IReadOnlyList<Building> GetBuildingsOfType(BuildingType type);

        int GetResourcesOfType(ResourceType type);
    }
}