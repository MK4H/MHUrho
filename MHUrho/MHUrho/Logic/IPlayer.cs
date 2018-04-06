using System.Collections.Generic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {
    public interface IPlayer {
        
        int ID { get; }

        StPlayer Save();

        void ConnectReferences(LevelManager level);

        void FinishLoading();

        void AddUnit(Unit unit);

        void AddBuilding(Building building);

        bool RemoveUnit(Unit unit);
    }
}