using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;
using MHUrho.Helpers;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.Logic
{
    public class Projectile : Component {
        public delegate bool OnDespawn(Projectile projectile);

        public ProjectileType ProjectileType { get; private set; }

        /// <summary>
        /// Cast it to your own type, the one returned by <see cref="IProjectileTypePlugin.CreateNewInstance(ILevelManager, Projectile)"/>
        /// for this type with name <see cref="IProjectileTypePlugin.IsMyType(string)"/>
        /// </summary>
        public object Plugin => plugin;
        
        public IPlayer Player { get; private set; }

        public Vector3 Position => Node.Position;
        public Vector3 Movement { get; private set; }

        private Map map;

        private readonly float baseTimeToDespawn;
        private float timeToDespawn;

        private OnDespawn onDespawn;
        private IProjectileInstancePlugin plugin;

        private StProjectile storedProjectile;

        public static Projectile Load(ILevelManager level, 
                                      ProjectileType type, 
                                      Node node,
                                      OnDespawn onDespawn,
                                      StProjectile storedProjectile) {

            if (type.ID != storedProjectile.TypeID) {
                throw new ArgumentException("provided type is not the type of the stored projectile");
            }

            node.Position = storedProjectile.Position.ToVector3();

            var projectile = new Projectile(level, type, onDespawn, storedProjectile);

            return projectile;
        }

        public static Projectile SpawnNew(Vector3 movement,
                                          Vector3 position,
                                          ILevelManager level,
                                          ProjectileType type,
                                          Node node,
                                          OnDespawn onDespawn) {
            node.Position = position;
            var projectile = new Projectile(movement, level.Map, type, onDespawn);
            node.AddComponent(projectile);

            projectile.plugin = type.GetNewInstancePlugin(projectile, level);

            return projectile;
        }

        protected Projectile(Vector3 movement, Map map, ProjectileType type, OnDespawn onDespawn) {
            ReceiveSceneUpdates = true;
            this.map = map;
            this.Movement = movement;
            this.baseTimeToDespawn = 6;
            this.timeToDespawn = baseTimeToDespawn;
            this.ProjectileType = type;
            this.onDespawn = onDespawn;
        }

        protected Projectile(ILevelManager level,
                             ProjectileType type,  
                             OnDespawn onDespawn,
                             StProjectile storedProjectile) {
            ReceiveSceneUpdates = true;
            this.map = level.Map;
            this.ProjectileType = type;
            this.Movement = storedProjectile.Movement.ToVector3();
            this.baseTimeToDespawn = storedProjectile.BaseTimeToDespawn;
            this.timeToDespawn = baseTimeToDespawn;
            this.onDespawn = onDespawn;
            this.storedProjectile = storedProjectile;

        }

        public void ReInitialize(ILevelManager level, Vector3 position, Vector3 movement) {
            Node.Enabled = true;
            Node.Position = position;
            this.Movement = movement;
            timeToDespawn = baseTimeToDespawn;
            plugin.ReInitialize(level);
        }

        public void ConnectReferences(ILevelManager level) {
            Player = level.GetPlayer(storedProjectile.PlayerID);

            plugin = ProjectileType.GetInstancePluginForLoading();
            plugin.LoadState(level, 
                             this, 
                             new PluginDataWrapper(storedProjectile.UserPlugin));
        }

        public StProjectile Save() {
            var stProjectile = new StProjectile();
            stProjectile.Movement = Movement.ToStVector3();
            stProjectile.Position = Node.Position.ToStVector3();
            stProjectile.PlayerID = Player.ID;
            stProjectile.TypeID = ProjectileType.ID;
            stProjectile.BaseTimeToDespawn = baseTimeToDespawn;
            plugin.SaveState(new PluginDataWrapper(stProjectile.UserPlugin));

            return stProjectile;
        }


        public void Despawn() {
            if (!onDespawn(this)) {
                Node.Remove();
            }
            else {
                Node.Enabled = false;
            }
        }

        protected override void OnUpdate(float timeStep) {
            base.OnUpdate(timeStep);

            if (!EnabledEffective) return;

            if (map.IsInside(Node.Position)) {
                Node.Position += Movement * timeStep;
                Node.LookAt(Node.Position + Movement, Vector3.UnitY);

                Movement += (-Vector3.UnitY * 10) * timeStep;
                timeToDespawn = baseTimeToDespawn;

                plugin.OnUpdate(timeStep);
            }
            else {
                //Stop movement
                Movement = Vector3.Zero;
                timeToDespawn -= timeStep;
                if (timeToDespawn < 0) {
                    Despawn();
                }
            }

            
        }


    }
}
