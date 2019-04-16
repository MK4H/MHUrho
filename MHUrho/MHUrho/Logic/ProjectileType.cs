using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{

	public class ProjectileType : ILoadableType, IDisposable {

		public int ID { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public ProjectileTypePlugin Plugin {get; private set;}

		public AssetContainer Assets { get; private set; }

		readonly Queue<Projectile> projectilePool;

		bool enablePooling = true;

		public ProjectileType() {
			projectilePool = new Queue<Projectile>();
		}


		public void Load(XElement xml, GamePack package) {
			ID = XmlHelpers.GetID(xml);
			Name = XmlHelpers.GetName(xml);
			Assets = AssetContainer.FromXml(xml.Element(ProjectileTypeXml.Inst.Assets));
			Package = package;

			XElement pathElement = xml.Element(ProjectileTypeXml.Inst.AssemblyPath);

			Plugin = TypePlugin.LoadTypePlugin<ProjectileTypePlugin>(XmlHelpers.GetPath(pathElement), package, Name);

			Plugin.Initialize(XmlHelpers.GetExtensionElement(xml),
								  package);
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

			if (!projectile.ProjectilePlugin.ShootProjectile(target)) {
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

			if (!projectile.ProjectilePlugin.ShootProjectile(movement)) {
				projectile.RemoveFromLevel();
				projectile = null;
			}

			return projectile;
		}

		internal ProjectileInstancePlugin GetNewInstancePlugin(IProjectile projectile, ILevelManager level) {
			return Plugin.CreateNewInstance(level, projectile);
		}

		internal ProjectileInstancePlugin GetInstancePluginForLoading(IProjectile projectile, ILevelManager level) {
			return Plugin.GetInstanceForLoading(level, projectile);
		}

		public bool IsInRange(Vector3 source, IRangeTarget target) {
			if (target == null) {
				return false;
			}

			return Plugin.IsInRange(source, target);
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
			Projectile projectile = null;

			if (projectilePool.Count != 0) {
				//NOTE: Add some clever algorithm to manage projectile count
				projectile = projectilePool.Dequeue();
				projectile.ReInitialize(newID, level, player, position);

			}
			else {
				projectile = Projectile.CreateNew(newID,
												 level,
												 player,
												 position,
												 initRotation,
												 this);


			}

			return projectile;
		}
	}
}
