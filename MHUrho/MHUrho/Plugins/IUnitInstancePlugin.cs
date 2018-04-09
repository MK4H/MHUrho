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

        /// <summary>
        /// Loads instance into the state saved in <paramref name="pluginData"/>
        /// 
        /// DO NOT LOAD the default components the unit had when saving, that is done independently by
        /// the Unit class and the components themselfs, just load your own data
        /// 
        /// The default components will be loaded and present on the <see cref="Unit.Node"/>, so you 
        /// can get them by calling <see cref="Node.GetComponent{T}(bool)"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="unit"></param>
        /// <param name="pluginData">stored state of the unit plugin</param>
        /// <returns>Instance loaded into saved state</returns>
        void LoadState(ILevelManager level, Unit unit, PluginDataWrapper pluginData);

        //TODO: Move this to AStar as a delegate argument, dont force this onto anyone
        bool CanGoFromTo(ITile fromTile, ITile toTile);
        //TODO: Expand this
    }
}
