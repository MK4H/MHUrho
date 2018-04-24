using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;

namespace MHUrho.Plugins
{

	public abstract class UnitInstancePluginBase : InstancePluginBase {

		public Unit Unit { get; protected set; }

		protected UnitInstancePluginBase(ILevelManager level, Unit unit) 
			:base(level)
		{
			this.Unit = unit;
		}

		protected UnitInstancePluginBase() {

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
		public abstract void LoadState(ILevelManager level, Unit unit, PluginDataWrapper pluginData);

		public abstract bool CanGoFromTo(ITile fromTile, ITile toTile);

		public virtual void OnUnitHit() {
			//NOTHING
		}

		//TODO: Expand this
	}
}
