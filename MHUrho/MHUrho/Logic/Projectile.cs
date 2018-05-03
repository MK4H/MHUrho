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

			public static Projectile CreateNew(int ID,
												ILevelManager level,
												IPlayer player,
												Vector3 position,
												ProjectileType type,
												Node node)
			{
				node.Position = position;
				var projectile = new Projectile(ID, level, type, player);
				node.AddComponent(projectile);

				AddBasicComponents(projectile);

				node.NodeCollisionStart += projectile.CollisionHandler;

				projectile.Plugin = type.GetNewInstancePlugin(projectile, level);

				return projectile;
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
				node.AddComponent(Projectile);

				AddBasicComponents(Projectile);

				node.NodeCollisionStart += Projectile.CollisionHandler;

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

			static void AddBasicComponents(Projectile projectile)
			{
				AddRigidBody(projectile);
				StaticModel model = AddModel(projectile.Node, projectile.ProjectileType);
				//TODO: Move collider to plugin
				var collider = projectile.Node.CreateComponent<CollisionShape>();
				collider.SetBox(model.BoundingBox.Size, Vector3.Zero, Quaternion.Identity);
			}

			static void AddRigidBody(Projectile projectile)
			{


				projectile.rigidBody = projectile.Node.CreateComponent<RigidBody>();
				projectile.rigidBody.CollisionLayer = (int)CollisionLayer.Projectile;
				projectile.rigidBody.CollisionMask = (int)(CollisionLayer.Unit | CollisionLayer.Building);
				projectile.rigidBody.Kinematic = true;
				projectile.rigidBody.Mass = 1;
				projectile.rigidBody.UseGravity = false;

				
			}

			static StaticModel AddModel(Node projectileNode, ProjectileType type)
			{
				var staticModel = type.Model.AddModel(projectileNode);
				type.Material.ApplyMaterial(staticModel);
				return staticModel;
			}
		}

		public ProjectileType ProjectileType { get; private set; }

		/// <summary>
		/// Cast it to your own type, the one returned by <see cref="ProjectileTypePlugin.CreateNewInstance(ILevelManager, Projectile)"/>
		/// for this type with name <see cref="ProjectileTypePlugin.IsMyType(string)"/>
		/// </summary>
		public ProjectileInstancePlugin Plugin { get; private set; }


		public override Vector3 Position {
			get => Node.Position;
			protected set => Node.Position = value;
		}

		/// <summary>
		/// Default true
		/// </summary>
		public bool FaceInTheDirectionOfMovement { get; set; }

		public bool TriggerCollisions {
			get => rigidBody.Enabled;
			set => rigidBody.Enabled = value;
		}

		RigidBody rigidBody;

		protected Projectile(int ID, ILevelManager level, ProjectileType type, IPlayer player)
			:base(ID,level)
		{
			ReceiveSceneUpdates = true;
			this.Player = player;
			this.ProjectileType = type;
			this.FaceInTheDirectionOfMovement = true;
		}

		protected Projectile(int ID,
							 ILevelManager level,
							 ProjectileType type)
			: base(ID,level)
		{
			ReceiveSceneUpdates = true;
			this.ProjectileType = type;
			this.FaceInTheDirectionOfMovement = true;
		}



		internal static Projectile CreateNew(int ID,
											ILevelManager level,
											IPlayer player,
											Vector3 position,
											ProjectileType type,
											Node node)
		{
			return Loader.CreateNew(ID, level, player, position, type, node);
		}

		public void ReInitialize(int newID, ILevelManager level, IPlayer player, Vector3 position) {
			ID = newID;
			Enabled = true;
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
			Enabled = false;
			Level.RemoveProjectile(this);
			if (!ProjectileType.ProjectileDespawn(this)) {
				
				Node.Remove();
				Node.Dispose();
				Dispose();
			}
			else {
				Node.Enabled = false;
			}
		}

		public bool Move(Vector3 movement)
		{
			if (!Map.IsInside(Position)) {
				Plugin.OnTerrainHit();
				return false;
			}

			Position += movement;

			if (FaceInTheDirectionOfMovement) {
				Node.LookAt(Position + movement, Node.Up);
			}

			if (!Map.IsInside(Position)) {
				Plugin.OnTerrainHit();
				return false;
			}

			return true;	
		}

		protected override void OnUpdate(float timeStep) 
		{

			if (!EnabledEffective) return;

			Plugin.OnUpdate(timeStep);
		}

		void CollisionHandler(NodeCollisionStartEventArgs e)
		{
			//TODO: Instead of linear time search implement constant time node to entity lookup
			Plugin.OnEntityHit(e.OtherNode.GetComponent<Entity>());
		}

	}
}
