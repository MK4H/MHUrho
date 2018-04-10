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
    public class Building : Component
    {
        public new int ID { get; private set; }

        public IntRect Rectangle { get; private set; }

        public IntVector2 Location => Rectangle.TopLeft();

        public Vector3 Center => Node.Position;

        public BuildingType BuildingType { get; private set; }

        public IntVector2 Size => new IntVector2(Rectangle.Width(), Rectangle.Height());

        public IPlayer Player { get; private set; }

        public object Plugin => plugin;

        private ITile[] tiles;

        private IBuildingInstancePlugin plugin;

        /// <summary>
        /// Used to store the reference to storedBuilding between Load and ConnectReferences calls
        /// </summary>
        private StBuilding storedBuilding;

        protected Building(int id, IntVector2 topLeftCorner, BuildingType type, IPlayer player, ILevelManager level) {
            this.ID = id;
            this.BuildingType = type;
            this.Player = player;
            this.Rectangle = new IntRect(topLeftCorner.X,
                                         topLeftCorner.Y,
                                         topLeftCorner.X + type.Size.X,
                                         topLeftCorner.Y + type.Size.Y);
            this.tiles = GetTiles(level.Map, type, topLeftCorner);
        }

        protected Building(BuildingType buildingType, Map map, StBuilding storedBuilding) {
            this.ID = storedBuilding.Id;
            this.BuildingType = buildingType;
            var topLeft = storedBuilding.Location.ToIntVector2();
            this.Rectangle = new IntRect(topLeft.X,
                                         topLeft.Y,
                                         topLeft.X + buildingType.Size.X,
                                         topLeft.Y + buildingType.Size.Y);
            this.tiles = GetTiles(map, buildingType, Location);
        }



        /// <summary>
        /// Builds the building at <paramref name="topLeftCorner"/> if its possible
        /// </summary>
        /// <param name="topLeftCorner"></param>
        /// <param name="type"></param>
        /// <param name="buildingNode"></param>
        /// <param name="level"></param>
        /// <returns>Null if it is not possible to build the building there, new Building if it is possible</returns>
        public static Building BuildAt(int id, 
                                       IntVector2 topLeftCorner, 
                                       BuildingType type, 
                                       Node buildingNode, 
                                       IPlayer player,  
                                       ILevelManager level) {
            if (!type.CanBuildIn(type.GetBuildingTilesRectangle(topLeftCorner), level)) {
                return null;
            }

            var newBuilding = new Building(id, topLeftCorner, type, player, level);
            buildingNode.AddComponent(newBuilding);

            var center = newBuilding.Rectangle.Center();

            buildingNode.Position = new Vector3(center.X, level.Map.GetHeightAt(center), center.Y);
            AddRigidBody(buildingNode);

            newBuilding.plugin = newBuilding.BuildingType.GetNewInstancePlugin(newBuilding, level);



            var collider = buildingNode.CreateComponent<CollisionShape>();
            //TODO: Move collisionShape to plugin
            collider.SetBox(new Vector3(1, 1, 1), new Vector3(-0.5f, -0.5f, -0.5f), Quaternion.Identity);


            return newBuilding;
        }

        public static Building Load(ILevelManager level, BuildingType type, Node buildingNode, StBuilding storedBuilding) {
            //TODO: Check arguments - node cant have more than one Building component
            if (type.ID != storedBuilding.TypeID) {
                throw new ArgumentException("Provided type is not the type of the stored building", nameof(type));
            }

            var building = new Building(type, level.Map, storedBuilding);
            buildingNode.AddComponent(building);

            var center = building.Rectangle.Center();

            buildingNode.Position = new Vector3(center.X, level.Map.GetHeightAt(center), center.Y);

            AddRigidBody(buildingNode);

            building.plugin = type.GetInstancePluginForLoading();
            return building;
        }

        public static Building Load(ILevelManager level, 
                                    PackageManager packageManager, 
                                    Node node,
                                    StBuilding storedBuilding) {
            var type = packageManager.ActiveGame.GetBuildingType(storedBuilding.TypeID);
            if (type == null) {
                throw new ArgumentException("Type of this building was not loaded");
            }

            return type.LoadBuilding();
        }


        public void ConnectReferences(ILevelManager level) {
            Player = level.GetPlayer(storedBuilding.PlayerID);
            //TODO: Tiles

            foreach (var defaultComponent in storedBuilding.DefaultComponentData) {
                Node.AddComponent(level.DefaultComponentFactory.LoadComponent(defaultComponent.Key,
                                                                              defaultComponent.Value, 
                                                                              level));
            }

            plugin.LoadState(level, this, new PluginDataWrapper(storedBuilding.UserPlugin));
        }

        public StBuilding Save() {
            var stBuilding = new StBuilding();
            stBuilding.Id = ID;
            stBuilding.TypeID = BuildingType.ID;
            stBuilding.PlayerID = Player.ID;
            stBuilding.Location = Location.ToStIntVector2();
            plugin.SaveState(new PluginDataWrapper(stBuilding.UserPlugin));

            foreach (var component in Node.Components) {
                var defaultComponent = component as DefaultComponent;
                if (defaultComponent != null) {
                    stBuilding.DefaultComponentData.Add((int)defaultComponent.ID, defaultComponent.SaveState());
                }
            }

            return stBuilding;
        }


        public void Destroy() {
            foreach (var tile in tiles) {
                tile.RemoveBuilding(this);
            }

            Node.Remove();
        }

        protected override void OnUpdate(float timeStep) {
            if (!EnabledEffective) return;

            plugin.OnUpdate(timeStep);
        }

        private int GetTileIndex(int x, int y) {
            return x + y * BuildingType.Size.X;
        }

        private int GetTileIndex(IntVector2 location) {
            return GetTileIndex(location.X, location.Y);
        }

        private static void AddRigidBody(Node node) {
            var rigidBody = node.CreateComponent<RigidBody>();
            rigidBody.CollisionLayer = (int)CollisionLayer.Building;
            rigidBody.CollisionMask = (int)CollisionLayer.Projectile;
            rigidBody.Kinematic = true;
            rigidBody.Mass = 1;
            rigidBody.UseGravity = false;
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