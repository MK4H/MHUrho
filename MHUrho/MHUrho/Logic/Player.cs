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
			readonly PlayerInsignia insignia;
			readonly PlayerType type;
			readonly int teamID;

			public Loader(LevelManager level, IList<StPlayer> storedPlayers, InsigniaGetter insigniaGetter, PlayerInfo newInfo)
			{
				this.level = level;
				this.storedPlayer = (from stPlayer in storedPlayers
									where stPlayer.InsigniaID == newInfo.Insignia.Index
									select stPlayer).FirstOrDefault();

				if (storedPlayer == null) {
					throw new
						ArgumentException("StoredPlayers did not contain player entry for player with provided playerInfo", nameof(storedPlayers));
				}

				this.type = newInfo.PlayerType;
				this.teamID = newInfo.TeamID;
				this.insignia = insigniaGetter.MarkUsed(newInfo.Insignia);

				//Clear the stored player plugin data
				storedPlayer.UserPlugin = new PluginData();
			}

			public Loader(LevelManager level, StPlayer storedPlayer, InsigniaGetter insigniaGetter, bool loadPlaceholder)
			{
				this.level = level;
				this.storedPlayer = storedPlayer;
				this.insignia = insigniaGetter.GetUnusedInsignia(storedPlayer.InsigniaID);
				if (loadPlaceholder) {
					this.type = PlayerType.Placeholder;
					teamID = 0;

				}
				else {
					if (storedPlayer.TypeID == 0)
					{
						throw new ArgumentException("StoredPlayer had no type", nameof(storedPlayer));
					}

					type = PackageManager.Instance.ActivePackage.GetPlayerType(storedPlayer.TypeID);
					teamID = storedPlayer.TeamID;
				}
				

			}

			public static StPlayer Save(Player player)
			{
				var storedPlayer = new StPlayer
									{
										Id = player.ID,
										TeamID = player.TeamID,
										TypeID = player.type?.ID ?? 0,
										InsigniaID = player.Insignia.Index
									};



				storedPlayer.UnitIDs.AddRange(from unitType in player.units
										from unit in unitType.Value
										select unit.ID);

				storedPlayer.BuildingIDs.AddRange(from buildingType in player.buildings
											from building in buildingType.Value
											select building.ID);

				storedPlayer.Resources.AddRange(from resourceType in player.resources
											select new StResource{ Id = resourceType.Key.ID, Amount = resourceType.Value});

				storedPlayer.UserPlugin = new PluginData();
				try {
					player.Plugin?.SaveState(new PluginDataWrapper(storedPlayer.UserPlugin, player.level));
				}
				catch (Exception e)
				{
					string message = $"Saving player plugin failed with Exception: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new SavingException(message, e);
				}

				return storedPlayer;
			}

			public void StartLoading()
			{

				//If the stored type is the same as the new type, the stored plugin data can be loaded
				// If it is a different type, it will probably not know the format of the data, so just create new plugin instance
				bool newPluginInstance = type.ID != storedPlayer.TypeID;
				loadingPlayer = new Player(storedPlayer.Id, teamID, level, insignia, type, newPluginInstance);
			}

			public void ConnectReferences() {
				foreach (var unitID in storedPlayer.UnitIDs) {
					loadingPlayer.AddUnit(level.GetUnit(unitID));
				}

				foreach (var buildingID in storedPlayer.BuildingIDs) {
					loadingPlayer.AddBuilding(level.GetBuilding(buildingID));
				}

				foreach (var resource in storedPlayer.Resources) {
					ResourceType resourceType = level.PackageManager.ActivePackage.GetResourceType(resource.Id);
					loadingPlayer.ChangeResourceAmount(resourceType, resource.Amount);
				}

				//If the stored data is from the same plugin type as the new type, load the data
				// otherwise we created new fresh plugin instance, that most likely does not understand the stored data
				if (type.ID == storedPlayer.TypeID) {
					loadingPlayer.Plugin?.LoadState(new PluginDataWrapper(storedPlayer.UserPlugin, level));
				}
				
			}

			public void FinishLoading() {

			}
		}

		public new int ID { get; }

		public PlayerInsignia Insignia { get; private set; }

		public PlayerAIInstancePlugin Plugin { get; private set; }

		public int TeamID { get; private set; }


		readonly Dictionary<UnitType,List<IUnit>> units;

		readonly Dictionary<BuildingType, List<IBuilding>> buildings;

		readonly Dictionary<ResourceType, double> resources;

		readonly PlayerType type;

		readonly ILevelManager level;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="level"></param>
		/// <param name="insignia"></param>
		/// <param name="type"></param>
		/// <param name="newPluginInstance"></param>
		protected Player(int id, int teamID, ILevelManager level, PlayerInsignia insignia, PlayerType type, bool newPluginInstance)
		{
			ReceiveSceneUpdates = true;
			this.ID = id;
			this.TeamID = teamID;
			units = new Dictionary<UnitType, List<IUnit>>();
			buildings = new Dictionary<BuildingType, List<IBuilding>>();
			resources = new Dictionary<ResourceType, double>();
			this.level = level;
			this.Insignia = insignia;
			this.type = type;
			this.Plugin = newPluginInstance
							? type.GetNewInstancePlugin(this, level)
							: type.GetInstancePluginForLoading(this, level);
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
			return new Player(id, 0, level, insignia, PlayerType.Placeholder, true);
		}

		/// <summary>
		/// Loads player as is stored in the <paramref name="storedPlayer"/>.
		/// </summary>
		/// <param name="level">Level into which the player is being loaded.</param>
		/// <param name="storedPlayer">Stored data of the player.</param>
		/// <param name="insigniaGetter"></param>
		/// <returns>Loader that loads the player as is stored in the <paramref name="storedPlayer"/>.</returns>
		public static IPlayerLoader GetLoader(LevelManager level, StPlayer storedPlayer, InsigniaGetter insigniaGetter)
		{
			return new Loader(level, storedPlayer, insigniaGetter, false);
		}

		/// <summary>
		/// Loads placeholder player with ownership of units and buildings.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="storedPlayer"></param>
		/// <param name="insigniaGetter"></param>
		/// <returns></returns>
		public static IPlayerLoader GetLoaderToPlaceholder(LevelManager level, StPlayer storedPlayer, InsigniaGetter insigniaGetter)
		{
			return new Loader(level, storedPlayer, insigniaGetter, true);
		}

		/// <summary>
		/// Loads player with given <paramref name="playerInfo"/>, overriding any player type info the player may have been stored with.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="storedPlayers"></param>
		/// <param name="playerInfo"></param>
		/// <param name="insigniaGetter"></param>
		/// <returns></returns>
		public static IPlayerLoader GetLoaderFromInfo(LevelManager level,
													IList<StPlayer> storedPlayers,
													PlayerInfo playerInfo,
													InsigniaGetter insigniaGetter)
		{


			return new Loader(level, storedPlayers, insigniaGetter, playerInfo);
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

			try {
				Plugin.UnitAdded(unit);
			}
			catch (Exception e)
			{
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.UnitAdded)} failed with Exception: {e.Message}");
			}
			
		}

		public void AddBuilding(IBuilding building) {
			if (buildings.TryGetValue(building.BuildingType, out var buildingList)) {
				buildingList.Add(building);
			}
			else {
				buildings.Add(building.BuildingType, new List<IBuilding> {building});
			}

			try {
				Plugin.BuildingAdded(building); 
			}
			catch (Exception e)
			{
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.BuildingAdded)} failed with Exception: {e.Message}");
			}
			
		}

		public void ChangeResourceAmount(ResourceType resourceType, double change)
		{
			//if key does not exist, tryGetValue sets the out variable to default(), which here is zero
			resources.TryGetValue(resourceType, out double currentValue);

			try {
				resources[resourceType] = Plugin.ResourceAmountChanged(resourceType, currentValue, currentValue + change);
			}
			catch (Exception e)
			{
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.ResourceAmountChanged)} failed with Exception: {e.Message}");
			}
			
		}

		public bool RemoveUnit(IUnit unit) {
			bool removed = units.TryGetValue(unit.UnitType, out var unitList) && unitList.Remove(unit);
			if (removed) {
				try {
					Plugin.OnUnitKilled(unit);
				}
				catch (Exception e) {
					//NOTE: Maybe add cap to prevent message flood
					Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.OnUnitKilled)} failed with Exception: {e.Message}");
				}
			}

			return removed;
		}

		public bool RemoveBuilding(IBuilding building) {
			bool removed = buildings.TryGetValue(building.BuildingType, out var buildingList) && buildingList.Remove(building);

			if (removed) {
				try {
					Plugin.OnBuildingDestroyed(building);
				}
				catch (Exception e)
				{
					//NOTE: Maybe add cap to prevent message flood
					Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.OnBuildingDestroyed)} failed with Exception: {e.Message}");
				}
			}

			return removed;
		}

		public IEnumerable<IUnit> GetAllUnits() {
			return from unitList in units.Values
				   from unit in unitList
				   select unit;
		}

		public IReadOnlyList<IUnit> GetUnitsOfType(UnitType unitType) {
			return units.TryGetValue(unitType, out List<IUnit> unitList) ? unitList : new List<IUnit>();
		}

		public IEnumerable<IBuilding> GetAllBuildings() {
			return from buildingList in buildings.Values
				   from building in buildingList
				   select building;
		}

		public IReadOnlyList<IBuilding> GetBuildingsOfType(BuildingType buildingType) {
			return buildings.TryGetValue(buildingType, out var buildingList) ? buildingList : new List<IBuilding>();
		}

		public double GetResourceAmount(ResourceType resourceType)
		{
			resources.TryGetValue(resourceType, out double count);
			return count;
		}

		public IEnumerable<IPlayer> GetEnemyPlayers() {
			return from player in level.Players
				   where IsEnemy(player)
				   select player;

		}


		public bool IsFriend(IPlayer player)
		{
			return player.TeamID == TeamID;
		}

		public bool IsEnemy(IPlayer player)
		{
			return !IsFriend(player);
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

			try {
				Plugin.OnUpdate(timeStep);
			}
			catch (Exception e)
			{
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.OnUpdate)} failed with Exception: {e.Message}");
			}
		}
	}
}