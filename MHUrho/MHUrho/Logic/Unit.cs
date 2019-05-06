using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Google.Protobuf;
using MHUrho.Control;
using MHUrho.EntityInfo;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;
using Urho.Physics;


namespace MHUrho.Logic
{
	/// <summary>
	/// Class representing unit, every action you want to do with the unit should go through this class
	/// </summary>
	class Unit : Entity, IUnit {
		class Loader : IUnitLoader {			

			public IUnit Unit => loadingUnit;

			Unit loadingUnit;

			List<DefaultComponentLoader> defComponentLoaders;

			readonly LevelManager level;
			readonly UnitType type;
			/// <summary>
			/// Holds the image of this unit between the steps of loading
			/// After the last step, is set to null to free the resources
			/// In game is null
			/// </summary>
			readonly StUnit storedUnit;

			const string NodeName = "UnitNode";

			/// <summary>
			/// Loads unit component from <paramref name="storedUnit"/> and all other needed components
			/// </summary>
			/// <param name="level"></param>
			/// <param name="storedUnit">stored unit</param>
			public Loader(LevelManager level, StUnit storedUnit) {
				this.level = level;
				this.storedUnit = storedUnit;
				this.defComponentLoaders = new List<DefaultComponentLoader>();

				type = PackageManager.Instance.ActivePackage.GetUnitType(storedUnit.TypeID);
				if (type == null) {
					throw new ArgumentException("Type of this unit was not loaded");
				}
			}

			/// <summary>
			/// Creates new instance of the <see cref="loadingUnit"/> component and ads it to the <paramref name="unitNode"/>
			/// Also adds the unit as PassingUnit to <paramref name="tile"/>
			/// </summary>
			/// <param name="id">The unique identifier of the unit, must be unique among other units</param>
			/// <param name="level">LevelManager of the level into which the unit is spawning</param>
			/// <param name="type">type of the unit</param>
			/// <param name="tile">tile where the unit will spawn</param>
			/// <param name="rotation">Initial rotation of the unit</param>
			/// <param name="player">owner of the unit</param>
			/// <returns>the unit component, already added to the node</returns>
			/// <exception cref="CreationException">Throws an exception when unit creation fails</exception>
			public static Unit CreateNew(int id, UnitType type, ILevelManager level, ITile tile, Quaternion rotation, IPlayer player) {
				Vector3 position = new Vector3(tile.Center.X,
												level.Map.GetHeightAt(tile.Center.X, tile.Center.Y),
												tile.Center.Y);
				Node unitNode = null;
				try {
					unitNode = type.Assets.Instantiate(level, position, rotation);
					unitNode.Name = NodeName;
				}
				catch (Exception e) {
					string message = $"There was an Exception while creating a new unit: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new CreationException(message, e);
				}
				

				try {
					new UnitComponentSetup().SetupComponentsOnNode(unitNode, level);
					var unit = new Unit(id, level, type, tile, player, unitNode);
					unitNode.AddComponent(unit);

					unit.UnitPlugin = type.GetNewInstancePlugin(unit, level);
					return unit;
				}
				catch (Exception e) {
					unitNode.Remove();
					unitNode.Dispose();

					string message = $"There was an Exception while creating a new unit: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new CreationException(message, e);
				}			
			}

			public static StUnit Save(Unit unit) {
				var storedUnit = new StUnit
								{
									Id = unit.ID,
									Position = unit.Position.ToStVector3(),
									Rotation = unit.Node.Rotation.ToStQuaternion(),
									PlayerID = unit.Player.ID,
									TypeID = unit.UnitType.ID,
									UserPlugin = new PluginData()
								};

				try {
					unit.UnitPlugin.SaveState(new PluginDataWrapper(storedUnit.UserPlugin, unit.Level));
				}
				catch (Exception e)
				{
					string message = $"Saving unit plugin failed with Exception: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new SavingException(message, e);
				}

				foreach (var component in unit.Node.Components) {
					var defaultComponent = component as DefaultComponent;
					if (defaultComponent != null) {
						storedUnit.DefaultComponents.Add(defaultComponent.SaveState());
					}
				}

				return storedUnit;
			}

			/// <summary>
			/// Loads ONLY the unit component of <paramref name="type"/> from <paramref name="storedUnit"/> and adds it to the <paramref name="legNode"/> 
			/// If you use this, you still need to add Model, Materials and other behavior to the unit
			/// </summary>
			/// <returns>Loaded unit component, already added to the node</returns>
			public void StartLoading() {
				//TODO: Check arguments - node cant have more than one Unit component
				if (type.ID != storedUnit.TypeID) {
					throw new ArgumentException("provided type is not the type of the stored unit", nameof(type));
				}

				Vector3 position = storedUnit.Position.ToVector3();
				Quaternion rotation = storedUnit.Rotation.ToQuaternion();

				Node centerNode = type.Assets.Instantiate(level, position, rotation);
				centerNode.Name = NodeName;

				new UnitComponentSetup().SetupComponentsOnNode(centerNode, level);

				var unitID = storedUnit.Id;

				loadingUnit = new Unit(unitID, level, type, centerNode);
				centerNode.AddComponent(loadingUnit);

				//Unit is automatically registered by the loaders
				//entityRegistry.Register(loadingUnit);


				loadingUnit.UnitPlugin = type.GetInstancePluginForLoading(loadingUnit, level);

				foreach (var defaultComponent in storedUnit.DefaultComponents) {
					var componentLoader =
						level.DefaultComponentFactory
							.StartLoadingComponent(defaultComponent,
													level,
													loadingUnit.UnitPlugin);
					defComponentLoaders.Add(componentLoader);
					loadingUnit.AddComponent(componentLoader.Component);
				}

			}

			/// <summary>
			/// Continues loading by connecting references and loading components
			/// </summary>
			public void ConnectReferences() {
				loadingUnit.Player = level.GetPlayer(storedUnit.PlayerID);
				loadingUnit.Tile = level.Map.GetContainingTile(loadingUnit.Position);
				
				foreach (var componentLoader in defComponentLoaders) {
					componentLoader.ConnectReferences();
				}

				loadingUnit.UnitPlugin.LoadState(new PluginDataWrapper(storedUnit.UserPlugin, level));
			}

			public void FinishLoading() {
				foreach (var componentLoader in defComponentLoaders) {
					componentLoader.FinishLoading();
				}
			}

		}

		#region Public members
		public UnitType UnitType { get; private set;}

		public override Vector3 Position {
			get => LegNode.Position;
			protected set => LegNode.Position = value;
		}

		public override InstancePlugin Plugin => UnitPlugin;

		/// <summary>
		/// Tile this unit is standing on
		/// TODO: Maybe include all the tiles this unit touches, which may be up to 4 tiles
		/// </summary>
		public ITile Tile { get; private set; }

		public UnitInstancePlugin UnitPlugin { get; private set; }

		public bool AlwaysVertical { get; set; } = false;

		/// <summary>
		/// Node used for movement and collisions with the terrain and buildings
		/// </summary>
		public Node LegNode { get; private set; }

		/// <summary>
		/// Node used for model, collisions with projectiles and logic
		/// </summary>
		public Node CenterNode => Node;

		public override Vector3 Forward => LegNode.WorldDirection;

		public override Vector3 Backward => -Forward;

		public override Vector3 Right => LegNode.WorldRight;

		public override Vector3 Left => -Right;

		public override Vector3 Up => LegNode.WorldUp;

		public override Vector3 Down => -Up;

		#endregion

		#region Private members



		#endregion

		#region Constructors

		/// <summary>
		/// Initializes everything apart from the things referenced by their ID or position
		/// </summary>
		/// <param name="type">type of the loading unit</param>
		protected Unit(int id, ILevelManager level, UnitType type, Node legNode)
			:base(id, level)
		{
			this.UnitType = type;
			this.LegNode = legNode;

			ReceiveSceneUpdates = true;
		}

		/// <summary>
		/// If you want to spawn new unit, call <see cref="LevelManager.SpawnUnit(Logic.UnitType,ITile,IPlayer)"/>
		/// 
		/// Constructs new instance of Unit control component
		/// </summary>
		/// <param name="id">identifier unique between units </param>
		/// <param name="type">the type of the unit</param>
		/// <param name="tile">Tile where the unit spawned</param>
		/// <param name="player">Owner of the unit</param>
		protected Unit(int id, ILevelManager level, UnitType type, ITile tile, IPlayer player, Node legNode) 
			: base(id, level)
		{
			this.Tile = tile;
			this.Player = player;
			this.UnitType = type;
			this.LegNode = legNode;

			ReceiveSceneUpdates = true;
		}

		#endregion

		#region Public methods

		public static IUnitLoader GetLoader(LevelManager level, StUnit storedUnit)
		{
			return new Loader(level, storedUnit);
		}

		public static Unit CreateNew(int id,
										UnitType type,
										ILevelManager level,
										ITile tile,
										Quaternion rotation,
										IPlayer player)
		{
			return Loader.CreateNew(id, type, level, tile, rotation, player);
		}

		public StUnit Save() {
			return Loader.Save(this);
		}

		public override void Accept(IEntityVisitor visitor) {
			visitor.Visit(this);
		}

		public override T Accept<T>(IEntityVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}

		public void SetHeight(float newHeight) {
			Position = new Vector3(Position.X, newHeight, Position.Z);
			SignalPositionChanged();
		}

		public void MoveBy(Vector3 moveBy) {
			var newPosition = Position + moveBy;

			MoveTo(newPosition);
		}

		public void MoveBy(Vector2 moveBy) {
			var newLocation = new Vector2(Position.X + moveBy.X, Position.Z + moveBy.Y);
			MoveTo(newLocation);
		}

		public void MoveTo(Vector3 newPosition) {

			FaceTowards(newPosition);
			Position = newPosition;
			ITile newTile;

			if ((newTile = Map.GetContainingTile(Position)) != Tile) {
				Tile.RemoveUnit(this);
				Tile = newTile;
				Tile.AddUnit(this);
			}

			SignalPositionChanged();
		}

		public void MoveTo(Vector2 newLocation) {
			MoveTo(new Vector3(newLocation.X, Map.GetTerrainHeightAt(newLocation), newLocation.Y));
		}

		/// <summary>
		/// Rotates the unit to face towards the <paramref name="lookPosition"/>, either directly if <see cref="AlwaysVertical"/> is false and
		/// <paramref name="rotateAroundY"/> is false, or to its projection into current the XZ plane of the Node if either of those two are true
		/// </summary>
		/// <param name="lookPosition">position to look towards</param>
		/// <param name="rotateAroundY">If <see cref="AlwaysVertical"/> is false, controls if the rotation will be only around the Y axis
		/// if <see cref="AlwaysVertical"/> is true, has no effect</param>
		public void FaceTowards(Vector3 lookPosition, bool rotateAroundY = false) {
			if (AlwaysVertical || rotateAroundY) {
				//Only rotate around Y
				LegNode.LookAt(new Vector3(lookPosition.X, Position.Y, lookPosition.Z), Node.Up);
			}
			else {
				LegNode.LookAt(lookPosition, Tile.Map.GetUpDirectionAt(Position.XZ2()));
				if (LegNode.Up.Y < 0) {
					Tile.Map.GetUpDirectionAt(Position.XZ2());
				}
			}

			SignalRotationChanged();
		}

		public void RotateAroundFeet(float pitch, float yaw, float roll) {
			LegNode.Rotate(new Quaternion(pitch, yaw, roll));
			SignalRotationChanged();
		}

		public void RotateAroundCenter(float pitch, float yaw, float roll) {
			Node.Rotate(new Quaternion(pitch, yaw, roll));
			SignalRotationChanged();
		}

		public override void RemoveFromLevel()
		{
			if (RemovedFromLevel) return;

			base.RemoveFromLevel();
			//We need removeFromLevel to work during any phase of loading, where connect references may not have been called yet
			try {
				Plugin?.Dispose();
			}
			catch (Exception e) {
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Unit plugin call {nameof(UnitPlugin.Dispose)} failed with Exception: {e.Message}");
			}
			
			Level.RemoveUnit(this);
			Tile?.RemoveUnit(this);
			Player?.RemoveUnit(this);
			LegNode.Remove();
			LegNode.Dispose();
			
			Dispose();	
		}

		public override void HitBy(IEntity other, object userData)
		{
			try {
				UnitPlugin.OnHit(other, userData);
			}
			catch (Exception e) {
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Unit plugin call {nameof(UnitPlugin.OnHit)} failed with Exception: {e.Message}");
			}
		}

		void IDisposable.Dispose()
		{
			RemoveFromLevel();
		}
		#endregion

		#region Protected Methods

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);
			//Level.LevelNode.Enabled is here because there seems to be a bug
			// where child nodes of level still receive updates even though 
			// the level node is not enabled
			if (!EnabledEffective || !Level.LevelNode.Enabled) {
				return;
			}

			try {
				UnitPlugin.OnUpdate(timeStep);
			}
			catch (Exception e) {
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Unit plugin call {nameof(UnitPlugin.OnUpdate)} failed with Exception: {e.Message}");
			}
			
		}

		#endregion

		#region Private Methods



		#endregion

	 
	}
}