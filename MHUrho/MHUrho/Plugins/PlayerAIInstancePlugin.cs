using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.Plugins
{
    public abstract class PlayerAIInstancePlugin
    {
		public ILevelManager Level { get; protected set; }

		public Map Map => Level.Map;

		protected IPlayer Player;

		protected PlayerAIInstancePlugin(ILevelManager level) {
			this.Level = level;
		}

		protected PlayerAIInstancePlugin() {

		}

		public abstract void OnUpdate(float timeStep);

		public abstract void SaveState(PluginDataWrapper pluginData);

		public abstract void LoadState(ILevelManager level, IPlayer player, PluginDataWrapper pluginData);

		public virtual void OnBuildingDestroyed(IBuilding building)
		{

		}

		public virtual void OnUnitKilled(IUnit unit)
		{

		}
	}
}
