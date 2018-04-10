using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{
    //TODO: Make this an arrow type
    public class ProjectileType : IEntityType, IDisposable {
        private const string IDAttributeName = "ID";
        private const string NameAttributeName = "name";
        private const string ModelPathElementName = "modelPath";
        private const string MaterialPathElementName = "materialPath";
        private const string SpeedElementName = "speed";
        private const string AssemblyPathElementName = "assemblyPath";
        private const string ExtensionElementName = "extension";

        public float ProjectileSpeed { get; private set; }

        public int ID { get; set; }

        public string Name { get; private set; }

        public GamePack Package { get; private set; }

        public object Plugin => typePlugin;

        private Model model;

        private Material material;

        private readonly Queue<Projectile> projectilePool;

        private IProjectileTypePlugin typePlugin;

        public ProjectileType() {
            projectilePool = new Queue<Projectile>();
        }


        public void Load(XElement xml, GamePack package) {
            ID = xml.GetIntFromAttribute(IDAttributeName);
            Name = xml.Attribute(NameAttributeName).Value;
            ProjectileSpeed = XmlHelpers.GetFloat(xml, SpeedElementName);
            model = LoadModel(xml, package.XmlDirectoryPath);
            material = LoadMaterial(xml, package.XmlDirectoryPath);
            Package = package;

            typePlugin = XmlHelpers.LoadTypePlugin<IProjectileTypePlugin>(xml,
                                                                          AssemblyPathElementName,
                                                                          package.XmlDirectoryPath,
                                                                          Name);
            typePlugin.Initialize(xml.Element(PackageManager.XMLNamespace + ExtensionElementName),
                                  package.PackageManager);
        }

        //TODO: PROJECTILE POOLING
        public Projectile SpawnProjectile(ILevelManager level, Scene sceneNode, Vector3 position, Vector3 projectileMovement) {

            if (projectilePool.Count != 0) {
                //TODO: Some clever algorithm to manage projectile count
                var pooledProjectile = projectilePool.Dequeue();
                pooledProjectile.ReInitialize(level, position ,projectileMovement);
                return pooledProjectile;
            }

            var projectileNode = sceneNode.CreateChild("Projectile");
            var projectile = Projectile.SpawnNew(projectileMovement, 
                                                 position, 
                                                 level, 
                                                 this, 
                                                 projectileNode,
                                                 OnProjectileDespawn);

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

        public IProjectileInstancePlugin GetNewInstancePlugin(Projectile projectile, ILevelManager levelManager) {
            return typePlugin.CreateNewInstance(levelManager, projectile);
        }

        public IProjectileInstancePlugin GetInstancePluginForLoading() {
            return typePlugin.GetInstanceForLoading();
        }

        public void Dispose() {
            model?.Dispose();
            material?.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectile"></param>
        /// <returns>True if the projectile was pooled, false if it should destroy itself</returns>
        private bool OnProjectileDespawn(Projectile projectile) {
            if (projectilePool.Count >= 124) return false;

            projectilePool.Enqueue(projectile);
            return true;
        }

        private static Model LoadModel(XElement projectileTypeXml, string pathToPackageXmlDir) {

            string fullPath = XmlHelpers.GetFullPath(projectileTypeXml, ModelPathElementName, pathToPackageXmlDir);

            return PackageManager.Instance.ResourceCache.GetModel(fullPath);
        }

        private static Material LoadMaterial(XElement projectileTypeXml, string pathToPackageXmlDir) {
            string materialPath = XmlHelpers.GetFullPath(projectileTypeXml, MaterialPathElementName, pathToPackageXmlDir);

            return PackageManager.Instance.ResourceCache.GetMaterial(materialPath);
        }
    }
}
