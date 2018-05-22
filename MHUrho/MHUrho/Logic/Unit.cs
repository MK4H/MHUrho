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
	public class Unit : Entity, IUnit {
		internal class Loader : ILoader {
		   
			public Unit Unit;

			List<DefaultComponentLoader> componentLoaders;

			/// <summary>
			/// Holds the image of this unit between the steps of loading
			/// After the last step, is set to null to free the resources
			/// In game is null
			/// </summary>
			StUnit storedUnit;

			protected Loader(StUnit storedUnit) {
				this.storedUnit = storedUnit;
				this.componentLoaders = new List<DefaultComponentLoader>();
			}

			/// <summary>
			/// Creates new instance of the <see cref="Unit"/> component and ads it to the <paramref name="unitNode"/>
			/// Also adds the unit as PassingUnit to <paramref name="tile"/>
			/// </summary>
			/// <param name="id">The unique identifier of the unit, must be unique among other units</param>
			/// <param name="unitNode">Scene node of the unit</param>
			/// <param name="type">type of the unit</param>
			/// <param name="tile">tile where the unit will spawn</param>
			/// <param name="player">owner of the unit</param>
			/// <returns>the unit component, already added to the node</returns>
			public static Unit CreateNew(int id, Node unitNode, UnitType type, ILevelManager level, ITile tile, IPlayer player) {
				//TODO: Check if there is already a Unit component on this node, if there is, throw exception

				var centerNode = CreateBasicNodeStructure(unitNode, type);

				unitNode.Position = tile.Center3;
				var unit = new Unit(id, level, type, tile, player, unitNode);
				centerNode.AddComponent(unit);
				
				//TODO: TEMPORARY
				unit.HealthBar = new HealthBar(level);
				unit.HealthBar.AddToNode(centerNode, unit);


				unit.UnitPlugin = type.GetNewInstancePlugin(unit, level);

				return unit;
			}

			public static StUnit Save(Unit unit) {
				var storedUnit = new StUnit();
				storedUnit.Id = unit.ID;
				storedUnit.Position = unit.Position.ToStVector3();
				storedUnit.PlayerID = unit.Player.ID;
				storedUnit.TypeID = unit.UnitType.ID;


				storedUnit.UserPlugin = new PluginData();
				unit.UnitPlugin.SaveState(new PluginDataWrapper(storedUnit.UserPlugin));

				foreach (var component in unit.Node.Components) {
					var defaultComponent = component as DefaultComponent;
					if (defaultComponent != null) {
						storedUnit.DefaultComponentData.Add((int)defaultComponent.ComponentTypeID, defaultComponent.SaveState());
					}
				}

				return storedUnit;
			}

			/// <summary>
			/// Loads unit component from <paramref name="storedUnit"/> and all other needed components
			///  and adds them to the <paramref name="node"/>
			/// </summary>
			/// <param name="level"></param>
			/// <param name="packageManager">Package manager to get unitType</param>
			/// <param name="node">scene node of the unit</param>
			/// <param name="storedUnit">stored unit</param>
			/// <returns>Loaded unit component, already added to the node</returns>
			public static Loader StartLoading(LevelManager level, PackageManager packageManager, Node node, StUnit storedUnit) {
				var type = packageManager.ActiveGame.GetUnitType(storedUnit.TypeID);
				if (type == null) {
					throw new ArgumentException("Type of this unit was not loaded");
				}

				var unitLoader = new Loader(storedUnit);
				unitLoader.Load(level, type, node);

				return unitLoader;
			}

			
			/// <summary>
			/// Loads ONLY the unit component of <paramref name="type"/> from <paramref name="storedUnit"/> and adds it to the <paramref name="legNode"/> 
			/// If you use this, you still need to add Model, Materials and other behavior to the unit
			/// </summary>
			/// <param name="type"></param>
			/// <param name="legNode"></param>
			/// <param name="storedUnit"></param>
			/// <returns>Loaded unit component, already added to the node</returns>
			void Load(LevelManager level, UnitType type, Node legNode) {
				//TODO: Check arguments - node cant have more than one Unit component
				if (type.ID != storedUnit.TypeID) {
					throw new ArgumentException("provided type is not the type of the stored unit", nameof(type));
				}

				var centerNode = CreateBasicNodeStructure(legNode, type);

				var unitID = storedUnit.Id;

				Unit = new Unit(unitID, level, type, legNode);
				centerNode.AddComponent(Unit);

				//This is the main reason i add Unit to node right here, because i want to isolate the storedUnit reading
				// to this class, and for that i need to set the Position here
				legNode.Position = new Vector3(storedUnit.Position.X, storedUnit.Position.Y, storedUnit.Position.Z);

				Unit.UnitPlugin = type.GetInstancePluginForLoading();

				foreach (var defaultComponent in storedUnit.DefaultComponentData) {
					var componentLoader =
						level.DefaultComponentFactory
							.StartLoadingComponent(defaultComponent.Key,
													defaultComponent.Value,
													level,
													Unit.UnitPlugin);
					componentLoaders.Add(componentLoader);
					Unit.AddComponent(componentLoader.Component);
				}
			}

			/// <summary>
			/// Continues loading by connecting references and loading components
			/// </summary>
			public void ConnectReferences(LevelManager level) {
				Unit.Player = level.GetPlayer(storedUnit.PlayerID);
				Unit.Tile = level.Map.GetContainingTile(Unit.Position);
				//TODO: Connect other things

				foreach (var componentLoader in componentLoaders) {
					componentLoader.ConnectReferences(level);
				}

				Unit.UnitPlugin.LoadState(level, Unit, new PluginDataWrapper(storedUnit.UserPlugin));
			}

			public void FinishLoading() {
				foreach (var componentLoader in componentLoaders) {
					componentLoader.FinishLoading();
				}
			}

			static Node CreateBasicNodeStructure(Node legNode, UnitType type) {
				var centerNode = legNode.CreateChild("UnitCenter");

				AddRigidBody(centerNode);
				var model = AddModel(centerNode, type);
				AddAnimationController(centerNode);

				centerNode.Position = new Vector3(0, 0, 0);

				//TODO: Move collisionShape to plugin
				var collider = centerNode.CreateComponent<CollisionShape>();
				collider.SetBox(model.BoundingBox.Size, Vector3.Zero, Quaternion.Identity);

				return centerNode;
			}

			static void AddRigidBody(Node node) {
				var rigidBody = node.CreateComponent<RigidBody>();
				rigidBody.CollisionLayer = (int)CollisionLayer.Unit;
				rigidBody.CollisionMask = (int)CollisionLayer.Projectile;
				rigidBody.Kinematic = true;
				rigidBody.Mass = 1;
				rigidBody.UseGravity = false;
			}

			static StaticModel AddModel(Node node, UnitType type) {
				var animatedModel = type.Model.AddModel(node);
				type.Material.ApplyMaterial(animatedModel);
				animatedModel.CastShadows = false;
				return animatedModel;
			}

			static void AddAnimationController(Node node) {
				var animationController = node.CreateComponent<AnimationController>();
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

		public Vector3 Forward => LegNode.WorldDirection;

		public Vector3 Backward => -Forward;

		public Vector3 Right => LegNode.WorldRight;

		public Vector3 Left => -Right;

		public Vector3 Up => LegNode.WorldUp;

		public Vector3 Down => -Up;

		public HealthBar HealthBar { get; private set; }

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

		/// <summary>
		/// Gets units movements speed while moving through the tile
		/// </summary>
		/// <param name="tile">the tile on which the returned movementspeed applies</param>
		/// <returns>movement in tiles per second</returns>
		public float MovementSpeed(ITile tile) {
			//TODO: Route this through UnitType
			return tile.MovementSpeedModifier;
		}


		public void SetHeight(float newHeight) {
			Position = new Vector3(Position.X, newHeight, Position.Z);
		}

		public bool MoveBy(Vector3 moveBy) {
			var newPosition = Position + moveBy;

			return MoveTo(newPosition);
		}

		public bool MoveBy(Vector2 moveBy) {
			var newLocation = new Vector2(Position.X + moveBy.X, Position.Z + moveBy.Y);
			return MoveTo(newLocation);
		}

		public bool MoveTo(Vector3 newPosition) {
			bool canMoveToTile = CheckTile(newPosition);
			if (!canMoveToTile) {
				return false;
			}

			FaceTowards(newPosition);
			Position = newPosition;
			return true;
		}

		public bool MoveTo(Vector2 newLocation) {
			return MoveTo(new Vector3(newLocation.X, Map.GetTerrainHeightAt(newLocation), newLocation.Y));
		}

		public void ChangeType(UnitType newType) {
			Node.RemoveAllComponents();
			//TODO: THIS
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
		}

		public void RotateAroundFeet(float pitch, float yaw, float roll) {
			LegNode.Rotate(new Quaternion(pitch, yaw, roll));
		}

		public void RotateAroundCenter(float pitch, float yaw, float roll) {
			Node.Rotate(new Quaternion(pitch, yaw, roll));
		}

		public override void RemoveFromLevel()
		{
			base.RemoveFromLevel();

			Tile.RemoveUnit(this);
			Player.RemoveUnit(this);
			LegNode.Remove();
			LegNode.Dispose();
			Dispose();
			Level.RemoveUnit(this);
		}
		#endregion

		#region Protected Methods

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);
			if (!EnabledEffective) return;

			UnitPlugin.OnUpdate(timeStep);
		}

		#endregion

		#region Private Methods



		bool CheckTile(Vector3 newPosition) {

			//New tile, but cant pass
			if (!UnitPlugin.CanGoFromTo(Position,newPosition) && !IsTileCorner(newPosition)) {
				return false;
			}

			//New tile, but can pass
			Tile.RemoveUnit(this);
			Tile = Map.GetContainingTile(newPosition);
			//TODO: Add as owning unit
			Tile.AddUnit(this);
			return true;
		}

		bool IsTileCorner(Vector3 position) {
			var x = position.X - (float) Math.Floor(position.X);
			var z = position.Z - (float) Math.Floor(position.Z);
			return (x < 0.05f || 0.95f < x) && (z < 0.05f || 0.95f < z);
		}

		void Collision(NodeCollisionStartEventArgs e)
		{
			var projectile = Level.GetProjectile(e.OtherNode);
			if (projectile == null) {
				throw new InvalidOperationException("Hit by something that is not a projectile");
			}

			UnitPlugin.OnProjectileHit(projectile);
		}

		#endregion

	 
	}
}