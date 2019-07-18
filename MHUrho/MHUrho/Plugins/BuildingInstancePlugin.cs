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
		/// Gets the height of the building overriding the height of the terrain at this point.
		/// </summary>
		/// <param name="x">The X coordinate in th XZ plane.</param>
		/// <param name="y">The Y coordinate in the XZ plane.</param>
		/// <returns>Height of the building if we want to override the height of the terrain, null otherwise.</returns>
		public virtual float? GetHeightAt(float x, float y)
		{
			return null;
		}

		/// <summary>
		/// Gets a formation controller that can be used to position groups of units around the
		/// <paramref name="centerPosition"/> on the building.
		/// </summary>
		/// <param name="centerPosition">The center position to order the units around.</param>
		/// <returns>Formation controller if units can be ordered onto the building, null otherwise.</returns>
		public virtual IFormationController GetFormationController(Vector3 centerPosition)
		{
			return null;
		}

		/// <summary>
		/// Informs the plugin that the building was hit by the <paramref name="byEntity"/>,
		///  which added the data <paramref name="userData"/> to the message.
		/// </summary>
		/// <param name="byEntity">The entity that hit the building.</param>
		/// <param name="userData">The user data associated with the hit.</param>
		public virtual void OnHit(IEntity byEntity, object userData)
		{

		}
	}
}
