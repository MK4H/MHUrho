using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using Urho;

namespace MHUrho.Plugins
{
	/// <summary>
	/// Base class for projectile instance plugins.
	/// </summary>
	public abstract class ProjectileInstancePlugin : EntityInstancePlugin
	{
		/// <summary>
		/// The projectile controlled by this plugin.
		/// </summary>
		public IProjectile Projectile { get; private set; }

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

		/// <summary>
		/// Starts the projectiles movement from it's current position towards the <paramref name="target"/>.
		/// </summary>
		/// <param name="target">The target to move to.</param>
		/// <returns>True if projectile can reach the target, false otherwise.</returns>
		public abstract bool ShootProjectile(IRangeTarget target);


		/// <summary>
		/// Starts the projectiles movement from it's current position, with <paramref name="movement"/> as change in position per second.
		/// </summary>
		/// <param name="movement">Change of the position of the projectile per second..</param>
		/// <returns>True if projectile can be shot this way, false otherwise.</returns>
		public abstract bool ShootProjectile(Vector3 movement);

		/// <summary>
		/// Invoked when the <see cref="Projectile"/> hit's an entity.
		/// </summary>
		/// <param name="hitEntity">The entity that was hit.</param>
		public abstract void OnEntityHit(IEntity hitEntity);

		/// <summary>
		/// Invoked when projectile hits the terrain.
		/// </summary>
		public abstract void OnTerrainHit();

	}
}
