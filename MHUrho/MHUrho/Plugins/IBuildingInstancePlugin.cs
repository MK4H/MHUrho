using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UserInterface;
using Urho;

namespace MHUrho.Plugins
{
    public interface IBuildingInstancePlugin
    {
        /// <summary>
        /// Provides a target tile that the worker unit should go to
        /// </summary>
        /// <param name="unit">Worker unit of the building</param>
        /// <returns>Target tile</returns>
        ITile GetExchangeTile(Unit unit);

        void OnUpdate(float timeStep);

        void SaveState(PluginDataWrapper pluginData);

        /// <summary>
        /// Loads instance into the state saved in <paramref name="pluginData"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="building"></param>
        /// <param name="pluginData">stored state of the building plugin</param>
        /// <returns>Instance loaded into saved state</returns>
        void LoadState(LevelManager level, Building building, PluginDataWrapper pluginData);

    }
}
