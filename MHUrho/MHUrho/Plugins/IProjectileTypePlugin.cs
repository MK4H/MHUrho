using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Storage;

namespace MHUrho.Plugins
{
    public interface IProjectileTypePlugin : ITypePlugin
    {
        /// <summary>
        /// Creates new instance from scratch
        /// </summary>
        /// <param name="level">level in which the building is created</param>
        /// <param name="projectile">projectile Plugin class</param>
        /// <returns>New instance in default state</returns>
        IProjectileInstancePlugin CreateNewInstance(ILevelManager level, Projectile projectile);



        /// <summary>
        /// Creates instance of <see cref="IProjectileInstancePlugin"/> that will be loaded by <see cref="IProjectileInstancePlugin.LoadState(ILevelManager, Projectile, PluginDataWrapper)"/>
        /// </summary>
        /// <returns>New instance, that will be loaded in the next step</returns>
        IProjectileInstancePlugin GetInstanceForLoading();

        void Initialize(XElement extensionElement, PackageManager packageManager);
    }
}
