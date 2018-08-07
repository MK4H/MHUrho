using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic {
	public interface ILevelManager {


		MyGame App { get; }

		Map Map { get; }

		Minimap Minimap { get; }

		Scene Scene { get; }

		Node LevelNode { get; }

		DefaultComponentFactory DefaultComponentFactory { get; }

		PackageManager PackageManager { get; }

		bool EditorMode { get; }

		IEnumerable<IUnit> Units { get; }

		IEnumerable<IPlayer> Players { get; }

		IEnumerable<IBuilding> Buildings { get; }

		IGameController Input { get; }

		CameraMover Camera { get; }

		ToolManager ToolManager { get; }

		event OnUpdateDelegate Update;

		StLevel Save();

		void SaveTo(Stream stream, bool leaveOpen = false);

		IUnit SpawnUnit(UnitType unitType, ITile tile, IPlayer player);

		IBuilding BuildBuilding(BuildingType buildingType, IntVector2 topLeft, IPlayer player);

		IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, IPlayer player, IRangeTarget target);

		IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, IPlayer player, Vector3 movement);

		bool RemoveUnit(IUnit unit);

		bool RemoveBuilding(IBuilding building);

		bool RemoveProjectile(IProjectile projectile);

		IUnit GetUnit(int ID);

		IUnit GetUnit(Node node);

		bool TryGetUnit(int ID, out IUnit unit);

		bool TryGetUnit(Node node, out IUnit unit);

		
		IBuilding GetBuilding(int ID);

		IBuilding GetBuilding(Node node);

		bool TryGetBuilding(int ID, out IBuilding building);

		bool TryGetBuilding(Node node, out IBuilding building);

		IPlayer GetPlayer(int ID);

		bool TryGetPlayer(int ID, out IPlayer player);

		IProjectile GetProjectile(int ID);

		IProjectile GetProjectile(Node node);

		bool TryGetProjectile(int ID, out IProjectile projectile);

		bool TryGetProjectile(Node node, out IProjectile projectile);

		IEntity GetEntity(int ID);

		IEntity GetEntity(Node node);

		bool TryGetEntity(int ID, out IEntity entity);

		bool TryGetEntity(Node node, out IEntity entity);

		IRangeTarget GetRangeTarget(int ID);

		int RegisterRangeTarget(IRangeTarget rangeTarget);

		bool UnRegisterRangeTarget(int ID);

		bool CanSee(Vector3 source, IEntity target, bool mapBlocks = true, bool buildingsBlock = true, bool unitsBlock = false);

		void Pause();

		void UnPause();

		void End();
	}
}
