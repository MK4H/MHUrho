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
	public class Projectile : Entity {
		public delegate bool OnDespawn(Projectile projectile);

		public ProjectileType ProjectileType { get; private set; }

		/// <summary>
		/// Cast it to your own type, the one returned by <see cref="ProjectileTypePluginBase.CreateNewInstance(ILevelManager, Projectile)"/>
		/// for this type with name <see cref="ProjectileTypePluginBase.IsMyType(string)"/>
		/// </summary>
		public ProjectileInstancePluginBase Plugin { get; private set; }


		public override Vector3 Position {
			get => Node.Position;
			protected set => Node.Position = value;
		}


		private StProjectile storedProjectile;

		
		protected Projectile(int ID, ILevelManager level, ProjectileType type, IPlayer player)
			:base(ID,level)
		{
			ReceiveSceneUpdates = true;
			this.Player = player;
			this.ProjectileType = type;
		}

		protected Projectile(int ID,
							 ILevelManager level,
							 ProjectileType type,
							 StProjectile storedProjectile)
			: base(ID,level)
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

			var instanceID = storedProjectile.Id;

			node.Position = storedProjectile.Position.ToVector3();

			var projectile = new Projectile(instanceID, level, type, storedProjectile);

			return projectile;
		}

		internal static Projectile SpawnNew(int ID,
											ILevelManager level,
											IPlayer player,
											Vector3 position,
											ProjectileType type,
											Node node) {
			node.Position = position;
			var projectile = new Projectile(ID, level, type, player);
			node.AddComponent(projectile);

			projectile.Plugin = type.GetNewInstancePlugin(projectile, level);

			return projectile;
		}

		public void ReInitialize(int newID, ILevelManager level, IPlayer player, Vector3 position) {
			ID = newID;
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
			var stProjectile = new StProjectile {
				Position = Node.Position.ToStVector3(),
				PlayerID = Player.ID,
				TypeID = ProjectileType.ID,
				UserPlugin = new PluginData()
			};
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
