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

        private ProjectileTypePluginBase typePlugin;

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

            typePlugin = XmlHelpers.LoadTypePlugin<ProjectileTypePluginBase>(xml,
                                                                          AssemblyPathElementName,
                                                                          package.XmlDirectoryPath,
                                                                          Name);
            typePlugin.Initialize(xml.Element(PackageManager.XMLNamespace + ExtensionElementName),
                                  package.PackageManager);
        }

        public Projectile ShootProjectile(int newID, ILevelManager level, IPlayer player, Vector3 position, Vector3 target) {
            var projectile = GetProjectile(newID, level, player, position);

            projectile.Plugin.ShootProjectile(target);

            return projectile;
        }

        public Projectile ShootProjectile(int newID, ILevelManager level, IPlayer player, Vector3 position, RangeTargetComponent target) {
            var projectile = GetProjectile(newID, level, player, position);

            projectile.Plugin.ShootProjectile(target);

            return projectile;
        }

        public ProjectileInstancePluginBase GetNewInstancePlugin(Projectile projectile, ILevelManager levelManager) {
            return typePlugin.CreateNewInstance(levelManager, projectile);
        }

        public ProjectileInstancePluginBase GetInstancePluginForLoading() {
            return typePlugin.GetInstanceForLoading();
        }

        public bool IsInRange(Vector3 source, Vector3 target) {
            return typePlugin.IsInRange(source, target);
        }

        public bool IsInRange(Vector3 source, RangeTargetComponent target) {
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


        private static Model LoadModel(XElement projectileTypeXml, string pathToPackageXmlDir) {

            string fullPath = XmlHelpers.GetFullPath(projectileTypeXml, ModelPathElementName, pathToPackageXmlDir);

            return PackageManager.Instance.ResourceCache.GetModel(fullPath);
        }

        private static Material LoadMaterial(XElement projectileTypeXml, string pathToPackageXmlDir) {
            string materialPath = XmlHelpers.GetFullPath(projectileTypeXml, MaterialPathElementName, pathToPackageXmlDir);

            return PackageManager.Instance.ResourceCache.GetMaterial(materialPath);
        }

        private Projectile GetProjectile(int newID, ILevelManager level, IPlayer player, Vector3 position) {
            Projectile projectile = null;

            if (projectilePool.Count != 0) {
                //TODO: Some clever algorithm to manage projectile count
                var pooledProjectile = projectilePool.Dequeue();
                pooledProjectile.ReInitialize(newID, level, player, position);

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

                //TODO: Move collider to plugin
                var collider = projectileNode.CreateComponent<CollisionShape>();
                collider.SetBox(new Vector3(0.2f, 0.2f, 0.8f), new Vector3(-0.1f, -0.1f, -0.4f), Quaternion.Identity);
            }

            return projectile;
        }
    }
}
