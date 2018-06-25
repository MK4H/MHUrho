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
	public class Projectile : Entity, IProjectile {
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

				projectile.ProjectilePlugin = type.GetNewInstancePlugin(projectile, level);

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
				projectile.ProjectilePlugin.SaveState(new PluginDataWrapper(stProjectile.UserPlugin, projectile.Level));

				return stProjectile;
			}


			public static Loader StartLoading(LevelManager level,
											Node node,
											StProjectile storedProjectile)
			{
				var type = PackageManager.Instance.ActiveGame.GetProjectileType(storedProjectile.TypeID);
				if (type == null) {
					throw new ArgumentException($"Projectile type {storedProjectile.TypeID} was not loaded");
				}
				var loader = new Loader(storedProjectile);
				loader.Load(level, type, node);

				return loader;

			}

			public void ConnectReferences(LevelManager level) {
				Projectile.Player = level.GetPlayer(storedProjectile.PlayerID);

				foreach (var componentLoader in componentLoaders) {
					componentLoader.ConnectReferences(level);
				}

				Projectile.ProjectilePlugin.LoadState(level,
											Projectile,
											new PluginDataWrapper(storedProjectile.UserPlugin, level));


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

				Projectile.ProjectilePlugin = Projectile.ProjectileType.GetInstancePluginForLoading();

				foreach (var defaultComponent in storedProjectile.DefaultComponentData) {
					var componentLoader =
						level.DefaultComponentFactory
							.StartLoadingComponent(defaultComponent.Key,
													defaultComponent.Value,
													level,
													Projectile.ProjectilePlugin);

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
		


		public override Vector3 Position {
			get => Node.Position;
			protected set => Node.Position = value;
		}

		public override Vector3 Forward => Node.WorldDirection;

		public override Vector3 Backward => -Forward;

		public override Vector3 Right => Node.WorldRight;

		public override Vector3 Left => -Right;

		public override Vector3 Up => Node.WorldUp;

		public override Vector3 Down => -Up;

		public override InstancePlugin Plugin => ProjectilePlugin;


		public ProjectileInstancePlugin ProjectilePlugin { get; private set; }
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
			RemovedFromLevel = false;
			Node.Enabled = true;
			Node.Position = position;
			this.Player = player;
			ProjectilePlugin.ReInitialize(level);
		}

		

		public StProjectile Save()
		{
			return Loader.Save(this);
		}


		public override void RemoveFromLevel() 
		{
			Enabled = false;
			if (RemovedFromLevel) return;
			base.RemoveFromLevel();

			Plugin.Dispose();
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

		public override void Accept(IEntityVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override T Accept<T>(IEntityVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}

		public bool Move(Vector3 movement)
		{
			if (!Map.IsInside(Position)) {
				ProjectilePlugin.OnTerrainHit();
				return false;
			}

			Position += movement;
			SignalPositionChanged();

			if (FaceInTheDirectionOfMovement) {
				Node.LookAt(Position + movement, Node.Up);
				SignalRotationChanged();
			}

			if (!Map.IsInside(Position)) {
				ProjectilePlugin.OnTerrainHit();
				return false;
			}

			return true;	
		}

		public override void HitBy(IProjectile projectile)
		{
			throw new InvalidOperationException("Projectiles should not hit each other");
		}

		void IDisposable.Dispose()
		{
			RemoveFromLevel();
		}

		protected override void OnUpdate(float timeStep) 
		{

			if (!EnabledEffective) return;

			ProjectilePlugin.OnUpdate(timeStep);
		}

		void CollisionHandler(NodeCollisionStartEventArgs e)
		{
			IEntity hitEntity = Level.GetEntity(e.OtherNode);
			hitEntity.HitBy(this);
			ProjectilePlugin.OnEntityHit(hitEntity);
		}

	}
}
