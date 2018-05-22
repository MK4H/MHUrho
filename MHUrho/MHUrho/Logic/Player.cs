using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Storage;
using Urho;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Plugins;


namespace MHUrho.Logic
{
	public class Player : Component, IPlayer {

		internal class Loader : ILoader {

			public Player Player { get; private set; }

			StPlayer storedPlayer;

			protected Loader(StPlayer storedPlayer) {
				this.storedPlayer = storedPlayer;
			}

			public static StPlayer Save(Player player)
			{
				//TODO: SAVE TYPE
				var storedPlayer = new StPlayer { PlayerID = player.ID };

				storedPlayer.Color = player.Color.ToStColor();

				storedPlayer.UnitIDs.Add(from unitType in player.units
										from unit in unitType.Value
										select unit.ID);

				storedPlayer.BuildingIDs.Add(from buildingType in player.buildings
											from building in buildingType.Value
											select building.ID);

				storedPlayer.FriendPlayerIDs.Add(from friend in player.friends
												select friend.ID);

				storedPlayer.UserPlugin = new PluginData();
				player.Plugin?.SaveState(new PluginDataWrapper(storedPlayer.UserPlugin, player.level));

				return storedPlayer;
			}

			public static Loader StartLoading(LevelManager level, StPlayer storedPlayer) {

				var loader = new Loader(storedPlayer);
				loader.Load(level);

				return loader;
			}

			public void ConnectReferences(LevelManager level) {
				foreach (var unitID in storedPlayer.UnitIDs) {
					Player.AddUnit(level.GetUnit(unitID));
				}

				foreach (var buildingID in storedPlayer.BuildingIDs) {
					Player.AddBuilding(level.GetBuilding(buildingID));
				}

				foreach (var friendID in storedPlayer.FriendPlayerIDs) {
					Player.friends.Add(level.GetPlayer(friendID));
				}
			}

			public void FinishLoading() {
				storedPlayer = null;
			}

			void Load(LevelManager level) {
				//TODO: Load with type
				Player = new Player(storedPlayer.PlayerID, level, storedPlayer.Color.ToColor());
			}
		}

		public new int ID { get; }

		public Color Color { get; private set; }

		public PlayerAIInstancePlugin Plugin { get; private set; }

		readonly HashSet<IPlayer> friends;


		readonly Dictionary<UnitType,List<IUnit>> units;

		readonly Dictionary<BuildingType, List<IBuilding>> buildings;

		readonly Dictionary<ResourceType, int> resources;

		readonly PlayerType type;

		ILevelManager level;

		protected Player(int id, ILevelManager level, Color color) {
			ReceiveSceneUpdates = true;

			this.ID = id;
			units = new Dictionary<UnitType, List<IUnit>>();
			buildings = new Dictionary<BuildingType, List<IBuilding>>();
			resources = new Dictionary<ResourceType, int>();
			friends = new HashSet<IPlayer>();
			this.level = level;
			this.Color = color;
		}

		protected Player(int id, ILevelManager level, PlayerType type, Color color)
			:this(id, level, color)
		{
			this.type = type;
			this.Plugin = type.GetNewInstancePlugin(this, level);
		}

		public static Player CreateNewAIPlayer(int id, ILevelManager level, Node node, PlayerType type, Color color)
		{
			var player = new Player(id, level, type, color);
			node.AddComponent(player);
			return player;
		}

		public static Player CreateNewHumanPlayer(int id, ILevelManager level, Node node, Color color)
		{
			var player = new Player(id, level, color);
			node.AddComponent(player);
			return player;
		}

		public StPlayer Save()
		{
			return Loader.Save(this);
		}

		/// <summary>
		/// Adds unit to players units
		/// </summary>
		/// <param name="unit">unit to add</param>
		public void AddUnit(IUnit unit) {
			if (units.TryGetValue(unit.UnitType, out var unitList)) {
				unitList.Add(unit);
			}
			else {
				units.Add(unit.UnitType, new List<IUnit> {unit});
			}
		}

		public void AddBuilding(IBuilding building) {
			if (buildings.TryGetValue(building.BuildingType, out var buildingList)) {
				buildingList.Add(building);
			}
			else {
				buildings.Add(building.BuildingType, new List<IBuilding> {building});
			}
		}

		public bool RemoveUnit(IUnit unit) {
			bool removed = units.TryGetValue(unit.UnitType, out var unitList) && unitList.Remove(unit);
			if (removed) {
				Plugin?.OnUnitKilled(unit);
			}

			return removed;
		}

		public bool RemoveBuilding(IBuilding building) {
			bool removed = buildings.TryGetValue(building.BuildingType, out var buildingList) && buildingList.Remove(building);

			if (removed) {
				Plugin.OnBuildingDestroyed(building);
			}

			return removed;
		}

		public IEnumerable<IUnit> GetAllUnits() {
			return from unitList in units.Values
				   from unit in unitList
				   select unit;
		}

		public IReadOnlyList<IUnit> GetUnitsOfType(UnitType type) {
			return units.TryGetValue(type, out List<IUnit> unitList) ? new List<IUnit>() : unitList;
		}

		public IEnumerable<IBuilding> GetAllBuildings() {
			return from buildingList in buildings.Values
				   from building in buildingList
				   select building;
		}

		public IReadOnlyList<IBuilding> GetBuildingsOfType(BuildingType type) {
			return buildings.TryGetValue(type, out var buildingList) ? new List<IBuilding>() : buildingList;
		}

		public int GetResourcesOfType(ResourceType type) {
			return resources.TryGetValue(type, out int count) ? 0 : count;
		}

		public IEnumerable<IPlayer> GetEnemyPlayers() {
			return from player in level.Players
				   where IsEnemy(player)
				   select player;

		}


		public bool IsFriend(IPlayer player)
		{
			return friends.Contains(player);
		}

		public bool IsEnemy(IPlayer player)
		{
			return player != this && !IsFriend(player);
		}



		public override int GetHashCode() {
			return ID;
		}

		public override bool Equals(object obj)
		{
			return ReferenceEquals(this, obj);
		}


		protected override void OnUpdate(float timeStep)
		{
			if (!EnabledEffective) return;

			Plugin?.OnUpdate(timeStep);
		}
	}
}