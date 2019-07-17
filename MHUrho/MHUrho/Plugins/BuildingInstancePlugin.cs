using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UserInterface;
using Urho;

namespace MHUrho.Plugins
{
	/// <summary>
	/// Serves as a predecessor to every instance plugin for buildings.
	/// </summary>
	public abstract class BuildingInstancePlugin : EntityInstancePlugin {
		
		/// <summary>
		/// The building this plugin is controlling.
		/// </summary>
		public IBuilding Building { get; protected set; }

		/// <summary>
		/// Creates a new plugin in the given <paramref name="level"/> to control the given <paramref name="building"/>.
		/// </summary>
		/// <param name="level">The level the plugin is created into.</param>
		/// <param name="building">The building the plugin will control.</param>
		protected BuildingInstancePlugin(ILevelManager level, IBuilding building) 
			:base (level, building) {
			this.Building = building;
		}

		/// <summary>
		/// Asks the plugin if the tile height at [<paramref name="x"/>, <paramref name="y"/>] can be change.
		/// </summary>
		/// <param name="x">The x coord of the position to be changed.</param>
		/// <param name="y">The z coord of the position to be changed.</param>
		/// <returns>True if the height of the given position can be changed, false otherwise.</returns>
		public abstract bool CanChangeTileHeight(int x, int y);

		/// <summary>
		/// Informs the plugin that the height of the <paramref name="tile"/> has changed.
		/// </summary>
		/// <param name="tile">The tile with the changed height.</param>
		public virtual void TileHeightChanged(ITile tile)
		{

		}

		/// <summary>
		/// Gets the height of the impassable portion of the building.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public virtual float? GetHeightAt(float x, float y)
		{
			return null;
		}

		public virtual IFormationController GetFormationController(Vector3 centerPosition)
		{
			return null;
		}

		public virtual void OnHit(IEntity byEntity, object userData)
		{

		}
	}
}
