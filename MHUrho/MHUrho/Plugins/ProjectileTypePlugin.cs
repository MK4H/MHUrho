using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using Urho;

namespace MHUrho.Plugins
{
	/// <summary>
	/// Base class for projectile type plugins.
	/// </summary>
	public abstract class ProjectileTypePlugin : TypePlugin
	{
		/// <summary>
		/// Creates new instance from scratch
		/// </summary>
		/// <param name="level">level in which the building is created</param>
		/// <param name="projectile">projectile Plugin class</param>
		/// <returns>New instance in default state</returns>
		public abstract ProjectileInstancePlugin CreateNewInstance(ILevelManager level, IProjectile projectile);



		/// <summary>
		/// Creates instance of <see cref="ProjectileInstancePlugin"/> that will be loaded by <see cref="ProjectileInstancePlugin.LoadState(ILevelManager, Projectile, PluginDataWrapper)"/>
		/// </summary>
		/// <returns>New instance, that will be loaded in the next step</returns>
		public abstract ProjectileInstancePlugin GetInstanceForLoading(ILevelManager level, IProjectile projectile);

		/// <summary>
		/// Decides if the <paramref name="target"/> is in range of this projectile when shot from the position <paramref name="source"/>.
		/// </summary>
		/// <param name="source">The source position of the projectile.</param>
		/// <param name="target">The target of the projectile.</param>
		/// <returns>True if projectile of this type can reach the target from the <paramref name="source"/> position, false otherwise.</returns>
		public abstract bool IsInRange(Vector3 source, IRangeTarget target);
	}
}
