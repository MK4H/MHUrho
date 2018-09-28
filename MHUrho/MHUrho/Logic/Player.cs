using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.EntityInfo;
using MHUrho.Helpers;
using MHUrho.Storage;
using Urho;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using MHUrho.Plugins;


namespace MHUrho.Logic
{
	class Player : Component, IPlayer {

		class Loader : IPlayerLoader {

			public Player Player => loadingPlayer;

			Player loadingPlayer;


			readonly LevelManager level;
			readonly StPlayer storedPlayer;
			readonly PlayerType type;

			public Loader(LevelManager level, StPlayer storedPlayer, PlayerType newType, bool overrideType)
			{
				this.level = level;
				this.storedPlayer = storedPlayer;

				if (storedPlayer.TypeID != 0 && !overrideType) {
					throw new ArgumentException("storedPlayer had a type and the override flag was not set",
												nameof(storedPlayer));
				}

				type = newType;
				
				//Clear typespecific data from the safe

				storedPlayer.UserPlugin = new PluginData();
			}

			public Loader(LevelManager level, StPlayer storedPlayer, bool loadType)
			{
				this.level = level;
				this.storedPlayer = storedPlayer;

				if (loadType) {
					if (storedPlayer.TypeID == 0) {
						throw new ArgumentException("StoredPlayer had no type", nameof(storedPlayer));
					}

					type = PackageManager.Instance.ActivePackage.GetPlayerType(storedPlayer.TypeID);
				}
			}

			public static StPlayer Save(Player player)
			{
				//TODO: HUMAN PLAYER TYPE
				var storedPlayer = new StPlayer
									{
										Id = player.ID,
										TypeID = player.type?.ID ?? 0,
										InsigniaID = player.Insignia.ID
									};



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

			public void StartLoading()
			{
				loadingPlayer = new Player(storedPlayer.Id, level, PlayerInsignia.GetInsignia(storedPlayer.InsigniaID));

				if (type != null) {
					if (type.ID == storedPlayer.TypeID) {
						loadingPlayer.Plugin = type.GetInstancePluginForLoading(loadingPlayer, level);
					}
					else {
						loadingPlayer.Plugin = type.GetNewInstancePlugin(loadingPlayer, level);
					}
				}
			}

			public void ConnectReferences() {
				foreach (var unitID in storedPlayer.UnitIDs) {
					loadingPlayer.AddUnit(level.GetUnit(unitID));
				}

				foreach (var buildingID in storedPlayer.BuildingIDs) {
					loadingPlayer.AddBuilding(level.GetBuilding(buildingID));
				}

				foreach (var friendID in storedPlayer.FriendPlayerIDs) {
					loadingPlayer.friends.Add(level.GetPlayer(friendID));
				}

				//If the stored data is from the same plugin type as the new type, load the data
				// otherwise we created new fresh plugin instance, that most likely does not understand the stored data
				if (type != null && type.ID == storedPlayer.TypeID) {
					loadingPlayer.Plugin?.LoadState(new PluginDataWrapper(storedPlayer.UserPlugin, level));
				}
				
			}

			public void FinishLoading() {

			}
		}

		public new int ID { get; }

		public PlayerInsignia Insignia { get; private set; }

		public PlayerAIInstancePlugin Plugin { get; private set; }

		readonly HashSet<IPlayer> friends;


		readonly Dictionary<UnitType,List<IUnit>> units;

		readonly Dictionary<BuildingType, List<IBuilding>> buildings;

		readonly Dictionary<ResourceType, int> resources;

		readonly PlayerType type;

		readonly ILevelManager level;

		protected Player(int id, ILevelManager level, PlayerInsignia insignia) {
			ReceiveSceneUpdates = true;

			this.ID = id;
			units = new Dictionary<UnitType, List<IUnit>>();
			buildings = new Dictionary<BuildingType, List<IBuilding>>();
			resources = new Dictionary<ResourceType, int>();
			friends = new HashSet<IPlayer>();
			this.level = level;
			this.Insignia = insignia;
		}

		protected Player(int id, ILevelManager level, PlayerType type, PlayerInsignia insignia)
			:this(id, level, insignia)
		{
			this.type = type;
			this.Plugin = type.GetNewInstancePlugin(this, level);
		}

		/// <summary>
		/// Creates a player for level editing, where player serves only as a container of his units, buildings and resources
		/// </summary>
		/// <param name="id"></param>
		/// <param name="level"></param>
		/// <param name="insignia"></param>
		/// <returns></returns>
		public static Player CreatePlaceholderPlayer(int id, ILevelManager level, PlayerInsignia insignia)
		{
			return new Player(id, level, insignia);
		}

		/// <summary>
		/// Loads player without the plugin and with cleared playerType
		///
		/// Loaded player serve only as a container of units, buildings and resources
		/// </summary>
		/// <param name="level"></param>
		/// <param name="storedPlayer"></param>
		/// <returns></returns>
		public static IPlayerLoader GetLoaderToEditor(LevelManager level, StPlayer storedPlayer)
		{
			return new Loader(level, storedPlayer, false);
		}

		/// <summary>
		/// Loads player with the type it is stored with
		///
		/// Throws if there is no playerType stored for the player
		/// </summary>
		/// <param name="level"></param>
		/// <param name="storedPlayer"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="storedPlayer"/> does not contain a player type</exception>
		public static IPlayerLoader GetLoaderStoredType(LevelManager level, StPlayer storedPlayer)
		{
			return new Loader(level, storedPlayer, true);
		}

		/// <summary>
		/// Loads player with the <paramref name="fillType"/> as its new type
		///
		/// If <paramref name="overrideType"/> is true, overrides any existing player type stored with this player,
		/// if it is false, throws exception if there is a playerType stored with the player
		/// </summary>
		/// <param name="level"></param>
		/// <param name="storedPlayer"></param>
		/// <param name="fillType"></param>
		/// <param name="overrideType"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="storedPlayer"/> contains a playerType and the flag <paramref name="overrideType"/> was not set</exception>
		public static IPlayerLoader GetLoaderFillType(LevelManager level,
													StPlayer storedPlayer,
													PlayerType fillType,
													bool overrideType)
		{
			return new Loader(level, storedPlayer, fillType, overrideType);
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
				//TODO: Make sure every player has a plugin
				Plugin?.OnBuildingDestroyed(building);
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
			if (!EnabledEffective || !level.LevelNode.Enabled) return;

			Plugin?.OnUpdate(timeStep);
		}
	}
}