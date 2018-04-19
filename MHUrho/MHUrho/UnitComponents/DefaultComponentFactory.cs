using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
	public class DefaultComponentFactory
	{
		public delegate DefaultComponent LoadComponentDelegate(ILevelManager level, InstancePluginBase unitPlugin, PluginData storedData);

		private readonly Dictionary<DefaultComponents, LoadComponentDelegate> loaders;

		private readonly Dictionary<string, DefaultComponents> nameToID;

		public DefaultComponentFactory() {
			loaders = new Dictionary<DefaultComponents, LoadComponentDelegate>();
			nameToID = new Dictionary<string, DefaultComponents>();

			AddLoaders(loaders);
			AddNameToIDMap(nameToID);

		}



		public DefaultComponent LoadComponent(string name, PluginData storedComponent, ILevelManager level, InstancePluginBase plugin) {
			return LoadComponent(nameToID[name], storedComponent, level, plugin);
		}

		public DefaultComponent LoadComponent(int ID, PluginData storedComponent, ILevelManager level, InstancePluginBase plugin) {
			return LoadComponent((DefaultComponents) ID, storedComponent, level, plugin);
		}

		public DefaultComponent LoadComponent(DefaultComponents ID, PluginData storedComponent, ILevelManager level, InstancePluginBase plugin ) {
			return loaders[ID].Invoke(level, plugin, storedComponent);
		}

		private static void AddLoaders(IDictionary<DefaultComponents, LoadComponentDelegate> loaders) {
			//TODO: Maybe reflection
			loaders.Add(UnitSelector.ComponentID, UnitSelector.Load);
			loaders.Add(WorldWalker.ComponentID, WorldWalker.Load);
			loaders.Add(Shooter.ComponentID, Shooter.Load);
			loaders.Add(ActionQueue.ComponentID, ActionQueue.Load);
			loaders.Add(UnpoweredFlier.ComponentID, UnpoweredFlier.Load);
			//TODO: Add other components
		}

		private static void AddNameToIDMap(IDictionary<string, DefaultComponents> map) {
			map.Add(UnitSelector.ComponentName, UnitSelector.ComponentID);
			map.Add(WorldWalker.ComponentName, WorldWalker.ComponentID);
			map.Add(Shooter.ComponentName, Shooter.ComponentID);
			map.Add(ActionQueue.ComponentName, ActionQueue.ComponentID);
			map.Add(UnpoweredFlier.ComponentName, UnpoweredFlier.ComponentID);

		}

	}
}
