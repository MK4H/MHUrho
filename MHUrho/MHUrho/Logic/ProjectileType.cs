using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{
    //TODO: Make this an arrow type
    public class ProjectileType : IEntityType, IDisposable {
        private const string NameAttribute = "name";
        private const string ModelPathElement = "modelPath";
        private const string SpeedElement = "speed";
        private const string AssemblyPathElement = "assemblyPath";

        public float ProjectileSpeed { get; private set; }

        public int ID { get; set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        private Model model;

        private Material material;

        public ProjectileType() {

        }

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

        public void Load(XElement xml, int newID, ResourcePack package) {
            ID = newID;
            Name = xml.Attribute(NameAttribute).Value;
            ProjectileSpeed = XmlHelpers.GetFloat(xml, SpeedElement);
            model = LoadModel(xml, package.XmlDirectoryPath);
            Package = package;
            //TODO: Material  
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
            rigidBody.CollisionLayer = (int)CollisionLayer.Projectile;
            rigidBody.CollisionMask = (int)(CollisionLayer.Unit | CollisionLayer.Building);
            rigidBody.Kinematic = true;
            rigidBody.Mass = 1;
            rigidBody.UseGravity = false;

            var collider = projectileNode.CreateComponent<CollisionShape>();
            collider.SetBox(new Vector3(0.2f, 0.2f, 0.8f), new Vector3(-0.1f, -0.1f, -0.4f), Quaternion.Identity);

            return projectile;
        }

        public StEntityType Save() {
            return new StEntityType {
                                        Name = Name,
                                        TypeID = ID,
                                        PackageID = Package.ID
                                    };
        }

        public void Dispose() {
            model?.Dispose();
            material?.Dispose();
        }

        private static Model LoadModel(XElement projectileTypeXml, string pathToPackageXmlDir) {

            string fullPath = XmlHelpers.GetFullPath(projectileTypeXml, ModelPathElement, pathToPackageXmlDir);

            return PackageManager.Instance.ResourceCache.GetModel(fullPath);
        }
    }
}
