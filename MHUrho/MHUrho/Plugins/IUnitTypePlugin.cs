using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Plugins
{
    public interface IUnitTypePlugin : ITypePlugin
    {
        /// <summary>
        /// Data overriding or superseding the XML data describing the type
        /// </summary>
        UnitTypeInitializationData TypeData{ get; }

        /// <summary>
        /// Create new instance of the unit in with default contents
        /// 
        /// Add components from <see cref="MHUrho.UnitComponents"/> to <see name="unit.Node"/> and/or
        /// create your own Plugin in <see cref="IUnitInstancePlugin.OnUpdate(float)"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        IUnitInstancePlugin CreateNewInstance(ILevelManager level, Unit unit);


        /// <summary>
        /// Creates instance of <see cref="IUnitInstancePlugin"/> that will be loaded by <see cref="IUnitInstancePlugin.LoadState(LevelManager, PluginDataWrapper)"/>
        /// </summary>
        /// <returns></returns>
        IUnitInstancePlugin GetInstanceForLoading();
    

        /// <summary>
        /// Checks if the UnitType can be spawned at <paramref name="centerTile"/>
        /// </summary>
        /// <param name="centerTile">Tile to spawn the unit at, the center of the unit will be at the center of the tile</param>
        /// <returns>true if can, false if cannot</returns>
        bool CanSpawnAt(ITile centerTile);

        /// <summary>
        /// Called to initialize the instance
        /// </summary>
        /// <param name="extensionElement">extension element of the unitType xml description or null if there is none</param>
        /// <param name="packageManager">package manager for connecting to other entityTypes</param>
        void Initialize(XElement extensionElement, PackageManager packageManager);
    }
}
