using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{
    //TODO: Make this an arrow type
    public class ProjectileType : IIDNameAndPackage, IDisposable {
        public float ProjectileSpeed { get; private set; }

        public int ID { get; set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        private readonly Model model;

        private readonly Material material;

        protected ProjectileType(string name,
                                 float projectileSpeed,
                                 Model model,
                                 ResourcePack package) {
            this.Name = name;
            this.ProjectileSpeed = projectileSpeed;
            this.Package = package;
            this.model = model;
            this.material = null;
        }

        public static ProjectileType Load(XElement xml, int newID, string pathToPackageXMLDir, ResourcePack package) {
            string name = xml.Attribute("name").Value;
            var speed = LoadSpeed(xml);
            var model = LoadModel(xml, pathToPackageXMLDir);

            var newProjectileType = new ProjectileType(name, speed, model, package);

        }

        //TODO: PROJECTILE POOLING
        public Projectile SpawnProjectile(Map map, Node projectileNode, Vector3 position, Vector3 projectileMovement) {
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

        

        public void Dispose() {
            model?.Dispose();
            material?.Dispose();
        }

        private static Model LoadModel(XElement projectileTypeXml, string pathToPackageXMLDir) {

            string relativeModelPath =
                FileManager.CorrectRelativePath(projectileTypeXml.Element(PackageManager.XMLNamespace + "modelPath").Value.Trim());
            string fullPath = System.IO.Path.Combine(pathToPackageXMLDir, relativeModelPath);

            return PackageManager.Instance.ResourceCache.GetModel(fullPath);
        }
    }
}
