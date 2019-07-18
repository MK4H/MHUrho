using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{

	/// <summary>
	/// Represents a projectile type loaded from package.
	/// </summary>
	public class ProjectileType : IEntityType, IDisposable {

		/// <inheritdoc />
		public int ID { get; private set; }

		/// <inheritdoc />
		public string Name { get; private set; }

		/// <inheritdoc />
		public GamePack Package { get; private set; }

		/// <summary>
		/// Assets that will be loaded with every instance of projectile of this type.
		/// </summary>
		public AssetContainer Assets { get; private set; }

		/// <summary>
		/// Type plugin of this projectile type.
		/// </summary>
		public ProjectileTypePlugin Plugin { get; private set; }

		/// <inheritdoc />
		TypePlugin IEntityType.Plugin => Plugin;

		/// <summary>
		/// Pool of the projectiles to be used instead of creating new projectiles.
		/// </summary>
		readonly Queue<Projectile> projectilePool;

		/// <summary>
		/// If removed projectiles should be moved to pool instead of deleted.
		/// </summary>
		bool enablePooling = true;

		/// <summary>
		/// Initializes the projectile type.
		/// </summary>
		public ProjectileType() {
			projectilePool = new Queue<Projectile>();
		}

		public override bool Equals(object obj)
		{
			return object.ReferenceEquals(this, obj);
		}

		public override int GetHashCode()
		{
			return ID;
		}

		/// <summary>
		/// Loads projectile type from xml data of the package.
		/// </summary>
		/// <param name="xml">Xml element with the projectile type data.</param>
		/// <param name="package">Package that is the source of the xml.</param>
		public void Load(XElement xml, GamePack package) {

			Package = package;

			string assemblyPath = null;
			XElement assetsElement = null;
			XElement extensionElem = null;
			try {
				ID = XmlHelpers.GetID(xml);
				Name = XmlHelpers.GetName(xml);
				assemblyPath = XmlHelpers.GetPath(xml.Element(ProjectileTypeXml.Inst.AssemblyPath));
				assetsElement = xml.Element(ProjectileTypeXml.Inst.Assets);
				extensionElem = XmlHelpers.GetExtensionElement(xml);
			}
			catch (Exception e) {
				LoadError($"Projectile type loading failed: Invalid XML of the package {package.Name}", e);
			}

			try
			{
				Assets = AssetContainer.FromXml(assetsElement, package);
			}
			catch (Exception e)
			{
				LoadError($"Projectile type \"{Name}\"[{ID}] loading failed: Asset instantiation failed with exception: {e.Message}", e);
			}

			try {
				Plugin = TypePlugin.LoadTypePlugin<ProjectileTypePlugin>(assemblyPath,
																		package,
																		Name,
																		 ID,
																		 extensionElem);
			}
			catch (Exception e) {
				LoadError($"Projectile type \"{Name}\"[{ID}] loading failed: Plugin loading failed with exception: {e.Message}", e);
			}
		}

		/// <summary>
		/// Clears the level specific state from this type.
		/// </summary>
		public void ClearCache()
		{
			enablePooling = false;
			foreach (var projectile in projectilePool) {
				projectile.RemoveFromLevel();
			}

			projectilePool.Clear();
			enablePooling = true;
		}

		/// <summary>
		/// Creates projectile to shoot at the given target, or null if projectile of this type cannot be shot and hit the target.
		/// </summary>
		/// <param name="newID">The id to give the projectile.</param>
		/// <param name="level">Level in which to shoot the projectile.</param>
		/// <param name="player">Owner of the projectile.</param>
		/// <param name="position">Initial position of the projectile, from where the shooting is happening.</param>
		/// <param name="initRotation">Initial rotation of the projectile.</param>
		/// <param name="target">Target to shoot the projectile at.</param>
		/// <returns>Projectile that was shot at the target, or null if projectile cannot be shot and hit the target.</returns>
		internal IProjectile ShootProjectile(int newID, 
											ILevelManager level, 
											IPlayer player, 
											Vector3 position, 
											Quaternion initRotation, 
											IRangeTarget target) {
			var projectile = GetProjectile(newID, level, player, position, initRotation);

			if (!projectile.Shoot(target)) {
				projectile.RemoveFromLevel();
				projectile = null;
			}

			return projectile;
		}

		/// <summary>
		/// Creates projectile to shoot with the given initial movement, or null if projectile of this type cannot be shot with the given movement.
		/// </summary>
		/// <param name="newID">The id to give the projectile.</param>
		/// <param name="level">Level in which to shoot the projectile.</param>
		/// <param name="player">Owner of the projectile.</param>
		/// <param name="position">Initial position of the projectile, from where the shooting is happening.</param>
		/// <param name="initRotation">Initial rotation of the projectile.</param>
		/// <param name="movement">Initial movement vector of the projectile</param>
		/// <returns>Projectile that was shot with the given movement, or null if projectile cannot be shot with the given movement.</returns>
		internal IProjectile ShootProjectile(int newID, 
										ILevelManager level,
										IPlayer player, 
										Vector3 position,
										Quaternion initRotation,
										Vector3 movement) {

			var projectile = GetProjectile(newID, level, player, position, initRotation);

			if (!projectile.Shoot(movement)) {
				projectile.RemoveFromLevel();
				projectile = null;
			}

			return projectile;
		}

		/// <summary>
		/// Returns new instance plugin to control the <paramref name="projectile"/>.
		/// </summary>
		/// <param name="projectile">The projectile that will be controlled by the instance plugin.</param>
		/// <param name="level">The level in which the projectile is.</param>
		/// <returns>Instance plugin to control the given <paramref name="projectile"/>.</returns>
		internal ProjectileInstancePlugin GetNewInstancePlugin(IProjectile projectile, ILevelManager level) {
			try {
				return Plugin.CreateNewInstance(level, projectile);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Projectile type plugin call {nameof(Plugin.CreateNewInstance)} failed with Exception: {e.Message}");
				throw;
			}
		}

		/// <summary>
		/// Returns an instance plugin to control the <paramref name="projectile"/> in the <paramref name="level"/>.
		/// The plugin will load all it's state from stored data.
		/// </summary>
		/// <param name="projectile">Projectile to control.</param>
		/// <param name="level">Level in which the projectile is.</param>
		/// <returns>Instance plugin that will expect data for loading.</returns>
		internal ProjectileInstancePlugin GetInstancePluginForLoading(IProjectile projectile, ILevelManager level) {
			try {
				return Plugin.GetInstanceForLoading(level, projectile);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Projectile type plugin call {nameof(Plugin.GetInstanceForLoading)} failed with Exception: {e.Message}");
				throw;
			}
			
		}

		/// <summary>
		/// If the <paramref name="target"/> can be hit when shooting from <paramref name="source"/>.
		/// </summary>
		/// <param name="source">The shooting position.</param>
		/// <param name="target">Target to hit.</param>
		/// <returns>True if the target can be hit, false otherwise.</returns>
		public bool IsInRange(Vector3 source, IRangeTarget target) {
			if (target == null) {
				return false;
			}

			try {
				return Plugin.IsInRange(source, target);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Projectile type plugin call {nameof(Plugin.IsInRange)} failed with Exception: {e.Message}");
				return false;
			}
			
		}

		/// <summary>
		/// Releases all assets.
		/// </summary>
		public void Dispose(){
			Assets.Dispose();
		}

		/// <summary>
		/// Try despawn the projectile.
		/// </summary>
		/// <param name="projectile">Projectile to despawn.</param>
		/// <returns>True if the projectile should be pooled, false if the projectile should be completely removed.</returns>
		internal bool ProjectileDespawn(Projectile projectile) {
			if (projectilePool.Count >= 124 || !enablePooling) return false;

			projectilePool.Enqueue(projectile);
			return true;
		}


		/// <summary>
		/// Gets a projectile from the pool and reinitializes it, if there are no projectiles in the pool, creates new projectile.
		/// </summary>
		/// <param name="newID">The new id to give to the projectile.</param>
		/// <param name="level">The level into which to reintroduce the projectile.</param>
		/// <param name="player">The owner of the projectile.</param>
		/// <param name="position">Position at which to spawn the projectile.</param>
		/// <param name="initRotation">Initial rotation of the projectile after the respawn.</param>
		/// <returns>Projectile ready to shoot.</returns>
		Projectile GetProjectile(int newID, ILevelManager level, IPlayer player, Vector3 position, Quaternion initRotation) {


			if (projectilePool.Count != 0) {
				//NOTE: Add some clever algorithm to manage projectile count
				var projectile = projectilePool.Dequeue();

				try {
					projectile.ReInitialize(newID, level, player, position);
					return projectile;
				}
				catch (CreationException) {
					projectile.HardRemove();
				}
			}

			return Projectile.CreateNew(newID,
										 level,
										 player,
										 position,
										 initRotation,
										 this);
		}

		/// <summary>
		/// Logs message and throws a <see cref="PackageLoadingException"/>
		/// </summary>
		/// <param name="message">Message to log and propagate via exception</param>
		/// <exception cref="PackageLoadingException">Always throws this exception</exception>
		void LoadError(string message, Exception e)
		{
			Urho.IO.Log.Write(LogLevel.Error, message);
			throw new PackageLoadingException(message, e);
		}
	}
}
