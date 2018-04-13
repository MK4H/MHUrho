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
    public abstract class UnitTypePluginBase : ITypePlugin
    {
        public abstract bool IsMyType(string typeName);

        /// <summary>
        /// Data overriding or superseding the XML data describing the type
        /// </summary>
        public virtual UnitTypeInitializationData TypeData { get; } = null;

        /// <summary>
        /// Create new instance of the unit in with default contents
        /// 
        /// Add components from <see cref="MHUrho.UnitComponents"/> to <see name="unit.Node"/> and/or
        /// create your own Plugin in <see cref="UnitInstancePluginBase.OnUpdate(float)"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public abstract UnitInstancePluginBase CreateNewInstance(ILevelManager level, Unit unit);


        /// <summary>
        /// Creates instance of <see cref="UnitInstancePluginBase"/> that will be loaded by <see cref="UnitInstancePluginBase.LoadState(LevelManager, PluginDataWrapper)"/>
        /// </summary>
        /// <returns></returns>
        public abstract UnitInstancePluginBase GetInstanceForLoading();


        /// <summary>
        /// Checks if the UnitType can be spawned at <paramref name="centerTile"/>
        /// </summary>
        /// <param name="centerTile">Tile to spawn the unit at, the center of the unit will be at the center of the tile</param>
        /// <returns>true if can, false if cannot</returns>
        public abstract bool CanSpawnAt(ITile centerTile);

        /// <summary>
        /// Called to initialize the instance
        /// </summary>
        /// <param name="extensionElement">extension element of the unitType xml description or null if there is none</param>
        /// <param name="packageManager">package manager for connecting to other entityTypes</param>
        public abstract void Initialize(XElement extensionElement, PackageManager packageManager);
    }
}
