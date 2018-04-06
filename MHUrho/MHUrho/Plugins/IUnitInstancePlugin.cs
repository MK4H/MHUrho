using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Storage;

namespace MHUrho.Plugins
{
    //TODO: Either make one instance of this for every unit,
    // OR make just it a singleton, where all the data will be stored in Unit class
    public interface IUnitInstancePlugin {
        void OnUpdate(float timeStep);

        void SaveState(PluginDataWrapper pluginData);
        //TODO: Expand this
    }
}
