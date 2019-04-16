using System.Collections.Generic;
using MHUrho.EntityInfo;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {
	public interface IPlayer {

		int ID { get; }

		int TeamID { get; }

		PlayerInsignia Insignia { get; }

		StPlayer Save();

		IEnumerable<IUnit> GetAllUnits();

		IReadOnlyList<IUnit> GetUnitsOfType(UnitType unitType);

		IEnumerable<IBuilding> GetAllBuildings();

		IReadOnlyList<IBuilding> GetBuildingsOfType(BuildingType buildingType);

		double GetResourceAmount(ResourceType resourceType);

		IEnumerable<IPlayer> GetEnemyPlayers();

		bool IsFriend(IPlayer player);

		bool IsEnemy(IPlayer player);

		void AddUnit(IUnit unit);

		void AddBuilding(IBuilding building);

		bool RemoveUnit(IUnit unit);

		bool RemoveBuilding(IBuilding building);

		void ChangeResourceAmount(ResourceType resourceType, double change);
	}
}