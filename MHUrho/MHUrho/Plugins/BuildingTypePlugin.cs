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
		public abstract BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding projectile);

		public abstract bool CanBuildIn(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, ILevelManager level);

		public abstract void PopulateUI(MandKGameUI mouseAndKeyboardUI);

		public abstract void ClearUI(MandKGameUI mouseAndKeyboardUI);

		public abstract void PopulateUI(TouchUI touchUI);

		public abstract void ClearUI(TouchUI touchUI);

		public abstract void AddSelected(BuildingInstancePlugin buildingInstance);

		public abstract void RemoveSelected(BuildingInstancePlugin buildingInstance);
	}
}
