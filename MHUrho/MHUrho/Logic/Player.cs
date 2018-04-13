using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Storage;
using Urho;
using MHUrho.Logic;
using MHUrho.WorldMap;


namespace MHUrho.Logic
{
    public class Player : Component, IPlayer {

        public int ID { get; private set; }

        private readonly List<IPlayer> friends;

        //TODO: Split units and buildings by types
        private readonly Dictionary<UnitType,List<Unit>> units;

        private readonly Dictionary<BuildingType, List<Building>> buildings;

        private readonly Dictionary<ResourceType, int> resources;

        private StPlayer storedPlayer;

        public Player(int ID) {
            this.ID = ID;
            units = new Dictionary<UnitType, List<Unit>>();
            buildings = new Dictionary<BuildingType, List<Building>>();
            resources = new Dictionary<ResourceType, int>();
            friends = new List<IPlayer>();
        }

        protected Player(StPlayer storedPlayer) 
            : this(storedPlayer.PlayerID) {
            this.storedPlayer = storedPlayer;
        }

        public static Player Load(StPlayer storedPlayer) {
            var newPlayer = new Player(storedPlayer);
            return newPlayer;
        }

        public StPlayer Save() {
            var storedPlayer = new StPlayer();

            storedPlayer.PlayerID = ID;

            storedPlayer.UnitIDs.Add(from unitType in units
                                     from unit in unitType.Value
                                     select unit.ID);

            storedPlayer.BuildingIDs.Add(from buildingType in buildings
                                         from building in buildingType.Value
                                         select building.ID);

            storedPlayer.FriendPlayerIDs.Add(from friend in friends
                                             select friend.ID);
            
            return storedPlayer;
        }

        public void ConnectReferences(ILevelManager level) {
            foreach (var unitID in storedPlayer.UnitIDs) {
                AddUnit(level.GetUnit(unitID));
            }

            foreach (var buildingID in storedPlayer.BuildingIDs) {
                AddBuilding(level.GetBuilding(buildingID));
            }

            foreach (var friendID in storedPlayer.FriendPlayerIDs) {
                friends.Add(level.GetPlayer(friendID));
            }
        }

        public void FinishLoading() {
            storedPlayer = null;
        }

        /// <summary>
        /// Adds unit to players units
        /// </summary>
        /// <param name="unit">unit to add</param>
        public void AddUnit(Unit unit) {
            if (units.TryGetValue(unit.UnitType, out var unitList)) {
                unitList.Add(unit);
            }
            else {
                units.Add(unit.UnitType, new List<Unit> {unit});
            }
        }

        public void AddBuilding(Building building) {
            if (buildings.TryGetValue(building.BuildingType, out var buildingList)) {
                buildingList.Add(building);
            }
            else {
                buildings.Add(building.BuildingType, new List<Building> {building});
            }
        }

        public bool RemoveUnit(Unit unit) {
            return units.TryGetValue(unit.UnitType, out var unitList) && unitList.Remove(unit);
        }

        public bool RemoveBuilding(Building building) {
            return buildings.TryGetValue(building.BuildingType, out var buildingList) && buildingList.Remove(building);
        }

        public IReadOnlyList<Unit> GetUnitsOfType(UnitType type) {
            return units.TryGetValue(type, out List<Unit> unitList) ? new List<Unit>() : unitList;
        }

        public IReadOnlyList<Building> GetBuildingsOfType(BuildingType type) {
            return buildings.TryGetValue(type, out var buildingList) ? new List<Building>() : buildingList;
        }

        public int GetResourcesOfType(ResourceType type) {
            return resources.TryGetValue(type, out int count) ? 0 : count;
        }

        protected override void OnUpdate(float timeStep) {
            
        }
    }
}