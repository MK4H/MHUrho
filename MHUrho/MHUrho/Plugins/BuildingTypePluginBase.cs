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
    public abstract class BuildingTypePluginBase : ITypePlugin {

        public abstract bool IsMyType(string typeName);

        /// <summary>
        /// Creates new instance from scratch
        /// </summary>
        /// <param name="level">level in which the building is created</param>
        /// <param name="building">building Plugin class</param>
        /// <returns>New instance in default state</returns>
        public abstract BuildingInstancePluginBase CreateNewInstance(ILevelManager level, Building building);



        /// <summary>
        /// Creates instance of <see cref="BuildingInstancePluginBase"/> that will be loaded by <see cref="BuildingInstancePluginBase.LoadState(LevelManager, PluginDataWrapper)"/>
        /// </summary>
        /// <returns>New instance, that will be loaded in the next step</returns>
        public abstract BuildingInstancePluginBase GetInstanceForLoading();

        public abstract bool CanBuildIn(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, ILevelManager level);

        public abstract void PopulateUI(MandKUI mouseAndKeyboardUI);

        public abstract void ClearUI(MandKUI mouseAndKeyboardUI);

        public abstract void PopulateUI(TouchUI touchUI);

        public abstract void ClearUI(TouchUI touchUI);

        public abstract void AddSelected(BuildingInstancePluginBase buildingInstance);

        public abstract void RemoveSelected(BuildingInstancePluginBase buildingInstance);

        /// <summary>
        /// Called to initialize the instance
        /// </summary>
        /// <param name="extensionElement">extension element of the unitType xml description or null if there is none</param>
        /// <param name="packageManager">package manager for connecting to other entityTypes</param>
        public abstract void Initialize(XElement extensionElement, PackageManager packageManager);
    }
}
