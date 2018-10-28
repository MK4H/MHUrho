using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Control;
using Urho;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho.Physics;

namespace MHUrho.Logic
{
	class Building : Entity, IBuilding {
		class Loader : IBuildingLoader {

			static class ComponentSetup
			{
				delegate void ComponentSetupDelegate(Component component, ILevelManager level);

				static readonly Dictionary<StringHash, ComponentSetupDelegate> SetupDispatch;

				static ComponentSetup()
				{
					SetupDispatch = new Dictionary<StringHash, ComponentSetupDelegate>
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
						if (SetupDispatch.TryGetValue(component.Type, out ComponentSetupDelegate value))
						{
							value(component, level);
						}
					}
				}

				static void SetupRigidBody(Component rigidBodyComponent, ILevelManager level)
				{
					RigidBody rigidBody = rigidBodyComponent as RigidBody;

					rigidBody.CollisionLayer = (int)CollisionLayer.Building;
					rigidBody.CollisionMask = (int)CollisionLayer.Projectile;
					rigidBody.Kinematic = true;
					rigidBody.Mass = 1;
					rigidBody.UseGravity = false;
				}

				static void SetupStaticModel(Component staticModelComponent, ILevelManager level)
				{
					StaticModel staticModel = staticModelComponent as StaticModel;

					staticModel.CastShadows = false;
					staticModel.DrawDistance = level.App.Config.UnitDrawDistance;
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

			public IBuilding Building => loadingBuilding;

			Building loadingBuilding;

			readonly LevelManager level;
		
			/// <summary>
			/// Used to store the reference to storedBuilding between Load and ConnectReferences calls
			/// </summary>
			readonly StBuilding storedBuilding;
			readonly BuildingType type;

			List<DefaultComponentLoader> componentLoaders;

			public Loader(LevelManager level,
						StBuilding storedBuilding)
			{
				this.level = level;
				this.storedBuilding = storedBuilding;
				componentLoaders = new List<DefaultComponentLoader>();

				type = PackageManager.Instance.ActivePackage.GetBuildingType(storedBuilding.TypeID);
				if (type == null) {
					throw new ArgumentException("Type of this building was not loaded");
				}
			}

			/// <summary>
			/// Builds the building at <paramref name="topLeftCorner"/> if its possible
			/// </summary>
			/// <param name="topLeftCorner"></param>
			/// <param name="type"></param>
			/// <param name="buildingNode"></param>
			/// <param name="level"></param>
			/// <returns>Null if it is not possible to build the building there, new Building if it is possible</returns>
			public static Building CreateNew(int id,
											IntVector2 topLeftCorner,
											BuildingType type,
											IPlayer player,
											ILevelManager level) {
				if (!type.CanBuildIn(type.GetBuildingTilesRectangle(topLeftCorner), level)) {
					return null;
				}

				//TODO: Redo building building
				var newBuilding = new Building(id, level, topLeftCorner, type, player);

				Vector2 center = newBuilding.Rectangle.Center();

				Vector3 position = new Vector3(center.X,
												level.Map.GetTerrainHeightAt(center),
												center.Y);

				Node buildingNode = type.Assets.Instantiate(level, position, Quaternion.Identity);
				buildingNode.AddComponent(newBuilding);

				ComponentSetup.SetupComponentsOnNode(buildingNode, level);



				newBuilding.BuildingPlugin = newBuilding.BuildingType.GetNewInstancePlugin(newBuilding, level);
				return newBuilding;
			}

			public static StBuilding Save(Building building) {
				var stBuilding = new StBuilding {
													Id = building.ID,
													TypeID = building.BuildingType.ID,
													PlayerID = building.Player.ID,
													Location = building.TopLeft.ToStIntVector2(),
													UserPlugin = new PluginData()
												};
				building.BuildingPlugin.SaveState(new PluginDataWrapper(stBuilding.UserPlugin, building.Level));

				foreach (var component in building.Node.Components) {
					var defaultComponent = component as DefaultComponent;
					if (defaultComponent != null) {
						stBuilding.DefaultComponents.Add(defaultComponent.SaveState());
					}
				}

				return stBuilding;
			}

			public void StartLoading() {
				//TODO: Check arguments - node cant have more than one Building component
				if (type.ID != storedBuilding.TypeID) {
					throw new ArgumentException("Provided type is not the type of the stored building", nameof(type));
				}

				loadingBuilding = new Building(level, type, storedBuilding);

				var center = loadingBuilding.Rectangle.Center();
				Vector3 position = new Vector3(center.X,
												level.Map.GetTerrainHeightAt(center),
												center.Y);

				//TODO: Save rotation
				Node buildingNode = type.Assets.Instantiate(level, position, Quaternion.Identity);
				buildingNode.AddComponent(loadingBuilding);

				loadingBuilding.BuildingPlugin = type.GetInstancePluginForLoading(loadingBuilding, level);

				foreach (var defaultComponent in storedBuilding.DefaultComponents) {
					var componentLoader =
						level.DefaultComponentFactory
							.StartLoadingComponent(defaultComponent,
													level,
													loadingBuilding.BuildingPlugin);

					componentLoaders.Add(componentLoader);
					loadingBuilding.AddComponent(componentLoader.Component);
				}
			}


			public void ConnectReferences() {
				loadingBuilding.Player = level.GetPlayer(storedBuilding.PlayerID);

				foreach (var componentLoader in componentLoaders) {
					componentLoader.ConnectReferences();
				}

				loadingBuilding.BuildingPlugin.LoadState(new PluginDataWrapper(storedBuilding.UserPlugin, level));
			}

			public void FinishLoading() {
				foreach (var componentLoader in componentLoaders) {
					componentLoader.FinishLoading();
				}
			}

		}

		public IntRect Rectangle { get; private set; }

		public IntVector2 TopLeft => Rectangle.TopLeft();

		public IntVector2 TopRight => Rectangle.TopRight();

		public IntVector2 BottomLeft => Rectangle.BottomLeft();

		public IntVector2 BottomRight => Rectangle.BottomRight();


		public override Vector3 Position {
			get => Node.Position;
			protected set => Node.Position = value;
		}

		public override InstancePlugin Plugin => BuildingPlugin;

		public Vector3 Center => Node.Position;

		public BuildingType BuildingType { get; private set; }

		public IntVector2 Size => new IntVector2(Rectangle.Width(), Rectangle.Height());

		public override Vector3 Forward => Node.WorldDirection;

		public override Vector3 Backward => -Forward;

		public override Vector3 Right => Node.WorldRight;

		public override Vector3 Left => -Right;

		public override Vector3 Up => Node.WorldUp;

		public override Vector3 Down => -Up;

		public BuildingInstancePlugin BuildingPlugin { get; private set; }

		ITile[] tiles;

		protected Building(int id, ILevelManager level, IntVector2 topLeftCorner, BuildingType type, IPlayer player) 
			:base(id, level)
		{

			this.BuildingType = type;
			this.Player = player;
			this.Rectangle = new IntRect(topLeftCorner.X,
										 topLeftCorner.Y,
										 topLeftCorner.X + type.Size.X,
										 topLeftCorner.Y + type.Size.Y);
			this.tiles = GetTiles();
		}

		protected Building(ILevelManager level, BuildingType buildingType, StBuilding storedBuilding) 
			:base(storedBuilding.Id, level)
		{
			this.BuildingType = buildingType;
			var topLeft = storedBuilding.Location.ToIntVector2();
			this.Rectangle = new IntRect(topLeft.X,
										 topLeft.Y,
										 topLeft.X + buildingType.Size.X,
										 topLeft.Y + buildingType.Size.Y);
			this.tiles = GetTiles();
		}


		public static IBuildingLoader GetLoader(LevelManager level, StBuilding storedBuilding)
		{
			return new Loader(level, storedBuilding);
		}

		public static Building CreateNew(int id,
										IntVector2 topLeftCorner,
										BuildingType type,
										IPlayer player,
										ILevelManager level)
		{
			return Loader.CreateNew(id, topLeftCorner, type, player, level);
		}

		public StBuilding Save() {
			return Loader.Save(this);
		}

		public override void Accept(IEntityVisitor visitor) {
			visitor.Visit(this);
		}

		public override T Accept<T>(IEntityVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}

		public override void RemoveFromLevel() {

			if (RemovedFromLevel) return;

			base.RemoveFromLevel();

			//We need removeFromLevel to work even when called during loading
			Plugin?.Dispose();
			Level.RemoveBuilding(this);
			foreach (var tile in tiles) {
				tile.RemoveBuilding(this);
			}

			Player?.RemoveBuilding(this);
			Node.Remove();
			
			Dispose();
			
		}

		public override void HitBy(IEntity other, object userData)
		{
			BuildingPlugin.OnHit(other, userData);
		}

		public float? GetHeightAt(float x, float y)
		{
			return BuildingPlugin.GetHeightAt(x, y);
		}

		public IFormationController GetFormationController(Vector3 centerPosition)
		{
			return BuildingPlugin.GetFormationController(centerPosition);
		}

		

		void IDisposable.Dispose()
		{
			RemoveFromLevel();
		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
			if (!EnabledEffective || !Level.LevelNode.Enabled) {
				return;
			}

			BuildingPlugin.OnUpdate(timeStep);
		}

		int GetTileIndex(int x, int y) {
			return x + y * BuildingType.Size.X;
		}

		int GetTileIndex(IntVector2 location) {
			return GetTileIndex(location.X, location.Y);
		}


		ITile[] GetTiles() {
			var newTiles = new ITile[BuildingType.Size.X * BuildingType.Size.Y];

			for (int y = 0; y < BuildingType.Size.Y; y++) {
				for (int x = 0; x < BuildingType.Size.X; x++) {
					var tile = Map.GetTileByTopLeftCorner(TopLeft.X + x, TopLeft.Y + y);
					newTiles[GetTileIndex(x, y)] = tile;
					tile.AddBuilding(this);
				}
			}

			return newTiles;
		}
	}
}