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

				unitNode.Position = new Vector3(tile.Center.X,
												level.Map.GetHeightAt(tile.Center.X, tile.Center.Y),
												tile.Center.Y);
				var unit = new Unit(id, level, type, tile, player, unitNode);
				centerNode.AddComponent(unit);

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
				unit.UnitPlugin.SaveState(new PluginDataWrapper(storedUnit.UserPlugin, unit.Level));

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

				Unit.UnitPlugin.LoadState(level, Unit, new PluginDataWrapper(storedUnit.UserPlugin, level));
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
		}

		public void MoveTo(Vector2 newLocation) {
			MoveTo(new Vector3(newLocation.X, Map.GetTerrainHeightAt(newLocation), newLocation.Y));
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
			if (RemovedFromLevel) return;

			base.RemoveFromLevel();

			Plugin.Dispose();
			Level.RemoveUnit(this);
			Tile.RemoveUnit(this);
			Player.RemoveUnit(this);
			LegNode.Remove();
			LegNode.Dispose();
			
			Dispose();	
		}

		public override void HitBy(IProjectile projectile)
		{
			UnitPlugin.OnProjectileHit(projectile);
		}

		void IDisposable.Dispose()
		{
			RemoveFromLevel();
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



		#endregion

	 
	}
}