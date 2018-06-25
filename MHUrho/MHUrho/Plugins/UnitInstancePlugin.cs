using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;

namespace MHUrho.Plugins
{

	public abstract class UnitInstancePlugin : InstancePlugin {

		public IUnit Unit { get; protected set; }

		protected UnitInstancePlugin(ILevelManager level, IUnit unit) 
			:base(level)
		{
			this.Unit = unit;
		}

		protected UnitInstancePlugin() {

		}

		/// <summary>
		/// Loads instance into the state saved in <paramref name="pluginData"/>
		/// 
		/// DO NOT LOAD the default components the unit had when saving, that is done independently by
		/// the Unit class and the components themselfs, just load your own data
		/// 
		/// The default components will be loaded and present on the <see cref="Unit.Node"/>, so you 
		/// can get them by calling <see cref="Node.GetComponent{T}(bool)"/>
		/// </summary>
		/// <param name="level"></param>
		/// <param name="unit"></param>
		/// <param name="pluginData">stored state of the unit plugin</param>
		/// <returns>Instance loaded into saved state</returns>
		public abstract void LoadState(ILevelManager level, IUnit unit, PluginDataWrapper pluginData);

		public abstract void OnProjectileHit(IProjectile projectile);

		public abstract void OnMeeleHit(IEntity byEntity);
	}
}
