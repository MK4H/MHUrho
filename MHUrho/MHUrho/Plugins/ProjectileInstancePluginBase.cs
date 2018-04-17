using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace MHUrho.Plugins
{
    public abstract class ProjectileInstancePluginBase : InstancePluginBase
    {
        protected Projectile projectile;

        protected ProjectileInstancePluginBase(ILevelManager level, Projectile projectile) 
            :base(level)
        {
            this.projectile = projectile;
        }

        protected ProjectileInstancePluginBase() {

        }

        /// <summary>
        /// Loads instance into the state saved in <paramref name="pluginData"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="projectile"></param>
        /// <param name="pluginData">stored state of the building plugin</param>
        /// <returns>Instance loaded into saved state</returns>
        public abstract void LoadState(ILevelManager level, Projectile projectile, PluginDataWrapper pluginData);

        /// <summary>
        /// Reinitializes this instance into default state, to allow for projectile pooling
        /// </summary>
        /// <param name="level">LevelManager to connect to other things</param>
        public abstract void ReInitialize(ILevelManager level);

        public virtual bool ShootProjectile(RangeTarget target) {
            return false;
        }

        public virtual bool ShootProjectile(Vector3 target) {
            return false;
        }

    }
}
