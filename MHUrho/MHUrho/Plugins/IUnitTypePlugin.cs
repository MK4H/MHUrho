using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Plugins
{
    public interface IUnitTypePlugin : ITypePlugin
    {
        IUnitInstancePlugin CreateNewInstance(LevelManager level, Node unitNode, Unit unit);

        /// <summary>
        /// Creates new instance in the state saved in <paramref name="pluginData"/>
        /// 
        /// Does NOT LOAD the default plugins the unit had when saving, that is done independently by
        /// the Unit class
        /// </summary>
        /// <param name="level">level into which the unit is being loaded</param>
        /// <param name="unitNode">scene node of the unit</param>
        /// <param name="unit">the unit logic class</param>
        /// <param name="pluginData">stored state of the unit plugin</param>
        /// <returns>New instance loaded into saved state</returns>
        IUnitInstancePlugin LoadNewInstance(LevelManager level, Node unitNode, Unit unit, PluginDataWrapper pluginData);
    }
}
