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
using MHUrho.DefaultComponents;

namespace MHUrho.Logic
{
	class Projectile : Entity, IProjectile {
		class Loader : IProjectileLoader {

			/// <summary>
			/// Loading projectile
			/// </summary>
			public IProjectile Projectile => loadingProjectile;

			/// <summary>
			/// Loading projectile
			/// </summary>
			Projectile loadingProjectile;

			/// <summary>
			/// Loaders of the default components stored with the projectile.
			/// </summary>
			readonly List<DefaultComponentLoader> componentLoaders;

			/// <summary>
			/// The level the projectile is being loaded into.
			/// </summary>
			readonly LevelManager level;

			/// <summary>
			/// Stored data of the projectile.
			/// </summary>
			readonly StProjectile storedProjectile;

			/// <summary>
			/// Type of the loading projectile.
			/// </summary>
			readonly ProjectileType type;

			const string NodeName = "ProjectileNode";

			/// <summary>
			/// Creates a loader that loads the <paramref name="storedProjectile"/> into the <paramref name="level"/>.
			/// </summary>
			/// <param name="level">The level the projectile is being loaded into.</param>
			/// <param name="storedProjectile">The stored data of the projectile.</param>
			public Loader(LevelManager level,
						StProjectile storedProjectile)
			{
				this.level = level;
				this.storedProjectile = storedProjectile;
				this.componentLoaders = new List<DefaultComponentLoader>();

				type = level.Package.GetProjectileType(storedProjectile.TypeID);
				if (type == null) {
					throw new ArgumentException($"Projectile type {storedProjectile.TypeID} was not loaded");
				}
			}

			/// <summary>
			/// Creates new projectile in the level.
			/// </summary>
			/// <param name="ID">The id of the new projectile.</param>
			/// <param name="level">Level the projectile is created in.</param>
			/// <param name="player">Owner of the new projectile.</param>
			/// <param name="position">Initial position of the projectile.</param>
			/// <param name="rotation">Initial rotation of the projectile.</param>
			/// <param name="type">Type of the projectile.</param>
			/// <returns>New projectile created in the level.</returns>
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

			/// <summary>
			/// Stores the projectile into an instance of <see cref="StProjectile"/> for serialization.
			/// </summary>
			/// <param name="projectile">The projectile to store.</param>
			/// <returns>Stored projectile in an instance of <see cref="StProjectile"/> for serialization</returns>
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

			/// <inheritdoc />
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

			/// <inheritdoc />
			public void ConnectReferences() {
				loadingProjectile.Player = level.GetPlayer(storedProjectile.PlayerID);

				foreach (var componentLoader in componentLoaders) {
					componentLoader.ConnectReferences();
				}

				loadingProjectile.ProjectilePlugin.LoadState(new PluginDataWrapper(storedProjectile.UserPlugin, level));


			}

			/// <inheritdoc />
			public void FinishLoading()
			{
				foreach (var componentLoader in componentLoaders) {
					componentLoader.FinishLoading();
				}
			}
		}

		/// <inheritdoc />
		public ProjectileType ProjectileType { get; private set; }

		/// <inheritdoc />
		public override IEntityType Type => ProjectileType;

		/// <inheritdoc />
		public override Vector3 Position {
			get => Node.Position;
			protected set => Node.Position = value;
		}

		/// <inheritdoc />
		public override Vector3 Forward => Node.WorldDirection;

		/// <inheritdoc />
		public override Vector3 Backward => -Forward;

		/// <inheritdoc />
		public override Vector3 Right => Node.WorldRight;

		/// <inheritdoc />
		public override Vector3 Left => -Right;

		/// <inheritdoc />
		public override Vector3 Up => Node.WorldUp;

		/// <inheritdoc />
		public override Vector3 Down => -Up;

		/// <inheritdoc />
		public override InstancePlugin Plugin => ProjectilePlugin;

		/// <inheritdoc />
		public ProjectileInstancePlugin ProjectilePlugin { get; private set; }

		/// <inheritdoc />
		public bool FaceInTheDirectionOfMovement { get; set; }

		/// <inheritdoc />
		public bool TriggerCollisions { get; set; }

		/// <summary>
		/// If the projectile is currently stored in a pool, waiting for reinitialization
		/// </summary>
		bool isPooled;

		/// <summary>
		/// Creates a projectile.
		/// </summary>
		/// <param name="ID">ID of the new projectile.</param>
		/// <param name="level">Level the projectile will be in.</param>
		/// <param name="type">The type of the projectile.</param>
		/// <param name="player">Owner of the projectile.</param>
		protected Projectile(int ID, ILevelManager level, ProjectileType type, IPlayer player)
			:base(ID,level)
		{
			ReceiveSceneUpdates = true;
			this.Player = player;
			this.ProjectileType = type;
			this.FaceInTheDirectionOfMovement = true;
			this.TriggerCollisions = true;
			this.isPooled = false;
		}

		/// <summary>
		/// Creates a projectile.
		/// </summary>
		/// <param name="ID">ID of the new projectile.</param>
		/// <param name="level">Level the projectile will be in.</param>
		/// <param name="type">The type of the projectile.</param>
		protected Projectile(int ID,
							 ILevelManager level,
							 ProjectileType type)
			: base(ID,level)
		{
			ReceiveSceneUpdates = true;
			this.ProjectileType = type;
			this.FaceInTheDirectionOfMovement = true;
			this.TriggerCollisions = true;
			this.isPooled = false;
		}


		/// <summary>
		/// Returns loader that will load the <paramref name="storedProjectile"/> into the <paramref name="level"/>.
		/// </summary>
		/// <param name="level">The level to load the projectile into.</param>
		/// <param name="storedProjectile">The stored projectile data.</param>
		/// <returns>Loader that will load the stored projectile.</returns>
		public static IProjectileLoader GetLoader(LevelManager level, StProjectile storedProjectile)
		{
			return new Loader(level, storedProjectile);
		}

		/// <summary>
		/// Creates new projectile in the level.
		/// </summary>
		/// <param name="ID">The id of the new projectile.</param>
		/// <param name="level">Level the projectile is created in.</param>
		/// <param name="player">Owner of the new projectile.</param>
		/// <param name="position">Initial position of the projectile.</param>
		/// <param name="rotation">Initial rotation of the projectile.</param>
		/// <param name="type">Type of the projectile.</param>
		/// <returns>New projectile created in the level.</returns>
		public static Projectile CreateNew(int ID,
											ILevelManager level,
											IPlayer player,
											Vector3 position,
											Quaternion rotation,
											ProjectileType type)
		{
			return Loader.CreateNew(ID, level, player, position, rotation, type);
		}

		/// <inheritdoc />
		public void ReInitialize(int newID, ILevelManager level, IPlayer player, Vector3 position) {
			ID = newID;
			Enabled = true;
			Node.NodeCollisionStart += CollisionHandler;
			IsRemovedFromLevel = false;
			isPooled = false;
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

		/// <inheritdoc />
		public StProjectile Save()
		{
			return Loader.Save(this);
		}

		/// <inheritdoc />
		public override void RemoveFromLevel() 
		{
			if (IsRemovedFromLevel && isPooled) {
				HardRemove();
				isPooled = false;
				return;
			}

			if (IsRemovedFromLevel) return;
			base.RemoveFromLevel();

		
			Node.NodeCollisionStart -= CollisionHandler;


			Level.RemoveProjectile(this);

			if (!ProjectileType.ProjectileDespawn(this)) {
				//If dispose was called before plugin was loaded, we need dispose to work
				isPooled = false;
				HardRemove();
			}
			else {
				Enabled = false;
				Node.Enabled = false;
				isPooled = true;
			}
		}

		/// <inheritdoc />
		public override void Accept(IEntityVisitor visitor)
		{
			visitor.Visit(this);
		}

		/// <inheritdoc />
		public override T Accept<T>(IEntityVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}

		/// <summary>
		/// Removes the projectile from the level completely, doesn't just put it into a pool.
		/// </summary>
		public void HardRemove()
		{
			if (!IsRemovedFromLevel) {
				RemoveFromLevel();
			}

			try
			{
				Plugin?.Dispose();
			}
			catch (Exception e)
			{
				//Log and ignore
				Urho.IO.Log.Write(LogLevel.Error, $"Projectile  plugin call {nameof(Plugin.Dispose)} failed with Exception: {e.Message}");
			}
			
			if (!IsDeleted)
			{
				Node.Remove();
				base.Dispose();
			}
		}

		/// <inheritdoc />
		public bool Move(Vector3 movement)
		{
			
			Position += movement;
			SignalPositionChanged();

			if (FaceInTheDirectionOfMovement) {
				Node.LookAt(Position + movement, Node.Up);
				SignalRotationChanged();
			}

			if (!Level.Map.IsInside(Position)) {
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

		/// <inheritdoc />
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public override void HitBy(IEntity other, object userData)
		{
			throw new InvalidOperationException("Projectiles should not hit each other");
		}

		/// <inheritdoc />
		void IDisposable.Dispose()
		{
			RemoveFromLevel();
		}

		/// <summary>
		/// Handles scene update.
		/// </summary>
		/// <param name="timeStep">Time elapsed since the last scene update.</param>
		protected override void OnUpdate(float timeStep) 
		{

			if (IsDeleted || !EnabledEffective || !Level.LevelNode.Enabled) {
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

		/// <summary>
		/// Handles collisions with other entities besides projectiles.
		/// </summary>
		/// <param name="args">Data of the collision event.</param>
		void CollisionHandler(NodeCollisionStartEventArgs args)
		{
			if (TriggerCollisions) {
				IEntity hitEntity = Level.GetEntity(args.OtherNode);
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
