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
    public class Projectile : EntityInstanceBase {
        public delegate bool OnDespawn(Projectile projectile);

        public ProjectileType ProjectileType { get; private set; }

        /// <summary>
        /// Cast it to your own type, the one returned by <see cref="ProjectileTypePluginBase.CreateNewInstance(ILevelManager, Projectile)"/>
        /// for this type with name <see cref="ProjectileTypePluginBase.IsMyType(string)"/>
        /// </summary>
        public ProjectileInstancePluginBase Plugin { get; private set; }

        private StProjectile storedProjectile;

        
        protected Projectile(ILevelManager level, ProjectileType type, IPlayer player)
            :base(level)
        {
            ReceiveSceneUpdates = true;
            this.Player = player;
            this.ProjectileType = type;
        }

        protected Projectile(ILevelManager level,
                             ProjectileType type,
                             StProjectile storedProjectile)
            : base(level)
        {
            ReceiveSceneUpdates = true;
            this.ProjectileType = type;
            this.storedProjectile = storedProjectile;

        }

        internal static Projectile Load(ILevelManager level,
                                        ProjectileType type,
                                        Node node,
                                        OnDespawn onDespawn,
                                        StProjectile storedProjectile) {

            if (type.ID != storedProjectile.TypeID) {
                throw new ArgumentException("provided type is not the type of the stored projectile");
            }

            node.Position = storedProjectile.Position.ToVector3();

            var projectile = new Projectile(level, type, storedProjectile);

            return projectile;
        }

        internal static Projectile SpawnNew(ILevelManager level,
                                            IPlayer player,
                                            Vector3 position,
                                            ProjectileType type,
                                            Node node) {
            node.Position = position;
            var projectile = new Projectile(level, type, player);
            node.AddComponent(projectile);

            projectile.Plugin = type.GetNewInstancePlugin(projectile, level);

            return projectile;
        }

        public void ReInitialize(ILevelManager level, IPlayer player, Vector3 position) {
            Node.Enabled = true;
            Node.Position = position;
            this.Player = player;
            Plugin.ReInitialize(level);
        }

        public void ConnectReferences(ILevelManager level) {
            Player = level.GetPlayer(storedProjectile.PlayerID);

            Plugin = ProjectileType.GetInstancePluginForLoading();
            Plugin.LoadState(level, 
                             this, 
                             new PluginDataWrapper(storedProjectile.UserPlugin));

            storedProjectile = null;
        }

        public StProjectile Save() {
            var stProjectile = new StProjectile();
            stProjectile.Position = Node.Position.ToStVector3();
            stProjectile.PlayerID = Player.ID;
            stProjectile.TypeID = ProjectileType.ID;
            Plugin.SaveState(new PluginDataWrapper(stProjectile.UserPlugin));

            return stProjectile;
        }


        public void Despawn() {
            if (!ProjectileType.ProjectileDespawn(this)) {
                Node.Remove();
            }
            else {
                Node.Enabled = false;
            }
        }

        protected override void OnUpdate(float timeStep) {

            if (!EnabledEffective) return;

            Plugin.OnUpdate(timeStep);


            
        }


    }
}
