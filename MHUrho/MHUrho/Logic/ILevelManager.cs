using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic {
	public interface ILevelManager {
		float GameSpeed { get; set; }

		Map Map { get; }

		Scene Scene { get; }

		DefaultComponentFactory DefaultComponentFactory { get; }

		PackageManager PackageManager { get; }

		event OnUpdateDelegate Update;

		IEnumerable<IUnit> Units { get; }

		IEnumerable<IPlayer> Players { get; }

		IEnumerable<IBuilding> Buildings { get; }

		IUnit SpawnUnit(UnitType unitType, ITile tile, IPlayer player);

		IBuilding BuildBuilding(BuildingType buildingType, IntVector2 topLeft, IPlayer player);

		IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, IPlayer player, IRangeTarget target);

		IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, IPlayer player, Vector3 movement);

		bool RemoveUnit(IUnit unit);

		bool RemoveBuilding(IBuilding building);

		bool RemoveProjectile(IProjectile projectile);

		IUnit GetUnit(int ID);

		IBuilding GetBuilding(int ID);

		IPlayer GetPlayer(int ID);

		IEntity GetEntity(int ID);

		IRangeTarget GetRangeTarget(int ID);

		int RegisterRangeTarget(IRangeTarget rangeTarget);

		bool UnRegisterRangeTarget(int ID);
	}
}
