using System.Collections.Generic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {
	public interface IPlayer {

		int ID { get; }

		Color Color { get; }

		StPlayer Save();

		IEnumerable<IUnit> GetAllUnits();

		IReadOnlyList<IUnit> GetUnitsOfType(UnitType type);

		IEnumerable<IBuilding> GetAllBuildings();

		IReadOnlyList<IBuilding> GetBuildingsOfType(BuildingType type);

		int GetResourcesOfType(ResourceType type);

		IEnumerable<IPlayer> GetEnemyPlayers();

		bool IsFriend(IPlayer player);

		bool IsEnemy(IPlayer player);

		void AddUnit(IUnit unit);

		void AddBuilding(IBuilding building);

		bool RemoveUnit(IUnit unit);
		bool RemoveBuilding(IBuilding building);
	}
}