﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UserInterface;
using Urho;

namespace MHUrho.Plugins
{
    public abstract class BuildingInstancePluginBase
    {

        public virtual void OnUpdate(float timeStep) {
            //NOTHING
        }

        public abstract void SaveState(PluginDataWrapper pluginData);

        /// <summary>
        /// Loads instance into the state saved in <paramref name="pluginData"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="building"></param>
        /// <param name="pluginData">stored state of the building plugin</param>
        /// <returns>Instance loaded into saved state</returns>
        public abstract void LoadState(ILevelManager level, Building building, PluginDataWrapper pluginData);

    }
}