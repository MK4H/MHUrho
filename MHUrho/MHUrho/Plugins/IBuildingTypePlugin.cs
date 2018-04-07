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
    public interface IBuildingTypePlugin : ITypePlugin
    {
        /// <summary>
        /// Creates new instance from scratch
        /// </summary>
        /// <param name="level">level in which the building is created</param>
        /// <param name="building">building Plugin class</param>
        /// <returns>New instance in default state</returns>
        IBuildingInstancePlugin CreateNewInstance(LevelManager level, Building building);



        /// <summary>
        /// Creates instance of <see cref="IBuildingInstancePlugin"/> that will be loaded by <see cref="IBuildingInstancePlugin.LoadState(LevelManager, PluginDataWrapper)"/>
        /// </summary>
        /// <returns>New instance, that will be loaded in the next step</returns>
        IBuildingInstancePlugin GetInstanceForLoading();

        bool CanBuildAt(IntVector2 centerLocation);

        void PopulateUI(MandKUI mouseAndKeyboardUI);

        void ClearUI(MandKUI mouseAndKeyboardUI);

        void PopulateUI(TouchUI touchUI);

        void ClearUI(TouchUI touchUI);

        void AddSelected(IBuildingInstancePlugin buildingInstance);

        void RemoveSelected(IBuildingInstancePlugin buildingInstance);

        /// <summary>
        /// Called to initialize the instance
        /// </summary>
        /// <param name="extensionElement">extension element of the unitType xml description or null if there is none</param>
        /// <param name="packageManager">package manager for connecting to other entityTypes</param>
        void Initialize(XElement extensionElement, PackageManager packageManager);
    }
}
