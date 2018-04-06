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
        /// Create new instance of the unit in with default contents
        /// 
        /// Add components from <see cref="MHUrho.UnitComponents"/> to <paramref name="unitNode"/> and/or
        /// create your own Plugin in <see cref="IUnitInstancePlugin.OnUpdate(float)"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="unitNode"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        IUnitInstancePlugin CreateNewInstance(LevelManager level, Node unitNode, Unit unit);

        /// <summary>
        /// Creates new instance in the state saved in <paramref name="pluginData"/>
        /// 
        /// DO NOT LOAD the default components the unit had when saving, that is done independently by
        /// the Unit class and the components themselfs, just load your own data
        /// 
        /// The default components will be loaded and present on the <paramref name="unitNode"/>, so you 
        /// can get them by calling <see cref="Node.GetComponent{T}(bool)"/>
        /// </summary>
        /// <param name="level">level into which the unit is being loaded</param>
        /// <param name="unitNode">scene node of the unit</param>
        /// <param name="unit">the unit Plugin class</param>
        /// <param name="pluginData">stored state of the unit plugin</param>
        /// <returns>New instance loaded into saved state</returns>
        IUnitInstancePlugin LoadNewInstance(LevelManager level, Node unitNode, Unit unit, PluginDataWrapper pluginData);

        /// <summary>
        /// Called to initialize the instance
        /// </summary>
        /// <param name="extensionElement">extension element of the unitType xml description or null if there is none</param>
        /// <param name="packageManager">package manager for connecting to other entityTypes</param>
        void Initialize(XElement extensionElement, PackageManager packageManager);
    }
}
