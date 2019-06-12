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

	public class ProjectileType : IEntityType, IDisposable {

		public int ID { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public AssetContainer Assets { get; private set; }

		public ProjectileTypePlugin Plugin { get; private set; }

		TypePlugin IEntityType.Plugin => Plugin;

		readonly Queue<Projectile> projectilePool;

		bool enablePooling = true;

		public ProjectileType() {
			projectilePool = new Queue<Projectile>();
		}


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
				Assets = AssetContainer.FromXml(assetsElement);
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

		public void ClearCache()
		{
			enablePooling = false;
			foreach (var projectile in projectilePool) {
				projectile.Dispose();
			}

			projectilePool.Clear();
			enablePooling = true;
		}


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

		public void Dispose(){
			Assets.Dispose();
		}

		internal bool ProjectileDespawn(Projectile projectile) {
			if (projectilePool.Count >= 124 || !enablePooling) return false;

			projectilePool.Enqueue(projectile);
			return true;
		}



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
