using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	/// <summary>
	/// Loader that loads projectiles.
	/// </summary>
    interface IProjectileLoader : ILoader
    {
		/// <summary>
		/// The loaded projectile.
		/// </summary>
		IProjectile Projectile { get; }
    }
}
