using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.UserInterface;
using Urho;

namespace MHUrho.Plugins
{
	/// <summary>
	/// Base class for building type plugins of packages.
	/// </summary>
	public abstract class BuildingTypePlugin : TypePlugin {

		/// <summary>
		/// Creates new instance from scratch
		/// </summary>
		/// <param name="level">level in which the building is created</param>
		/// <param name="building">building Plugin class</param>
		/// <returns>New instance in default state</returns>
		public abstract BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building);

		/// <summary>
		/// Creates instance of <see cref="BuildingInstancePlugin"/> that will be loaded by <see cref="BuildingInstancePlugin.LoadState(LevelManager, PluginDataWrapper)"/>
		/// </summary>
		/// <returns>New instance, that will be loaded in the next step</returns>
		public abstract BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building);

		/// <summary>
		/// If a building of the type of this plugin can be built at the <paramref name="topLeftTileIndex"/> by the <paramref name="owner"/>.
		/// </summary>
		/// <param name="topLeftTileIndex">The position of the top left corner of the building.</param>
		/// <param name="owner">The owner of the building.</param>
		/// <param name="level">The level to build in.</param>
		/// <returns>True if the building can be built, false otherwise.</returns>
		public abstract bool CanBuild(IntVector2 topLeftTileIndex, IPlayer owner, ILevelManager level);
	}
}
