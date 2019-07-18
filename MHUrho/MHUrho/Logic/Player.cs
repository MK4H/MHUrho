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

					type = level.Package.GetPlayerType(storedPlayer.TypeID);
					teamID = storedPlayer.TeamID;
				}
				

			}

			public static StPlayer Save(Player player)
			{
				var storedPlayer = new StPlayer
									{
										Id = player.ID,
										TeamID = player.TeamID,
										TypeID = player.PlayerType?.ID ?? 0,
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
					player.Plugin?.SaveState(new PluginDataWrapper(storedPlayer.UserPlugin, player.Level));
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
					loadingPlayer.AddUnitImpl(level.GetUnit(unitID));
				}

				foreach (var buildingID in storedPlayer.BuildingIDs) {
					loadingPlayer.AddBuildingImpl(level.GetBuilding(buildingID));
				}

				foreach (var resource in storedPlayer.Resources) {
					ResourceType resourceType = level.PackageManager.ActivePackage.GetResourceType(resource.Id);
					loadingPlayer.resources.Add(resourceType, resource.Amount);
				}

				//If the stored data is from the same plugin type as the new type, load the data
				// otherwise we created new fresh plugin instance, that most likely does not understand the stored data
				if (type.ID == storedPlayer.TypeID) {
					loadingPlayer.Plugin?.LoadState(new PluginDataWrapper(storedPlayer.UserPlugin, level));
				}
				else {
					loadingPlayer.Plugin?.Init(level);
				}
			}

			public void FinishLoading() {

			}
		}

		public new int ID { get; }

		public PlayerInsignia Insignia { get; private set; }

		public PlayerAIInstancePlugin Plugin { get; private set; }

		public int TeamID { get; private set; }

		public ILevelManager Level { get; private set; }

		public PlayerType PlayerType { get; private set; }

		public bool IsRemovedFromLevel { get; private set; }

		public IReadOnlyDictionary<ResourceType, double> Resources => resources;

		public event Action<IPlayer> OnRemoval;

		readonly Dictionary<UnitType,List<IUnit>> units;

		readonly Dictionary<BuildingType, List<IBuilding>> buildings;

		readonly Dictionary<ResourceType, double> resources;

		/// <summary>
		/// Creates new instance of a player.
		/// </summary>
		/// <param name="id">Unique identifier of the player.</param>
		/// <param name="teamID">Identifier of the team this player is on.</param>
		/// <param name="level">The level this player is spawning into.</param>
		/// <param name="insignia">The graphical identifications of the player.</param>
		/// <param name="type">The type of the player.</param>
		/// <param name="newPluginInstance">If new plugin instance should be created.</param>
		protected Player(int id, int teamID, ILevelManager level, PlayerInsignia insignia, PlayerType type, bool newPluginInstance)
		{
			ReceiveSceneUpdates = true;
			this.ID = id;
			this.TeamID = teamID;
			units = new Dictionary<UnitType, List<IUnit>>();
			buildings = new Dictionary<BuildingType, List<IBuilding>>();
			resources = new Dictionary<ResourceType, double>();
			this.Level = level;
			this.Insignia = insignia;
			this.PlayerType = type;
			this.Plugin = newPluginInstance
							? type.GetNewInstancePlugin(this, level)
							: type.GetInstancePluginForLoading(this, level);
		}

		/// <summary>
		/// Creates a player for level editing, where player serves only as a container of his units, buildings and resources
		/// </summary>
		/// <param name="id">Unique identifier of the player.</param>
		/// <param name="level">The level this player is spawning into.</param>
		/// <param name="insignia">The graphical identifications of the player.</param>
		/// <returns>New instance of placeholder player.</returns>
		public static Player CreatePlaceholderPlayer(int id, ILevelManager level, PlayerInsignia insignia)
		{
			return new Player(id, 0, level, insignia, PlayerType.Placeholder, true);
		}

		/// <summary>
		/// Loads player as is stored in the <paramref name="storedPlayer"/>.
		/// </summary>
		/// <param name="level">Level into which the player is being loaded.</param>
		/// <param name="storedPlayer">Stored data of the player.</param>
		/// <param name="insigniaGetter">Provider of graphical identifications for players.</param>
		/// <returns>Loader that loads the player as is stored in the <paramref name="storedPlayer"/>.</returns>
		public static IPlayerLoader GetLoader(LevelManager level, StPlayer storedPlayer, InsigniaGetter insigniaGetter)
		{
			return new Loader(level, storedPlayer, insigniaGetter, false);
		}

		/// <summary>
		/// Loads placeholder player with ownership of units and buildings.
		/// </summary>
		/// <param name="level">The level the placeholder player is loading into.</param>
		/// <param name="storedPlayer">The stored data for this placeholder player.</param>
		/// <param name="insigniaGetter">Provider of graphical identifications for players.</param>
		/// <returns>Loader that loads the placeholder player from the data in the <paramref name="storedPlayer"/>.</returns>
		public static IPlayerLoader GetLoaderToPlaceholder(LevelManager level, StPlayer storedPlayer, InsigniaGetter insigniaGetter)
		{
			return new Loader(level, storedPlayer, insigniaGetter, true);
		}

		/// <summary>
		/// Loads a player from <paramref name="storedPlayers"/> which matches the identifications given in <paramref name="playerInfo"/>
		/// , overriding any player type info the player may have been stored with.
		/// </summary>
		/// <param name="level">The level the player is loading into.</param>
		/// <param name="storedPlayers">Data of the all stored players.</param>
		/// <param name="playerInfo">The new info to match or override the stored info with.</param>
		/// <param name="insigniaGetter">Provider of graphical identifications for players.</param>
		/// <returns>Loader that loads a player from <paramref name="storedPlayers"/> which matches the identifications given in <paramref name="playerInfo"/></returns>
		public static IPlayerLoader GetLoaderFromInfo(LevelManager level,
													IList<StPlayer> storedPlayers,
													PlayerInfo playerInfo,
													InsigniaGetter insigniaGetter)
		{


			return new Loader(level, storedPlayers, insigniaGetter, playerInfo);
		}

		/// <summary>
		/// Serializes the current state of the player to an instance of <see cref="StPlayer"/>.
		/// </summary>
		/// <returns>Serialized current state of the player.</returns>
		public StPlayer Save()
		{
			return Loader.Save(this);
		}

		/// <summary>
		/// Removes player from level, with all the buildings and units that belong to him.
		/// Player can be removed either by calling this or by calling the <see cref="ILevelManager.RemovePlayer(IPlayer)"/>.
		/// </summary>
		public void RemoveFromLevel()
		{
			if (IsRemovedFromLevel) return;
			IsRemovedFromLevel = true;

			try
			{
				OnRemoval?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(OnRemoval)}: {e.Message}");
			}
			OnRemoval = null;

			//We need removeFromLevel to work during any phase of loading, where connect references may not have been called yet
			try
			{
				Plugin?.Dispose();
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.Dispose)} failed with Exception: {e.Message}");
			}

			//Enumerate over copies of the collections, because with each removal the unit will get removed from the live collection.
			foreach (var unitsOfType in units.Values.ToArray()) {
				foreach (var unit in unitsOfType.ToArray()) {
					unit.RemoveFromLevel();
				}
			}

			//Enumerate over copies of the collections, because with each removal the unit will get removed from the live collection.
			foreach (var buildingsOfType in buildings.Values.ToArray()) {
				foreach (var building in buildingsOfType.ToArray()) {
					building.RemoveFromLevel();
				}
			}

			Level.RemovePlayer(this);
			if (!IsDeleted)
			{
				Remove();
				base.Dispose();
			}
		}

		public new void Dispose()
		{
			RemoveFromLevel();
		}

		/// <summary>
		/// Adds unit to players units.
		/// </summary>
		/// <param name="unit">Unit to add.</param>
		public void AddUnit(IUnit unit)
		{
			AddUnitImpl(unit);

			try {
				Plugin.UnitAdded(unit);
			}
			catch (Exception e)
			{
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.UnitAdded)} failed with Exception: {e.Message}");
			}
			
		}

		/// <summary>
		/// Adds building to players ownership.
		/// </summary>
		/// <param name="building">The added building.</param>
		public void AddBuilding(IBuilding building)
		{
			AddBuildingImpl(building);

			try {
				Plugin.BuildingAdded(building); 
			}
			catch (Exception e)
			{
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.BuildingAdded)} failed with Exception: {e.Message}");
			}
			
		}

		/// <summary>
		/// Tries to change the amount of <paramref name="resourceType"/> owned by the player by <paramref name="change"/>.
		/// Checks with the <see cref="Plugin"/> if it is possible, if not, may change by different amount or not at all.
		/// </summary>
		/// <param name="resourceType">The resourceType to change the amount of.</param>
		/// <param name="change">The size of the change.</param>
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

		/// <summary>
		/// Removes unit from the player.
		/// </summary>
		/// <param name="unit">The unit to remove.</param>
		/// <returns>True if the unit was removed, false if the unit did not belong to this player.</returns>
		public bool RemoveUnit(IUnit unit) {
			bool removed = units.TryGetValue(unit.UnitType, out var unitList) && unitList.Remove(unit);
			
			//Just deleting all the players units from the level
			if (IsRemovedFromLevel)
			{
				return removed;
			}

			if (removed) {
				try {
					Plugin.UnitKilled(unit);
				}
				catch (Exception e) {
					//NOTE: Maybe add cap to prevent message flood
					Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.UnitKilled)} failed with Exception: {e.Message}");
				}
			}

			return removed;
		}

		/// <summary>
		/// Removes building from the player.
		/// </summary>
		/// <param name="building">The building to remove.</param>
		/// <returns>True if the building was removed, false if the building did not belong to this player.</returns>
		public bool RemoveBuilding(IBuilding building) {
			
			bool removed = buildings.TryGetValue(building.BuildingType, out var buildingList) && buildingList.Remove(building);

			//Just deleting all the players units from the level
			if (IsRemovedFromLevel)
			{
				return removed;
			}

			if (removed) {
				try {
					Plugin.BuildingDestroyed(building);
				}
				catch (Exception e)
				{
					//NOTE: Maybe add cap to prevent message flood
					Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.BuildingDestroyed)} failed with Exception: {e.Message}");
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

		public IReadOnlyDictionary<ResourceType, double> GetAllResources()
		{
			return Resources;
		}

		public double GetResourceAmount(ResourceType resourceType)
		{
			resources.TryGetValue(resourceType, out double count);
			return count;
		}

		public IEnumerable<IPlayer> GetEnemyPlayers() {
			return from player in Level.Players
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
			if (IsDeleted || !EnabledEffective || !Level.LevelNode.Enabled) return;

			try {
				Plugin.OnUpdate(timeStep);
			}
			catch (Exception e)
			{
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Player plugin call {nameof(Plugin.OnUpdate)} failed with Exception: {e.Message}");
			}
		}

		/// <summary>
		/// Adds unit owned by this player.
		/// Split from the public AddUnit to be used for loading.
		/// </summary>
		/// <param name="unit">The unit to add.</param>
		void AddUnitImpl(IUnit unit)
		{
			if (units.TryGetValue(unit.UnitType, out var unitList))
			{
				unitList.Add(unit);
			}
			else
			{
				units.Add(unit.UnitType, new List<IUnit> { unit });
			}
		}


		/// <summary>
		/// Adds building owned by this player.
		/// Split from the public AddBuilding to be used for loading.
		/// </summary>
		/// <param name="unit">The building to add.</param>
		void AddBuildingImpl(IBuilding building)
		{
			if (buildings.TryGetValue(building.BuildingType, out var buildingList))
			{
				buildingList.Add(building);
			}
			else
			{
				buildings.Add(building.BuildingType, new List<IBuilding> { building });
			}
		} 
	}
}