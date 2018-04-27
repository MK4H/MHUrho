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
using MHUrho.UnitComponents;

namespace MHUrho.Logic
{
	public class Projectile : Entity {
		internal class Loader : ILoader {

			public Projectile Projectile;

			List<DefaultComponentLoader> componentLoaders;

			StProjectile storedProjectile;

			protected Loader(StProjectile storedProjectile)
			{
				this.storedProjectile = storedProjectile;
				this.componentLoaders = new List<DefaultComponentLoader>();
			}

			public static StProjectile Save(Projectile projectile)
			{
				var stProjectile = new StProjectile {
														Position = projectile.Node.Position.ToStVector3(),
														PlayerID = projectile.Player.ID,
														TypeID = projectile.ProjectileType.ID,
														UserPlugin = new PluginData()
													};
				projectile.Plugin.SaveState(new PluginDataWrapper(stProjectile.UserPlugin));

				return stProjectile;
			}


			public static Loader StartLoading(LevelManager level,
											ProjectileType type,
											Node node,
											StProjectile storedProjectile)
			{
				var loader = new Loader(storedProjectile);
				loader.Load(level, type, node);

				return loader;

			}

			public void ConnectReferences(LevelManager level) {
				Projectile.Player = level.GetPlayer(storedProjectile.PlayerID);

				foreach (var componentLoader in componentLoaders) {
					componentLoader.ConnectReferences(level);
				}

				Projectile.Plugin.LoadState(level,
											Projectile,
											new PluginDataWrapper(storedProjectile.UserPlugin));


			}

			public void FinishLoading()
			{
				storedProjectile = null;

				foreach (var componentLoader in componentLoaders) {
					componentLoader.FinishLoading();
				}
			}

			void Load(LevelManager level,
					ProjectileType type,
					Node node) {
				if (type.ID != storedProjectile.TypeID) {
					throw new ArgumentException("provided type is not the type of the stored projectile");
				}

				var instanceID = storedProjectile.Id;

				node.Position = storedProjectile.Position.ToVector3();

				Projectile = new Projectile(instanceID, level, type);

				Projectile.Plugin = Projectile.ProjectileType.GetInstancePluginForLoading();

				foreach (var defaultComponent in storedProjectile.DefaultComponentData) {
					var componentLoader =
						level.DefaultComponentFactory
							.StartLoadingComponent(defaultComponent.Key,
													defaultComponent.Value,
													level,
													Projectile.Plugin);

					componentLoaders.Add(componentLoader);
					Projectile.AddComponent(componentLoader.Component);
				}
			}
		}

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


		

		
		protected Projectile(int ID, ILevelManager level, ProjectileType type, IPlayer player)
			:base(ID,level)
		{
			ReceiveSceneUpdates = true;
			this.Player = player;
			this.ProjectileType = type;
		}

		protected Projectile(int ID,
							 ILevelManager level,
							 ProjectileType type)
			: base(ID,level)
		{
			ReceiveSceneUpdates = true;
			this.ProjectileType = type;

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

		

		public StProjectile Save()
		{
			return Loader.Save(this);
		}


		public void Despawn() 
		{
			if (!ProjectileType.ProjectileDespawn(this)) {
				Node.Remove();
			}
			else {
				Node.Enabled = false;
			}
		}

		protected override void OnUpdate(float timeStep) 
		{

			if (!EnabledEffective) return;

			Plugin.OnUpdate(timeStep);
		}


	}
}
