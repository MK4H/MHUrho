using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;

namespace MHUrho.Plugins
{
    //TODO: Either make one instance of this for every unit,
    // OR make just it a singleton, where all the data will be stored in Unit class
    public interface IUnitPlugin {
        bool IsMyUnitType(string unitTypeName);

        void OnUpdate(float timeStep);

        IUnitPlugin CreateNewInstance(LevelManager level, Node unitNode, IUnit unit);
        //TODO: Expand this
    }
}
