using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Storage;

namespace MHUrho.Plugins
{
    public abstract class ProjectileTypePluginBase : ITypePlugin
    {
        public abstract bool IsMyType(string typeName);

        /// <summary>
        /// Creates new instance from scratch
        /// </summary>
        /// <param name="level">level in which the building is created</param>
        /// <param name="projectile">projectile Plugin class</param>
        /// <returns>New instance in default state</returns>
        public abstract ProjectileInstancePluginBase CreateNewInstance(ILevelManager level, Projectile projectile);



        /// <summary>
        /// Creates instance of <see cref="ProjectileInstancePluginBase"/> that will be loaded by <see cref="ProjectileInstancePluginBase.LoadState(ILevelManager, Projectile, PluginDataWrapper)"/>
        /// </summary>
        /// <returns>New instance, that will be loaded in the next step</returns>
        public abstract ProjectileInstancePluginBase GetInstanceForLoading();

        public abstract void Initialize(XElement extensionElement, PackageManager packageManager);
    }
}
