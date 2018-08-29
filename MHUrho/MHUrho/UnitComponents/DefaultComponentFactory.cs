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
		
		readonly Dictionary<StDefaultComponent.ComponentOneofCase, DefaultComponentLoader> loaders;

		public DefaultComponentFactory() {
			loaders = new Dictionary<StDefaultComponent.ComponentOneofCase, DefaultComponentLoader>();

			AddLoaders(loaders);

		}

		static DefaultComponentFactory() {

		}

		internal DefaultComponentLoader StartLoadingComponent(StDefaultComponent storedComponent, LevelManager level, InstancePlugin plugin) {
			DefaultComponentLoader loader = loaders[storedComponent.ComponentCase].Clone(level, plugin, storedComponent);
			loader.StartLoading();
			return loader;
		}

		void AddLoaders(IDictionary<StDefaultComponent.ComponentOneofCase, DefaultComponentLoader> loaders) {
			//TODO: Maybe reflection
			loaders.Add(StDefaultComponent.ComponentOneofCase.UnitSelector, new UnitSelector.Loader());
			loaders.Add(StDefaultComponent.ComponentOneofCase.WorldWalker, new WorldWalker.Loader());
			loaders.Add(StDefaultComponent.ComponentOneofCase.Shooter, new Shooter.Loader());
			loaders.Add(StDefaultComponent.ComponentOneofCase.BallisticProjectile, new BallisticProjectile.Loader());
			loaders.Add(StDefaultComponent.ComponentOneofCase.StaticRangeTarget, new StaticRangeTarget.Loader());
			loaders.Add(StDefaultComponent.ComponentOneofCase.MovingRangeTarget, new MovingRangeTarget.Loader());
			loaders.Add(StDefaultComponent.ComponentOneofCase.MovingMeeleAttacker, new MovingMeeleAttacker.Loader());
			loaders.Add(StDefaultComponent.ComponentOneofCase.StaticMeeleAttacker, new StaticMeeleAttacker.Loader());
			loaders.Add(StDefaultComponent.ComponentOneofCase.Clicker, new Clicker.Loader());
			//TODO: Add other components
		}


	}
}
