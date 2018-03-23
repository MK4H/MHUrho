using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{
    //TODO: Make this an arrow type
    public class ProjectileType {
        public float ProjectileSpeed { get; private set; }

        private readonly Model model;

        private readonly Material material;

        private readonly Map map;

        public ProjectileType(float projectileSpeed, Map map) {
            //TODO: READ FROM XML
            this.ProjectileSpeed = projectileSpeed;
            this.map = map;
            this.model = PackageManager.Instance.ResourceCache.GetModel("Models/Box.mdl");
            this.material = PackageManager.Instance.ResourceCache.GetMaterial("Materials/BoxMaterial.xml");
        }

        //TODO: PROJECTILE POOLING
        public Projectile SpawnProjectile(Node projectileNode, Vector3 position, Vector3 projectileMovement) {
            projectileNode.Position = position;
            var projectile = new Projectile(projectileMovement, map);
            projectileNode.AddComponent(projectile);

            var staticModel = projectileNode.CreateComponent<StaticModel>();
            staticModel.Model = model;
            staticModel.Material = material;

            projectileNode.Scale = new Vector3(0.2f, 0.2f, 0.8f);

            var rigidBody = projectileNode.CreateComponent<RigidBody>();
            rigidBody.CollisionLayer = (int)CollisionLayer.Arrow;
            rigidBody.CollisionMask = (int)(CollisionLayer.Unit | CollisionLayer.Building);
            rigidBody.Kinematic = true;
            rigidBody.Mass = 1;
            rigidBody.UseGravity = false;

            var collider = projectileNode.CreateComponent<CollisionShape>();
            collider.SetBox(new Vector3(0.2f, 0.2f, 0.8f), new Vector3(-0.1f, -0.1f, -0.4f), Quaternion.Identity);

            return projectile;
        }

    }
}
