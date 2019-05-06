using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;

namespace MHUrho.Logic
{
	class Projectile : Entity, IProjectile {
		class Loader : IProjectileLoader {

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
												Quaternion rotation,
												ProjectileType type)
			{
				Node projectileNode;
				try {
					projectileNode = type.Assets.Instantiate(level, position, rotation);
					projectileNode.Name = NodeName;
				}
				catch (Exception e) {
					string message = $"There was an Exception while creating a projectile: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new CreationException(message, e);
				}

				try {
					new ProjectileComponentSetup().SetupComponentsOnNode(projectileNode, level);

					var projectile = new Projectile(ID, level, type, player);
					projectileNode.AddComponent(projectile);

					projectileNode.NodeCollisionStart += projectile.CollisionHandler;

					projectile.ProjectilePlugin = type.GetNewInstancePlugin(projectile, level);
					return projectile;
				}
				catch (Exception e) {
					projectileNode.Remove();
					projectileNode.Dispose();

					string message = $"There was an Exception while creating a projectile: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new CreationException(message, e);
				}

				
			}

			public static StProjectile Save(Projectile projectile)
			{
				var stProjectile = new StProjectile
									{
										Id = projectile.ID,
										Position = projectile.Node.Position.ToStVector3(),
										Rotation = projectile.Node.Rotation.ToStQuaternion(),
										PlayerID = projectile.Player.ID,
										TypeID = projectile.ProjectileType.ID,
										FaceDir = projectile.FaceInTheDirectionOfMovement,
										Trigger = projectile.TriggerCollisions,
										UserPlugin = new PluginData()
									};

				try {
					projectile.ProjectilePlugin.SaveState(new PluginDataWrapper(stProjectile.UserPlugin,
																				projectile.Level));
				}
				catch (Exception e) {
					string message = $"Saving projectile plugin failed with Exception: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new SavingException(message, e);
				}


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
				Quaternion rotation = storedProjectile.Rotation.ToQuaternion();

				var instanceID = storedProjectile.Id;


				Node projectileNode = type.Assets.Instantiate(level, position, rotation);
				projectileNode.Name = NodeName;

				new ProjectileComponentSetup().SetupComponentsOnNode(projectileNode, level);

				loadingProjectile = new Projectile(instanceID, level, type)
									{
										FaceInTheDirectionOfMovement = storedProjectile.FaceDir,
										TriggerCollisions = storedProjectile.Trigger
									};
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

		public bool TriggerCollisions { get; set; }

		protected Projectile(int ID, ILevelManager level, ProjectileType type, IPlayer player)
			:base(ID,level)
		{
			ReceiveSceneUpdates = true;
			this.Player = player;
			this.ProjectileType = type;
			this.FaceInTheDirectionOfMovement = true;
			this.TriggerCollisions = true;
		}

		protected Projectile(int ID,
							 ILevelManager level,
							 ProjectileType type)
			: base(ID,level)
		{
			ReceiveSceneUpdates = true;
			this.ProjectileType = type;
			this.FaceInTheDirectionOfMovement = true;
			this.TriggerCollisions = true;
		}


		public static IProjectileLoader GetLoader(LevelManager level, StProjectile storedProjectile)
		{
			return new Loader(level, storedProjectile);
		}

		public static Projectile CreateNew(int ID,
											ILevelManager level,
											IPlayer player,
											Vector3 position,
											Quaternion rotation,
											ProjectileType type)
		{
			return Loader.CreateNew(ID, level, player, position, rotation, type);
		}

		public void ReInitialize(int newID, ILevelManager level, IPlayer player, Vector3 position) {
			ID = newID;
			Enabled = true;
			Node.NodeCollisionStart += CollisionHandler;
			RemovedFromLevel = false;
			Node.Enabled = true;
			Node.Position = position;
			this.Player = player;

			try {
				ProjectilePlugin.ReInitialize(level);
			}
			catch (Exception e) {
				string message = $"There was an Exception while reinitializing a projectile: {e.Message}";
				Urho.IO.Log.Write(LogLevel.Error, message);
				throw new CreationException(message, e);
			}
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
				//If dispose was called before plugin was loaded, we need dispose to work
				HardRemove();
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

		public void HardRemove()
		{
			if (!RemovedFromLevel) {
				RemoveFromLevel();
			}

			Plugin?.Dispose();
			Node.Remove();
			Node.Dispose();
			Dispose();
		}

		public bool Move(Vector3 movement)
		{
			
			Position += movement;
			SignalPositionChanged();

			if (FaceInTheDirectionOfMovement) {
				Node.LookAt(Position + movement, Node.Up);
				SignalRotationChanged();
			}

			if (!Map.IsInside(Position)) {
				try
				{
					ProjectilePlugin.OnTerrainHit();
				}
				catch (Exception e)
				{
					//NOTE: Maybe add cap to prevent message flood
					Urho.IO.Log.Write(LogLevel.Error, $"Projectile plugin call {nameof(ProjectilePlugin.OnTerrainHit)} failed with Exception: {e.Message}");
				}
				return false;
			}

			return true;	
		}

		public bool Shoot(IRangeTarget target)
		{
			try {
				return ProjectilePlugin.ShootProjectile(target);
			}
			catch (Exception e) {
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error,
								$"Projectile plugin call {nameof(ProjectilePlugin.ShootProjectile)} failed with Exception: {e.Message}");
				return false;
			}
		}

		public bool Shoot(Vector3 movement)
		{
			try
			{
				return ProjectilePlugin.ShootProjectile(movement);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Error,
								$"Projectile plugin call {nameof(ProjectilePlugin.ShootProjectile)} failed with Exception: {e.Message}");
				return false;
			}
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

			try
			{
				ProjectilePlugin.OnUpdate(timeStep);
			}
			catch (Exception e)
			{
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Projectile plugin call {nameof(ProjectilePlugin.OnUpdate)} failed with Exception: {e.Message}");
			}
			
		}

		void CollisionHandler(NodeCollisionStartEventArgs args)
		{
			if (TriggerCollisions) {
				IEntity hitEntity = Level.GetEntity(args.OtherNode);
				hitEntity.HitBy(this);
				try {
					ProjectilePlugin.OnEntityHit(hitEntity);
				}
				catch (Exception e) {
					//NOTE: Maybe add cap to prevent message flood
					Urho.IO.Log.Write(LogLevel.Error, $"Projectile plugin call {nameof(ProjectilePlugin.OnEntityHit)} failed with Exception: {e.Message}");
				}
				
			}
		}

	}
}
