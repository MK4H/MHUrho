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
		
		readonly Dictionary<DefaultComponents, DefaultComponentLoader> loaders;

		readonly Dictionary<string, DefaultComponents> nameToID;

		public DefaultComponentFactory() {
			loaders = new Dictionary<DefaultComponents, DefaultComponentLoader>();
			nameToID = new Dictionary<string, DefaultComponents>();

			AddLoaders(loaders);
			AddNameToIDMap(nameToID);

		}

		static DefaultComponentFactory() {

		}

		internal DefaultComponentLoader StartLoadingComponent(string name, PluginData storedComponent, LevelManager level, InstancePluginBase plugin) {
			return StartLoadingComponent(nameToID[name], storedComponent, level, plugin);
		}

		internal DefaultComponentLoader StartLoadingComponent(int ID, PluginData storedComponent, LevelManager level, InstancePluginBase plugin) {
			return StartLoadingComponent((DefaultComponents) ID, storedComponent, level, plugin);
		}

		internal DefaultComponentLoader StartLoadingComponent(DefaultComponents ID, PluginData storedComponent, LevelManager level, InstancePluginBase plugin )
		{
			DefaultComponentLoader loader = loaders[ID].Clone();
			loader.StartLoading(level, plugin, storedComponent);
			return loader;
		}

		void AddLoaders(IDictionary<DefaultComponents, DefaultComponentLoader> loaders) {
			//TODO: Maybe reflection
			loaders.Add(UnitSelector.ComponentID, new UnitSelector.Loader());
			loaders.Add(WorldWalker.ComponentID, new WorldWalker.Loader());
			loaders.Add(Shooter.ComponentID, new Shooter.Loader());
			loaders.Add(ActionQueue.ComponentID, new ActionQueue.Loader());
			loaders.Add(BallisticProjectile.ComponentID, new BallisticProjectile.Loader());
			loaders.Add(StaticRangeTarget.ComponentID, new StaticRangeTarget.Loader());
			loaders.Add(MovingRangeTarget.ComponentID, new MovingRangeTarget.Loader());

			//TODO: Add other components
		}

		void AddNameToIDMap(IDictionary<string, DefaultComponents> map) {
			map.Add(UnitSelector.ComponentName, UnitSelector.ComponentID);
			map.Add(WorldWalker.ComponentName, WorldWalker.ComponentID);
			map.Add(Shooter.ComponentName, Shooter.ComponentID);
			map.Add(ActionQueue.ComponentName, ActionQueue.ComponentID);
			map.Add(BallisticProjectile.ComponentName, BallisticProjectile.ComponentID);
			map.Add(StaticRangeTarget.ComponentName, StaticRangeTarget.ComponentID);
			map.Add(MovingRangeTarget.ComponentName, MovingRangeTarget.ComponentID);
		}

	}
}
