using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;

namespace MHUrho.Plugins
{
    public interface IProjectileInstancePlugin
    {
        void OnUpdate(float timeStep);

        void SaveState(PluginDataWrapper pluginData);

        /// <summary>
        /// Loads instance into the state saved in <paramref name="pluginData"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="projectile"></param>
        /// <param name="pluginData">stored state of the building plugin</param>
        /// <returns>Instance loaded into saved state</returns>
        void LoadState(ILevelManager level, Projectile projectile, PluginDataWrapper pluginData);

        /// <summary>
        /// Reinitializes this instance into default state, to allow for projectile pooling
        /// </summary>
        /// <param name="level">LevelManager to connect to other things</param>
        void ReInitialize(ILevelManager level);
    }
}
