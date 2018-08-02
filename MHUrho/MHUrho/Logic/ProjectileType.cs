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
	//TODO: Make this an arrow type
	public class ProjectileType : ILoadableType, IDisposable {

		public int ID { get; set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public object Plugin => typePlugin;

		public ModelWrapper Model { get; private set; }

		public MaterialWrapper Material { get; private set; }

		readonly Queue<Projectile> projectilePool;

		ProjectileTypePlugin typePlugin;

		public ProjectileType() {
			projectilePool = new Queue<Projectile>();
		}


		public void Load(XElement xml, GamePack package) {
			ID = XmlHelpers.GetID(xml);
			Name = XmlHelpers.GetName(xml);
			Model = XmlHelpers.GetModel(xml);
			Material = XmlHelpers.GetMaterial(xml);
			Package = package;

			typePlugin = XmlHelpers.LoadTypePlugin<ProjectileTypePlugin>(xml,
																		  package.XmlDirectoryPath,
																		  Name);
			typePlugin.Initialize(XmlHelpers.GetExtensionElement(xml),
								  package.PackageManager);
		}


		public Projectile ShootProjectile(int newID, ILevelManager level, IPlayer player, Vector3 position, IRangeTarget target) {
			var projectile = GetProjectile(newID, level, player, position);

			if (!projectile.ProjectilePlugin.ShootProjectile(target)) {
				projectile.RemoveFromLevel();
				projectile = null;
			}

			return projectile;
		}

		public Projectile ShootProjectile(int newID, 
										ILevelManager level,
										IPlayer player, 
										Vector3 position,
										Vector3 movement) {

			var projectile = GetProjectile(newID, level, player, position);

			if (!projectile.ProjectilePlugin.ShootProjectile(movement)) {
				projectile.RemoveFromLevel();
				projectile = null;
			}

			return projectile;
		}

		public ProjectileInstancePlugin GetNewInstancePlugin(IProjectile projectile, ILevelManager level) {
			return typePlugin.CreateNewInstance(level, projectile);
		}

		public ProjectileInstancePlugin GetInstancePluginForLoading(IProjectile projectile, ILevelManager level) {
			return typePlugin.GetInstanceForLoading(level, projectile);
		}

		public bool IsInRange(Vector3 source, IRangeTarget target) {
			if (target == null) {
				return false;
			}

			return typePlugin.IsInRange(source, target);
		}

		public void Dispose() {
			Model?.Dispose();
			Material?.Dispose();
		}

		internal bool ProjectileDespawn(Projectile projectile) {
			if (projectilePool.Count >= 124) return false;

			projectilePool.Enqueue(projectile);
			return true;
		}



		Projectile GetProjectile(int newID, ILevelManager level, IPlayer player, Vector3 position) {
			Projectile projectile = null;

			if (projectilePool.Count != 0) {
				//TODO: Some clever algorithm to manage projectile count
				projectile = projectilePool.Dequeue();
				projectile.ReInitialize(newID, level, player, position);

			}
			else {
				//Projectile node has to be a child of the scene directly for physics to work correctly
				var projectileNode = level.LevelNode.CreateChild("Projectile");
				projectile = Projectile.CreateNew(newID,
												 level,
												 player,
												 position,
												 this,
												 projectileNode);


			}

			return projectile;
		}
	}
}
