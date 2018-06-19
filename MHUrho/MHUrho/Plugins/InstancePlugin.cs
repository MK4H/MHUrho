using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.Plugins
{
	public abstract class InstancePlugin : IDisposable
	{
		public ILevelManager Level { get; protected set; }

		public Map Map => Level.Map;

		protected InstancePlugin(ILevelManager level) {
			this.Level = level;
		}

		protected InstancePlugin() {

		}

		public virtual void OnUpdate(float timeStep) {
			//NOTHING
		}

		public abstract void SaveState(PluginDataWrapper pluginData);

		public abstract void Dispose();
	}
}
