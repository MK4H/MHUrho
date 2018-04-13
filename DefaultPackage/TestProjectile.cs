using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Urho;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace DefaultPackage
{
    public class TestProjectileType : ProjectileTypePluginBase {
        public override bool IsMyType(string typeName) {
            return typeName == "TestProjectile";
        }

        public override ProjectileInstancePluginBase CreateNewInstance(ILevelManager level, Projectile projectile) {
            return new TestProjectileInstance(level, projectile);
        }

        public override ProjectileInstancePluginBase GetInstanceForLoading() {
            throw new NotImplementedException();
        }

        public override void Initialize(XElement extensionElement, PackageManager packageManager) {
            
        }
    }

    public class TestProjectileInstance : ProjectileInstancePluginBase 
    {
        private const float baseTimeToSplit = 0.5f;
        private float timeToSplit = baseTimeToSplit;

        private ILevelManager level;
        private Projectile projectile;

        private Random rng;

        private int splits = 10;

        public TestProjectileInstance(ILevelManager level, Projectile projectile) {
            this.level = level;
            this.projectile = projectile;
            this.rng = new Random();
        }

        public override void OnUpdate(float timeStep) {
            timeToSplit -= timeStep;
            if (timeToSplit > 0) return;

            timeToSplit = baseTimeToSplit;

            for (int i = 0; i < splits; i++) {
                var movement = projectile.Movement;

                movement = new Quaternion((float)rng.NextDouble() * 5, (float)rng.NextDouble() * 5, (float)rng.NextDouble() * 5) * movement;

                var newProjectile = projectile.ProjectileType.SpawnProjectile(level, level.Scene, projectile.Node.Position, movement);
                ((TestProjectileInstance) newProjectile.Plugin).splits = 0;
                
            }

            if (splits != 0) {
                splits = 0;
                projectile.Despawn();
            }
            
        }

        public override void SaveState(PluginDataWrapper pluginData) {
            throw new NotImplementedException();
        }

        public override void LoadState(ILevelManager level, Projectile projectile, PluginDataWrapper pluginData) {
            throw new NotImplementedException();
        }

        public override void ReInitialize(ILevelManager level) {
            timeToSplit = baseTimeToSplit;
            splits = 10;
        }
    }
}
