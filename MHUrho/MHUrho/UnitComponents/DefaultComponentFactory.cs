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

        private readonly Dictionary<DefaultComponents, LoadComponentDelegate> loaders;

        private readonly Dictionary<string, DefaultComponents> nameToID;

        public DefaultComponentFactory() {
            loaders = new Dictionary<DefaultComponents, LoadComponentDelegate>();
            nameToID = new Dictionary<string, DefaultComponents>();

            AddLoaders(loaders);
            AddNameToIDMap(nameToID);

        }



        public DefaultComponent LoadComponent(string name, PluginData storedComponent, LevelManager level) {
            return LoadComponent(nameToID[name], storedComponent, level);
        }

        public DefaultComponent LoadComponent(int ID, PluginData storedComponent, LevelManager level) {
            return LoadComponent((DefaultComponents) ID, storedComponent, level);
        }

        public DefaultComponent LoadComponent(DefaultComponents ID, PluginData storedComponent, LevelManager level) {
            return loaders[ID].Invoke(level, storedComponent);
        }

        private static void AddLoaders(IDictionary<DefaultComponents, LoadComponentDelegate> loaders) {
            //TODO: Maybe reflection
            loaders.Add(UnitSelector.ComponentID, UnitSelector.Load);
            loaders.Add(WorldWalker.ComponentID, WorldWalker.Load);
            loaders.Add(DirectShooter.ComponentID, DirectShooter.Load);
            loaders.Add(WorkQueue.ComponentID, WorkQueue.Load);
            //TODO: Add other components
        }

        private static void AddNameToIDMap(IDictionary<string, DefaultComponents> map) {
            map.Add(UnitSelector.ComponentName, UnitSelector.ComponentID);
            map.Add(WorldWalker.ComponentName, WorldWalker.ComponentID);
            map.Add(DirectShooter.ComponentName, DirectShooter.ComponentID);
            map.Add(WorkQueue.ComponentName, WorkQueue.ComponentID);

        }

    }
}
