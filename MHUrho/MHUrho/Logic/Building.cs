using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Urho;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
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

        private IBuildingInstancePlugin logic;

        private ITile[] tiles;

        /// <summary>
        /// Builds the building at <paramref name="topLeftCorner"/> if its possible
        /// </summary>
        /// <param name="topLeftCorner"></param>
        /// <param name="type"></param>
        /// <param name="buildingNode"></param>
        /// <param name="level"></param>
        /// <returns>Null if it is not possible to build the building there, new Building if it is possible</returns>
        public static Building BuildAt(IntVector2 topLeftCorner, BuildingType type, Node buildingNode,  LevelManager level) {
            if (!type.CanBuildAt(topLeftCorner)) {
                return null;
            }

            var newBuilding = new Building(topLeftCorner, type, level);
            buildingNode.AddComponent(newBuilding);

            newBuilding.logic = newBuilding.BuildingType.GetNewInstancePlugin(newBuilding, level);

            var rigidBody = buildingNode.CreateComponent<RigidBody>();
            rigidBody.CollisionLayer = (int) CollisionLayer.Building;
            rigidBody.CollisionLayer = (int) (CollisionLayer.Arrow | CollisionLayer.Boulder);
            rigidBody.Kinematic = true;
            rigidBody.Mass = 1;
            rigidBody.UseGravity = false;

            var collider = buildingNode.CreateComponent<CollisionShape>();
            //TODO: Collider
            collider.SetBox(new Vector3(1, 1, 1), new Vector3(-0.5f, -0.5f, -0.5f), Quaternion.Identity);


            return newBuilding;
        }

        public static Building Load(LevelManager level, BuildingType type, Node node, StBuilding storedBuilding) {
            //TODO: Check arguments - node cant have more than one Building component
            if (type.ID != storedBuilding.TypeID) {
                throw new ArgumentException("Provided type is not the type of the stored building", nameof(type));
            }

            var building = new Building(storedBuilding.Location.ToIntVector2(), type, level);
            node.AddComponent(building);
            Vector2 positionXZ =
                new Vector2(building.Location.X + building.Size.X, building.Location.Y + building.Size.Y);
            
            //TODO: LEVEL THE GROUND
            float height = level.Map.GetHeightAt(positionXZ);
            node.Position = new Vector3(positionXZ.X, height, positionXZ.Y);

            building.logic = type.LoadInstancePlugin(building, level, storedBuilding.UserPlugin);
            return building;
        }

        public static Building Load(LevelManager level, 
                                    PackageManager packageManager, 
                                    Node node,
                                    StBuilding storedBuilding) {
            var type = packageManager.GetBuildingType(storedBuilding.TypeID);
            if (type == null) {
                throw new ArgumentException("Type of this building was not loaded");
            }

            return type.LoadBuilding();


        }

        protected Building(IntVector2 topLeftCorner, BuildingType type, LevelManager level) {
            this.BuildingType = type;
            this.Rectangle = new IntRect(topLeftCorner.X,
                                         topLeftCorner.Y,
                                         topLeftCorner.X + type.Size.X - 1,
                                         topLeftCorner.Y + type.Size.Y - 1);
            this.tiles = new ITile[type.Size.X * type.Size.Y];

            for (int y = 0; y < type.Size.Y; y++) {
                for (int x = 0; x < type.Size.X; x++) {
                    tiles[GetTileIndex(x,y)] = level.Map.GetTile(topLeftCorner.X + x, topLeftCorner.Y + y);
                }
            }
        }

        /// <summary>
        /// Provides a target tile that the worker unit should go to
        /// </summary>
        /// <param name="unit">Worker unit of the building</param>
        /// <returns>Target tile</returns>
        public ITile GetExchangeTile(Unit unit) {
            return logic.GetExchangeTile(unit);
        }

        private int GetTileIndex(int x, int y) {
            return x + y * BuildingType.Size.X;
        }

        private int GetTileIndex(IntVector2 location) {
            return GetTileIndex(location.X, location.Y);
        }
    }
}