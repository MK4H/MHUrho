using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Plugins
{
	/// <summary>
	/// Base class for all instance plugins.
	/// </summary>
	public abstract class InstancePlugin : IDisposable
	{
		/// <summary>
		/// Level this instance belongs to.
		/// </summary>
		public ILevelManager Level { get; protected set; }

		protected InstancePlugin(ILevelManager level) {
			this.Level = level;
		}

		/// <summary>
		/// Called on each scene update.
		/// </summary>
		/// <param name="timeStep">Time elapsed since the last call of this method.</param>
		public virtual void OnUpdate(float timeStep) {
			//NOTHING
		}

		/// <summary>
		/// Saves the state of the plugin into <paramref name="pluginData"/>.
		///
		/// This same <paramref name="pluginData"/> will then be provided on loading of the level to
		/// <see cref="LoadState(PluginDataWrapper)"/> method.
		/// </summary>
		/// <param name="pluginData">Data storage for the plugin data.</param>
		public abstract void SaveState(PluginDataWrapper pluginData);

		/// <summary>
		/// Loads instance into the state saved in <paramref name="pluginData"/>.
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
