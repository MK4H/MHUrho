using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace MHUrho.Plugins
{
	public abstract class ProjectileInstancePlugin : EntityInstancePlugin
	{
		protected IProjectile Projectile { get; private set; }

		protected ProjectileInstancePlugin(ILevelManager level, IProjectile projectile) 
			:base(level, projectile)
		{
			this.Projectile = projectile;
		}



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
