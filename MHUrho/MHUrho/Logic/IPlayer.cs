using System.Collections.Generic;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Control {
    public interface IPlayer {
        
        int ID { get; }

        StPlayer Save();

        void ConnectReferences(LevelManager level);

        void FinishLoading();

        void AddUnit(Unit unit);

        void RemoveUnit(Unit unit);
    }
}