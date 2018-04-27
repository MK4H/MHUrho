using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
	public class Building : Entity
	{
		internal class Loader : ILoader {
			public Building Building { get; private set; }
			
			/// <summary>
			/// Used to store the reference to storedBuilding between Load and ConnectReferences calls
			/// </summary>
			StBuilding storedBuilding;

			List<DefaultComponentLoader> componentLoaders;

			protected Loader(StBuilding storedBuilding) {
				this.storedBuilding = storedBuilding;
				componentLoaders = new List<DefaultComponentLoader>();
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
				StaticModel model = AddModel(buildingNode, type);

				var newBuilding = new Building(id, level, topLeftCorner, type, player);
				buildingNode.AddComponent(newBuilding);

				var center = newBuilding.Rectangle.Center();

				buildingNode.Position = new Vector3(center.X,
													level.Map.GetHeightAt(center) + model.BoundingBox.HalfSize.Y * buildingNode.Scale.Y,
													center.Y);

				newBuilding.plugin = newBuilding.BuildingType.GetNewInstancePlugin(newBuilding, level);



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
													Location = building.Location.ToStIntVector2(),
													UserPlugin = new PluginData()
												};
				building.plugin.SaveState(new PluginDataWrapper(stBuilding.UserPlugin));

				foreach (var component in building.Node.Components) {
					var defaultComponent = component as DefaultComponent;
					if (defaultComponent != null) {
						stBuilding.DefaultComponentData.Add((int)defaultComponent.ComponentTypeID, defaultComponent.SaveState());
					}
				}

				return stBuilding;
			}

			public static Loader StartLoading(LevelManager level,
											  PackageManager packageManager,
											  Node node,
											  StBuilding storedBuilding) {
				var type = packageManager.ActiveGame.GetBuildingType(storedBuilding.TypeID);
				if (type == null) {
					throw new ArgumentException("Type of this building was not loaded");
				}

				var loader = new Loader(storedBuilding);
				loader.Load(level, type, node);
				return loader;
			}


			public void ConnectReferences(LevelManager level) {
				Building.Player = level.GetPlayer(storedBuilding.PlayerID);
				//TODO: Tiles

				foreach (var componentLoader in componentLoaders) {
					componentLoader.ConnectReferences(level);
				}

				Building.plugin.LoadState(level, Building, new PluginDataWrapper(storedBuilding.UserPlugin));
			}

			public void FinishLoading() {
				foreach (var componentLoader in componentLoaders) {
					componentLoader.FinishLoading();
				}
			}

			void Load(LevelManager level, BuildingType type, Node buildingNode) {
				//TODO: Check arguments - node cant have more than one Building component
				if (type.ID != storedBuilding.TypeID) {
					throw new ArgumentException("Provided type is not the type of the stored building", nameof(type));
				}

				AddRigidBody(buildingNode);
				StaticModel model = AddModel(buildingNode, type);

				Building = new Building(level, type, storedBuilding);
				buildingNode.AddComponent(Building);

				var center = Building.Rectangle.Center();

				buildingNode.Position = new Vector3(center.X, 
													level.Map.GetHeightAt(center) + model.BoundingBox.HalfSize.Y * buildingNode.Scale.Y, 
													center.Y);

				var collider = buildingNode.CreateComponent<CollisionShape>();
				collider.SetBox(model.BoundingBox.Size,
								Vector3.Zero,
								Quaternion.Identity);

				Building.plugin = type.GetInstancePluginForLoading();

				foreach (var defaultComponent in storedBuilding.DefaultComponentData) {
					var componentLoader = 
						level.DefaultComponentFactory
							.StartLoadingComponent(defaultComponent.Key,
													defaultComponent.Value,
													level,
													Building.plugin);

					componentLoaders.Add(componentLoader);
					Building.AddComponent(componentLoader.Component);
				}
			}

			static void AddRigidBody(Node node) {
				var rigidBody = node.CreateComponent<RigidBody>();
				rigidBody.CollisionLayer = (int)CollisionLayer.Building;
				rigidBody.CollisionMask = (int)CollisionLayer.Projectile;
				rigidBody.Kinematic = true;
				rigidBody.Mass = 1;
				rigidBody.UseGravity = false;
			}

			static StaticModel AddModel(Node node, BuildingType type) {
				var model = type.Model.AddModel(node);
				type.Material.ApplyMaterial(model);
				model.CastShadows = false;
				return model;
			}
		}

		public IntRect Rectangle { get; private set; }

		public IntVector2 Location => Rectangle.TopLeft();

		public override Vector3 Position {
			get => Node.Position;
			protected set => Node.Position = value;
		}

		public Vector3 Center => Node.Position;

		public BuildingType BuildingType { get; private set; }

		public IntVector2 Size => new IntVector2(Rectangle.Width(), Rectangle.Height());

		public object Plugin => plugin;

		private ITile[] tiles;

		private BuildingInstancePluginBase plugin;

		protected Building(int id, ILevelManager level, IntVector2 topLeftCorner, BuildingType type, IPlayer player) 
			:base(id, level)
		{

			this.BuildingType = type;
			this.Player = player;
			this.Rectangle = new IntRect(topLeftCorner.X,
										 topLeftCorner.Y,
										 topLeftCorner.X + type.Size.X,
										 topLeftCorner.Y + type.Size.Y);
			this.tiles = GetTiles(level.Map, type, topLeftCorner);
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
			this.tiles = GetTiles(Map, buildingType, Location);
		}


		public StBuilding Save() {
			return Loader.Save(this);
		}


		public void Kill() {
			foreach (var tile in tiles) {
				tile.RemoveBuilding(this);
			}

			Node.Remove();
			Level.RemoveBuilding(this);
		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
			if (!EnabledEffective) return;

			plugin.OnUpdate(timeStep);
		}

		private int GetTileIndex(int x, int y) {
			return x + y * BuildingType.Size.X;
		}

		private int GetTileIndex(IntVector2 location) {
			return GetTileIndex(location.X, location.Y);
		}


		private ITile[] GetTiles(Map map, BuildingType type, IntVector2 topLeft) {
			var newTiles = new ITile[type.Size.X * type.Size.Y];

			for (int y = 0; y < type.Size.Y; y++) {
				for (int x = 0; x < type.Size.X; x++) {
					var tile = map.GetTileByTopLeftCorner(topLeft.X + x, topLeft.Y + y);
					newTiles[GetTileIndex(x, y)] = tile;
					tile.AddBuilding(this);
				}
			}

			return newTiles;
		}
	}
}