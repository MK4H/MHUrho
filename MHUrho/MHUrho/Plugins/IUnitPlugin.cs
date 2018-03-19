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
    public interface IUnitPlugin {
        bool IsMyUnitType(string unitTypeName);

        bool Order(ITile tile);

        void OnUpdate(float timeStep);

        IUnitPlugin CreateNewInstance(LevelManager level, Node unitNode, Unit unit);

        /// <summary>
        /// Creates new instance in the state saved in <paramref name="pluginDataStorage"/>
        /// 
        /// Does NOT LOAD the default plugins the unit had when saving, that is done independently by
        /// the Unit class
        /// </summary>
        /// <param name="level">level into which the unit is being loaded</param>
        /// <param name="unitNode">scene node of the unit</param>
        /// <param name="unit">the unit logic class</param>
        /// <param name="pluginDataStorage">stored state of the unit plugin</param>
        /// <returns>New instance loaded into saved state</returns>
        IUnitPlugin LoadNewInstance(LevelManager level, Node unitNode, Unit unit, PluginDataWrapper pluginDataStorage);

        void SaveState(PluginDataWrapper pluginDataStorage);
        //TODO: Expand this
    }
}
