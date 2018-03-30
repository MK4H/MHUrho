using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UserInterface;
using Urho;

namespace MHUrho.Plugins
{
    public interface IBuildingTypePlugin : ITypePlugin
    {
        /// <summary>
        /// Creates new instance from scratch
        /// </summary>
        /// <param name="level">level in which the building is created</param>
        /// <param name="buildingNode">scene node of the building with <paramref name="building"/> already added as component</param>
        /// <param name="building">building logic class</param>
        /// <returns>New instance in default state</returns>
        IBuildingInstancePlugin CreateNewInstance(LevelManager level, Node buildingNode, Building building);

        /// <summary>
        /// Creates new instance in the state saved in <paramref name="pluginData"/>
        /// </summary>
        /// <param name="level">level into which the building is being loaded</param>
        /// <param name="buildingNode">scene node of the building with <paramref name="building"/> already added as component</param>
        /// <param name="building">the building logic class</param>
        /// <param name="pluginData">stored state of the building plugin</param>
        /// <returns>New instance loaded into saved state</returns>
        IBuildingInstancePlugin LoadNewInstance(LevelManager level, Node buildingNode, Building building, PluginDataWrapper pluginData);

        bool CanBuildAt(IntVector2 topLeftLocation);

        void PopulateUI(MandKUI mouseAndKeyboardUI);

        void ClearUI(MandKUI mouseAndKeyboardUI);

        void PopulateUI(TouchUI touchUI);

        void ClearUI(TouchUI touchUI);

        void AddSelected(IBuildingInstancePlugin buildingInstance);

        void RemoveSelected(IBuildingInstancePlugin buildingInstance);
    }
}
