using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.Plugins
{
    public abstract class PlayerLogicPlugin
    {
		public ILevelManager Level { get; protected set; }

		public Map Map => Level.Map;

		protected PlayerLogicPlugin(ILevelManager level) {
			this.Level = level;
		}

		protected PlayerLogicPlugin() {

		}

		public abstract void OnUpdate(float timeStep);

		public abstract void SaveState(PluginDataWrapper pluginData);

		public abstract void LoadState(ILevelManager level, IPlayer player, PluginDataWrapper pluginData);

		public virtual void OnBuildingDestroyed(Building building)
		{

		}

		public virtual void OnUnitKilled(Unit unit)
		{

		}
	}
}
