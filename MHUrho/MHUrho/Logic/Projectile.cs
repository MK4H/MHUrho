using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;
using MHUrho.Helpers;

namespace MHUrho.Logic
{
    class Projectile : Component {
        private Map map;

        private Vector3 movement;

        protected Projectile(Map map, Vector3 movement) {
            ReceiveSceneUpdates = true;
            this.map = map;
            this.movement = movement;
        }

        public static Projectile CreateNew(Node node, Vector3 position, Vector3 movement, LevelManager level) {
            var projectile = new Projectile(level.Map, movement);

            node.AddComponent(projectile);
            node.Position = position;
            var staticModel = node.CreateComponent<StaticModel>();
            staticModel.Model = PackageManager.Instance.ResourceCache.GetModel("Models/Box.mdl");
            staticModel.Material = PackageManager.Instance.ResourceCache.GetMaterial("Materials/BoxMaterial.xml");

            node.Scale = new Vector3(0.4f, 0.4f, 0.4f);

            var rigidBody = node.CreateComponent<RigidBody>();
            rigidBody.CollisionLayer = 2; //CollisionLayer 1 is units, 2 is arrows
            rigidBody.CollisionMask = 1;
            rigidBody.Kinematic = true;
            rigidBody.Mass = 1;
            rigidBody.UseGravity = false;

            var collider = node.CreateComponent<CollisionShape>();
            collider.SetBox(new Vector3(0.4f, 0.4f, 0.4f), new Vector3(-0.2f, -0.2f, -0.2f), Quaternion.Identity);

            return projectile;
        }

        protected override void OnUpdate(float timeStep) {
            base.OnUpdate(timeStep);
            
            if (map.GetHeightAt(Node.Position.XZ2()) >= Node.Position.Y) {
                //Node.Remove();
            }
            else {
                Node.Position += movement * timeStep;

                movement += (-Vector3.UnitY * 10) * timeStep;
            }
        }

    }
}
