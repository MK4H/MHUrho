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

			public IBuilding Building => loadingBuilding;

			Building loadingBuilding;

			readonly LevelManager level;
			readonly Node node;
		
			/// <summary>
			/// Used to store the reference to storedBuilding between Load and ConnectReferences calls
			/// </summary>
			readonly StBuilding storedBuilding;
			readonly BuildingType type;

			List<DefaultComponentLoader> componentLoaders;

			public Loader(LevelManager level,
						Node node,
						StBuilding storedBuilding)
			{
				this.level = level;
				this.node = node;
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
											Node buildingNode,
											IPlayer player,
											ILevelManager level) {
				if (!type.CanBuildIn(type.GetBuildingTilesRectangle(topLeftCorner), level)) {
					return null;
				}

				AddRigidBody(buildingNode);
				StaticModel model = AddModel(buildingNode, type, level);

				var newBuilding = new Building(id, level, topLeftCorner, type, player);
				buildingNode.AddComponent(newBuilding);

				var center = newBuilding.Rectangle.Center();

				buildingNode.Position = new Vector3(center.X,
													level.Map.GetTerrainHeightAt(center) + model.BoundingBox.HalfSize.Y * buildingNode.Scale.Y,
													center.Y);

				newBuilding.BuildingPlugin = newBuilding.BuildingType.GetNewInstancePlugin(newBuilding, level);



				var collider = buildingNode.CreateComponent<CollisionShape>();
				//TODO: Move collisionShape to plugin
				collider.SetBox(model.BoundingBox.Size,
								Vector3.Zero, 
								Quaternion.Identity);


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

				AddRigidBody(node);
				StaticModel model = AddModel(node, type, level);

				loadingBuilding = new Building(level, type, storedBuilding);
				node.AddComponent(loadingBuilding);

				var center = loadingBuilding.Rectangle.Center();

				node.Position = new Vector3(center.X,
													level.Map.GetTerrainHeightAt(center) + model.BoundingBox.HalfSize.Y * node.Scale.Y,
													center.Y);

				var collider = node.CreateComponent<CollisionShape>();
				collider.SetBox(model.BoundingBox.Size,
								Vector3.Zero,
								Quaternion.Identity);

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

			void Load(LevelManager level, BuildingType type, Node buildingNode) {
				
			}

			static void AddRigidBody(Node node) {
				var rigidBody = node.CreateComponent<RigidBody>();
				rigidBody.CollisionLayer = (int)CollisionLayer.Building;
				rigidBody.CollisionMask = (int)CollisionLayer.Projectile;
				rigidBody.Kinematic = true;
				rigidBody.Mass = 1;
				rigidBody.UseGravity = false;
			}

			static StaticModel AddModel(Node node, BuildingType type, ILevelManager level) {
				var model = type.Model.AddModel(node);
				type.Material.ApplyMaterial(model);
				model.CastShadows = false;
				model.DrawDistance = level.App.Config.UnitDrawDistance;
				return model;
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


		public static IBuildingLoader GetLoader(LevelManager level, Node buildingNode, StBuilding storedBuilding)
		{
			return new Loader(level, buildingNode, storedBuilding);
		}

		public static Building CreateNew(int id,
										IntVector2 topLeftCorner,
										BuildingType type,
										Node buildingNode,
										IPlayer player,
										ILevelManager level)
		{
			return Loader.CreateNew(id, topLeftCorner, type, buildingNode, player, level);
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

			Plugin.Dispose();
			Level.RemoveBuilding(this);
			foreach (var tile in tiles) {
				tile.RemoveBuilding(this);
			}

			Player.RemoveBuilding(this);
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