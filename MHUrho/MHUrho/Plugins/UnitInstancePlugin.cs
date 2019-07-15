using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.DefaultComponents;

namespace MHUrho.Plugins
{

	public abstract class UnitInstancePlugin : EntityInstancePlugin {

		/// <summary>
		/// The unit controlled by this plugin.
		/// </summary>
		public IUnit Unit { get; private set; }

		protected UnitInstancePlugin(ILevelManager level, IUnit unit) 
			:base(level, unit)
		{
			this.Unit = unit;
		}

		/// <summary>
		/// Notifies unit plugin that the tile the unit is on has changed its height.
		///
		/// Before this call, the platform ensures that the unit is not below the terrain,
		/// and moves it above the terrain if it is.
		/// </summary>
		/// <param name="tile">The tile with changed height.</param>
		public abstract void TileHeightChanged(ITile tile);

		/// <summary>
		/// Notifies the unit plugin that a building was built on the tile the unit is standing on.
		/// </summary>
		/// <param name="building">The new building.</param>
		/// <param name="tile">The tile the unit is standing on.</param>
		public abstract void BuildingBuilt(IBuilding building, ITile tile);

		/// <summary>
		/// Notifies the unit plugin that a building was destroyed on the tile the unit is standing on.
		/// </summary>
		/// <param name="building">The destroyed building.</param>
		/// <param name="tile">The tile the unit is standing on.</param>
		public abstract void BuildingDestroyed(IBuilding building, ITile tile);

		/// <summary>
		/// Notifies the unit plugin that the unit has been hit by <paramref name="other"/> entity.
		/// </summary>
		/// <param name="other">The entity that hit this unit.</param>
		/// <param name="userData">User defined data provided by the other entity.</param>
		public abstract void OnHit(IEntity other, object userData);
	}
}
