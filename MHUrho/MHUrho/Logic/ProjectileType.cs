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
	public class ProjectileType : IEntityType, IDisposable {

		public float ProjectileSpeed { get; private set; }

		public int ID { get; set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public object Plugin => typePlugin;

		private ModelWrapper model;

		private MaterialWrapper material;

		private readonly Queue<Projectile> projectilePool;

		private ProjectileTypePluginBase typePlugin;

		public ProjectileType() {
			projectilePool = new Queue<Projectile>();
		}


		public void Load(XElement xml, GamePack package) {
			ID = XmlHelpers.GetID(xml);
			Name = XmlHelpers.GetName(xml);
			model = XmlHelpers.GetModel(xml);
			material = XmlHelpers.GetMaterial(xml);
			Package = package;

			typePlugin = XmlHelpers.LoadTypePlugin<ProjectileTypePluginBase>(xml,
																		  package.XmlDirectoryPath,
																		  Name);
			typePlugin.Initialize(XmlHelpers.GetExtensionElement(xml),
								  package.PackageManager);
		}


		public Projectile ShootProjectile(int newID, ILevelManager level, IPlayer player, Vector3 position, IRangeTarget target) {
			var projectile = GetProjectile(newID, level, player, position);

			if (!projectile.Plugin.ShootProjectile(target)) {
				projectile.Despawn();
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

			if (!projectile.Plugin.ShootProjectile(movement)) {
				projectile.Despawn();
				projectile = null;
			}

			return projectile;
		}

		public ProjectileInstancePluginBase GetNewInstancePlugin(Projectile projectile, ILevelManager levelManager) {
			return typePlugin.CreateNewInstance(levelManager, projectile);
		}

		public ProjectileInstancePluginBase GetInstancePluginForLoading() {
			return typePlugin.GetInstanceForLoading();
		}

		public bool IsInRange(Vector3 source, IRangeTarget target) {
			if (target == null) {
				return false;
			}

			return typePlugin.IsInRange(source, target);
		}

		public void Dispose() {
			model?.Dispose();
			material?.Dispose();
		}

		internal bool ProjectileDespawn(Projectile projectile) {
			if (projectilePool.Count >= 124) return false;

			projectilePool.Enqueue(projectile);
			return true;
		}



		private Projectile GetProjectile(int newID, ILevelManager level, IPlayer player, Vector3 position) {
			Projectile projectile = null;

			if (projectilePool.Count != 0) {
				//TODO: Some clever algorithm to manage projectile count
				projectile = projectilePool.Dequeue();
				projectile.ReInitialize(newID, level, player, position);

			}
			else {
				//Projectile node has to be a child of the scene directly for physics to work correctly
				var projectileNode = level.Scene.CreateChild("Projectile");
				projectile = Projectile.SpawnNew(newID,
												 level,
												 player,
												 position,
												 this,
												 projectileNode);

				var staticModel = model.AddModel(projectileNode);
				material.ApplyMaterial(staticModel);

				var rigidBody = projectileNode.CreateComponent<RigidBody>();
				rigidBody.CollisionLayer = (int)CollisionLayer.Projectile;
				rigidBody.CollisionMask = (int)(CollisionLayer.Unit | CollisionLayer.Building);
				rigidBody.Kinematic = true;
				rigidBody.Mass = 1;
				rigidBody.UseGravity = false;

				//TODO: Move collider to plugin
				var collider = projectileNode.CreateComponent<CollisionShape>();
				collider.SetBox(staticModel.BoundingBox.Size, Vector3.Zero, Quaternion.Identity);
			}

			return projectile;
		}
	}
}
