using System;
using System.Collections.Generic;
using MHUrho.EntityInfo;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {

	/// <summary>
	/// Player in the current level.
	/// </summary>
	public interface IPlayer : IDisposable {

		/// <summary>
		/// Unique identifier of the player inside the level.
		/// </summary>
		int ID { get; }

		/// <summary>
		/// Unique identifier of the players team.
		/// </summary>
		int TeamID { get; }

		/// <summary>
		/// The graphical representation of the player added to units.
		/// </summary>
		PlayerInsignia Insignia { get; }

		/// <summary>
		/// The level this player is part of.
		/// </summary>
		ILevelManager Level { get; }

		/// <summary>
		/// Plugin of the player.
		/// </summary>
		PlayerAIInstancePlugin Plugin { get; }

		/// <summary>
		/// Type of the player as loaded from the package.
		/// </summary>
		PlayerType PlayerType { get; }

		/// <summary>
		/// If the player has been removed from level
		/// </summary>
		bool IsRemovedFromLevel { get; }

		/// <summary>
		/// All resources owned by the player and their amounts.
		/// </summary>
		IReadOnlyDictionary<ResourceType, double> Resources { get; }

		/// <summary>
		/// Invoked on player removal from level.
		/// </summary>
		event Action<IPlayer> OnRemoval;

		/// <summary>
		/// Serializes current state of the player into an instance of <see cref="StPlayer"/>.
		/// </summary>
		/// <returns>Serialized current state.</returns>
		StPlayer Save();

		/// <summary>
		/// Removes the player and all that he owns from the level.
		/// </summary>
		void RemoveFromLevel();

		/// <summary>
		/// Gets all units that belong to this player.
		/// </summary>
		/// <returns>All units that belong to this player.</returns>
		IEnumerable<IUnit> GetAllUnits();

		/// <summary>
		/// Gets all units of type <paramref name="unitType"/> that belong to this player.
		/// </summary>
		/// <param name="unitType">The type of the units to retrieve.</param>
		/// <returns>All units of type <paramref name="unitType"/> that belong to this player</returns>
		IReadOnlyList<IUnit> GetUnitsOfType(UnitType unitType);

		/// <summary>
		/// Gets all building that belong to this player.
		/// </summary>
		/// <returns>All buildings that belong to this player.</returns>
		IEnumerable<IBuilding> GetAllBuildings();

		/// <summary>
		/// Gets all buildings of type <paramref name="buildingType"/> that belong to this player.
		/// </summary>
		/// <param name="buildingType">The type of the buildings to retrieve.</param>
		/// <returns>All buildings of type <paramref name="buildingType"/> that belong to this player</returns>
		IReadOnlyList<IBuilding> GetBuildingsOfType(BuildingType buildingType);

		/// <summary>
		/// Gets all resources and their amounts owned by this player.
		/// </summary>
		/// <returns></returns>
		IReadOnlyDictionary<ResourceType, double> GetAllResources();

		/// <summary>
		/// Returns the amount of resource of type <paramref name="resourceType"/> owned by this player.
		/// </summary>
		/// <param name="resourceType">The type of resource we want to know the amount of.</param>
		/// <returns>The amount of resource of type <paramref name="resourceType"/> owned by this player.</returns>
		double GetResourceAmount(ResourceType resourceType);

		/// <summary>
		/// Returns all enemy players.
		/// </summary>
		/// <returns>All enemy players.</returns>
		IEnumerable<IPlayer> GetEnemyPlayers();

		/// <summary>
		/// Checks if the provided <paramref name="player"/> is in the same team.
		/// </summary>
		/// <param name="player">The player to check.</param>
		/// <returns>True if the <paramref name="player"/> is in the same team, false otherwise.</returns>
		bool IsFriend(IPlayer player);

		/// <summary>
		/// Checks if the provided <paramref name="player"/> is in the same team.
		/// </summary>
		/// <param name="player">The player to check.</param>
		/// <returns>False if the <paramref name="player"/> is in the same team, true otherwise.</returns>
		bool IsEnemy(IPlayer player);

		/// <summary>
		/// Adds unit to players ownership.
		/// </summary>
		/// <param name="unit">The unit to add to the player.</param>
		void AddUnit(IUnit unit);

		/// <summary>
		/// Adds building to players ownership.
		/// </summary>
		/// <param name="building">The building to add to the player.</param>
		void AddBuilding(IBuilding building);

		/// <summary>
		/// Removes <paramref name="unit"/> from players ownership.
		/// </summary>
		/// <param name="unit">The unit to remove from the player.</param>
		bool RemoveUnit(IUnit unit);


		/// <summary>
		/// Removes <paramref name="building"/> from players ownership.
		/// </summary>
		/// <param name="building">The building to remove from the player.</param>
		bool RemoveBuilding(IBuilding building);

		/// <summary>
		/// Tries to change the amount of resource of the type <paramref name="resourceType"/> owned by the player by <paramref name="change"/>.
		/// Checks with the <see cref="Plugin"/> if it is possible, if not, may change by different amount or not at all.
		/// </summary>
		/// <param name="resourceType">The type of the resource to change.</param>
		/// <param name="change">The size of the change.</param>
		void ChangeResourceAmount(ResourceType resourceType, double change);
	}
}