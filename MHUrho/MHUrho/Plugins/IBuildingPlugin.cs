using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Plugins
{
    public interface IBuildingPlugin
    {
        IUnitPlugin CreateNewInstance(LevelManager level, Node buildingNode, Building building);

        /// <summary>
        /// Creates new instance in the state saved in <paramref name="pluginData"/>
        /// </summary>
        /// <param name="level">level into which the building is being loaded</param>
        /// <param name="buildingNode">scene node of the building</param>
        /// <param name="building">the building logic class</param>
        /// <param name="pluginData">stored state of the building plugin</param>
        /// <returns>New instance loaded into saved state</returns>
        IUnitPlugin LoadNewInstance(LevelManager level, Node buildingNode, Building building, PluginDataWrapper pluginData);

        bool CanBuildAt(IntVector2 topLeftLocation);

    }
}
