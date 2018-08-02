using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Plugins
{
	public abstract class InstancePlugin : IDisposable
	{
		public ILevelManager Level { get; protected set; }

		public Map Map => Level.Map;

		protected InstancePlugin(ILevelManager level) {
			this.Level = level;
		}

		public virtual void OnUpdate(float timeStep) {
			//NOTHING
		}

		public abstract void SaveState(PluginDataWrapper pluginData);

		/// <summary>
		/// Loads instance into the state saved in <paramref name="pluginData"/>
		/// 
		/// DO NOT LOAD the default components, that is done independently by
		/// the Entity class and the components themselfs, just load your own data
		/// 
		/// The default components will be loaded and present on the <see cref="IEntity.Node"/>, so you 
		/// can get them by calling <see cref="IEntity.GetComponent{T}()"/>
		/// </summary>
		/// <param name="pluginData">stored state of the instance plugin</param>
		/// <returns>Instance loaded into saved state</returns>
		public abstract void LoadState(PluginDataWrapper pluginData);

		public abstract void Dispose();
	}
}
