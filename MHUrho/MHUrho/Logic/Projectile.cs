﻿using System;
using System.Collections.Generic;
using System.Linq;
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
	class Projectile : Entity, IProjectile {
		class Loader : IProjectileLoader {

			static class ComponentSetup
			{
				delegate void ComponentSetupDelegate(Component component, ILevelManager level);

				static readonly Dictionary<StringHash, ComponentSetupDelegate> setupDispatch;

				static ComponentSetup()
				{
					setupDispatch = new Dictionary<StringHash, ComponentSetupDelegate>
									{
										{ RigidBody.TypeStatic, SetupRigidBody },
										{ StaticModel.TypeStatic, SetupStaticModel },
										{ AnimatedModel.TypeStatic, SetupAnimatedModel }
									};
				}


				public static void SetupComponentsOnNode(Node node, ILevelManager level)
				{
					//TODO: Maybe loop through child nodes
					foreach (var component in node.Components)
					{
						if (setupDispatch.TryGetValue(component.Type, out ComponentSetupDelegate value))
						{
							value(component, level);
						}
					}
				}

				static void SetupRigidBody(Component rigidBodyComponent, ILevelManager level)
				{
					RigidBody rigidBody = rigidBodyComponent as RigidBody;

					rigidBody.CollisionLayer = (int)CollisionLayer.Projectile;
					rigidBody.CollisionMask = (int)(CollisionLayer.Unit | CollisionLayer.Building);
					rigidBody.Kinematic = true;
					rigidBody.Mass = 1;
					rigidBody.UseGravity = false;
				}

				static void SetupStaticModel(Component staticModelComponent, ILevelManager level)
				{
					StaticModel staticModel = staticModelComponent as StaticModel;

					staticModel.CastShadows = false;
					staticModel.DrawDistance = level.App.Config.ProjectileDrawDistance;
				}

				static void SetupAnimatedModel(Component animatedModelComponent, ILevelManager level)
				{
					AnimatedModel animatedModel = animatedModelComponent as AnimatedModel;

					SetupStaticModel(animatedModel, level);
				}

				static void SetupAnimationController()
				{
					//TODO: Maybe add animation controller
				}
			}

			public IProjectile Projectile => loadingProjectile;

			Projectile loadingProjectile;

			List<DefaultComponentLoader> componentLoaders;

			readonly LevelManager level;
			readonly StProjectile storedProjectile;
			readonly ProjectileType type;

			const string NodeName = "ProjectileNode";

			public Loader(LevelManager level,
						StProjectile storedProjectile)
			{
				this.level = level;
				this.storedProjectile = storedProjectile;
				this.componentLoaders = new List<DefaultComponentLoader>();

				type = PackageManager.Instance.ActivePackage.GetProjectileType(storedProjectile.TypeID);
				if (type == null) {
					throw new ArgumentException($"Projectile type {storedProjectile.TypeID} was not loaded");
				}
			}

			public static Projectile CreateNew(int ID,
												ILevelManager level,
												IPlayer player,
												Vector3 position,
												ProjectileType type)
			{

				//TODO: Maybe pass rotation as an argument
				Node projectileNode = type.Assets.Instantiate(level, position, Quaternion.Identity);
				projectileNode.Name = NodeName;

				ComponentSetup.SetupComponentsOnNode(projectileNode, level);

				var projectile = new Projectile(ID, level, type, player);
				projectileNode.AddComponent(projectile);

				//AddBasicComponents(projectile, level);

				projectileNode.NodeCollisionStart += projectile.CollisionHandler;

				projectile.ProjectilePlugin = type.GetNewInstancePlugin(projectile, level);

				return projectile;
			}

			public static StProjectile Save(Projectile projectile)
			{
				var stProjectile = new StProjectile
									{
										Id = projectile.ID,
										Position = projectile.Node.Position.ToStVector3(),
										PlayerID = projectile.Player.ID,
										TypeID = projectile.ProjectileType.ID,
										UserPlugin = new PluginData()
									};

				projectile.ProjectilePlugin.SaveState(new PluginDataWrapper(stProjectile.UserPlugin, projectile.Level));

				foreach (var component in projectile.Node.Components) {
					var defaultComponent = component as DefaultComponent;
					if (defaultComponent != null) {
						stProjectile.DefaultComponents.Add(defaultComponent.SaveState());
					}
				}

				return stProjectile;
			}


			public void StartLoading()
			{
				if (type.ID != storedProjectile.TypeID) {
					throw new ArgumentException("provided type is not the type of the stored projectile");
				}

				Vector3 position = storedProjectile.Position.ToVector3();

				var instanceID = storedProjectile.Id;

				//TODO: Store rotation
				Node projectileNode = type.Assets.Instantiate(level, position, Quaternion.Identity);
				projectileNode.Name = NodeName;

				ComponentSetup.SetupComponentsOnNode(projectileNode, level);

				loadingProjectile = new Projectile(instanceID, level, type);
				projectileNode.AddComponent(loadingProjectile);


				projectileNode.NodeCollisionStart += loadingProjectile.CollisionHandler;

				loadingProjectile.ProjectilePlugin = loadingProjectile.ProjectileType.GetInstancePluginForLoading(loadingProjectile, level);

				foreach (var defaultComponent in storedProjectile.DefaultComponents) {
					var componentLoader =
						level.DefaultComponentFactory
							.StartLoadingComponent(defaultComponent,
													level,
													loadingProjectile.ProjectilePlugin);

					componentLoaders.Add(componentLoader);
					loadingProjectile.AddComponent(componentLoader.Component);
				}
			}

			public void ConnectReferences() {
				loadingProjectile.Player = level.GetPlayer(storedProjectile.PlayerID);

				foreach (var componentLoader in componentLoaders) {
					componentLoader.ConnectReferences();
				}

				loadingProjectile.ProjectilePlugin.LoadState(new PluginDataWrapper(storedProjectile.UserPlugin, level));


			}

			public void FinishLoading()
			{
				foreach (var componentLoader in componentLoaders) {
					componentLoader.FinishLoading();
				}
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


		public static IProjectileLoader GetLoader(LevelManager level, StProjectile storedProjectile)
		{
			return new Loader(level, storedProjectile);
		}

		public static Projectile CreateNew(int ID,
											ILevelManager level,
											IPlayer player,
											Vector3 position,
											ProjectileType type)
		{
			return Loader.CreateNew(ID, level, player, position, type);
		}

		public void ReInitialize(int newID, ILevelManager level, IPlayer player, Vector3 position) {
			ID = newID;
			Enabled = true;
			Node.NodeCollisionStart += CollisionHandler;
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

			Node.NodeCollisionStart -= CollisionHandler;
			Level.RemoveProjectile(this);
			if (!ProjectileType.ProjectileDespawn(this)) {
				Plugin.Dispose();
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

		public override void HitBy(IEntity other, object userData)
		{
			throw new InvalidOperationException("Projectiles should not hit each other");
		}

		void IDisposable.Dispose()
		{
			RemoveFromLevel();
		}

		protected override void OnUpdate(float timeStep) 
		{

			if (!EnabledEffective || !Level.LevelNode.Enabled) {
				return;
			}

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
