using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Urho;
using MHUrho.Helpers;
using MHUrho.Plugins;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{
    public class Building : Component
    {
        ITile[] Tiles;

        Unit[] Workers;

        public IntRect Rectangle { get; private set; }

        public IntVector2 Location => Rectangle.TopLeft();


        public BuildingType Type { get; private set; }

        private IBuildingInstancePlugin logic;

        /// <summary>
        /// Builds the building at <paramref name="topLeftCorner"/> if its possible
        /// </summary>
        /// <param name="topLeftCorner"></param>
        /// <param name="type"></param>
        /// <param name="map"></param>
        /// <returns>Null if it is not possible to build the building there, new Building if it is possible</returns>
        public static Building BuildAt(IntVector2 topLeftCorner, BuildingType type, Node buildingNode,  LevelManager level) {
            if (!type.CanBuildAt(topLeftCorner)) {
                return null;
            }

            var newBuilding = new Building(topLeftCorner, type, buildingNode, level);


            return newBuilding;
        }

        protected Building(IntVector2 topLeftCorner, BuildingType type, Node buildingNode, LevelManager level) {
            this.Type = type;
            this.Rectangle = new IntRect(topLeftCorner.X,
                                         topLeftCorner.Y,
                                         topLeftCorner.X + type.Size.X - 1,
                                         topLeftCorner.Y + type.Size.Y - 1);
            this.logic = type.BuildingLogic.CreateNewInstance(level, buildingNode, this);
            this.Tiles = new ITile[type.Size.X * type.Size.Y];

            for (int y = 0; y < type.Size.Y; y++) {
                for (int x = 0; x < type.Size.X; x++) {
                    Tiles[GetTileIndex(x,y)] = level.Map.GetTile(topLeftCorner.X + x, topLeftCorner.Y + y);
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
            return x + y * Type.Size.X;
        }

        private int GetTileIndex(IntVector2 location) {
            return GetTileIndex(location.X, location.Y);
        }
    }
}