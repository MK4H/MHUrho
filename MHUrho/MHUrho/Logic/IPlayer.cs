using System.Collections.Generic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {
	public interface IPlayer {

		int ID { get; }

		IEnumerable<Unit> GetAllUnits();

		IReadOnlyList<Unit> GetUnitsOfType(UnitType type);

		IEnumerable<Building> GetAllBuildings();

		IReadOnlyList<Building> GetBuildingsOfType(BuildingType type);

		int GetResourcesOfType(ResourceType type);

		IEnumerable<IPlayer> GetEnemyPlayers();

		bool IsFriend(IPlayer player);

		bool IsEnemy(IPlayer player);

		void AddUnit(Unit unit);

		void AddBuilding(Building building);

		bool RemoveUnit(Unit unit);
		bool RemoveBuilding(Building building);
	}
}