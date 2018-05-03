using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace MHUrho.Plugins
{
	public abstract class ProjectileInstancePlugin : InstancePlugin
	{
		protected IProjectile projectile;

		protected ProjectileInstancePlugin(ILevelManager level, IProjectile projectile) 
			:base(level)
		{
			this.projectile = projectile;
		}

		protected ProjectileInstancePlugin() {

		}

		/// <summary>
		/// Loads instance into the state saved in <paramref name="pluginData"/>
		/// </summary>
		/// <param name="level"></param>
		/// <param name="projectile"></param>
		/// <param name="pluginData">stored state of the building plugin</param>
		/// <returns>Instance loaded into saved state</returns>
		public abstract void LoadState(ILevelManager level, IProjectile projectile, PluginDataWrapper pluginData);

		/// <summary>
		/// Reinitializes this instance into default state, to allow for projectile pooling
		/// </summary>
		/// <param name="level">LevelManager to connect to other things</param>
		public abstract void ReInitialize(ILevelManager level);

		public abstract bool ShootProjectile(IRangeTarget target);

		public abstract bool ShootProjectile(Vector3 movement);

		public abstract void OnEntityHit(IEntity hitEntity);

		public abstract void OnTerrainHit();

	}
}
