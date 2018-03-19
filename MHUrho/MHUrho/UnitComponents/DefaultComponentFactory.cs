using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    public class DefaultComponentFactory
    {
        public delegate DefaultComponent LoadComponentDelegate(LevelManager level, PluginData storedData);

        private readonly Dictionary<string, LoadComponentDelegate> loaders;

        public DefaultComponentFactory() {
            loaders = new Dictionary<string, LoadComponentDelegate>();
            loaders.Add(UnitSelector.ComponentName, UnitSelector.Load);
            loaders.Add(WorldWalker.ComponentName, WorldWalker.Load);
            //TODO: Add other components
        }

        public DefaultComponent LoadComponent(string name, PluginData storedComponent, LevelManager level) {
            return loaders[name].Invoke(level, storedComponent);
        }
    }
}
