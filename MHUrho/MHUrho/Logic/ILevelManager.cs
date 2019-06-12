using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic {
	public interface ILevelManager : IDisposable {

		LevelRep LevelRep { get; }

		MHUrhoApp App { get; }

		IMap Map { get; }

		Minimap Minimap { get; }

		Scene Scene { get; }

		Node LevelNode { get; }

		PackageManager PackageManager { get; }

		GamePack Package { get; }

		bool EditorMode { get; }

		bool IsEnding { get; }

		IEnumerable<IUnit> Units { get; }

		IEnumerable<IPlayer> Players { get; }

		IEnumerable<IBuilding> Buildings { get; }

		IGameController Input { get; }

		GameUIManager UIManager { get; }

		CameraMover Camera { get; }

		ToolManager ToolManager { get; }

		IPlayer NeutralPlayer { get;  }

		IPlayer HumanPlayer { get; }

		LevelLogicInstancePlugin Plugin { get; }

		/// <summary>
		/// Invoked on each scene update.
		/// </summary>
		event OnUpdateDelegate Update;

		/// <summary>
		/// Invoked when level is ending.
		/// </summary>
		event OnEndDelegate Ending;

		/// <summary>
		/// Stores the current level into a StLevel object.
		/// </summary>
		/// <returns>Stored level</returns>
		/// <exception cref="SavingException">Thrown when the saving of the level fails</exception>
		StLevel Save();

		void SaveTo(Stream stream, bool leaveOpen = false);

		/// <summary>
		/// Spawns new unit of given <paramref name="unitType"/> into the world map at <paramref name="tile"/>.
		/// </summary>
		/// <param name="unitType">The unit to be added.</param>
		/// <param name="tile">Tile to spawn the unit at.</param>
		/// <param name="initRotation">Initial rotation of the spawned unit.</param>
		/// <param name="player">owner of the new unit.</param>
		/// <returns>The new unit if a unit was spawned, or null if no unit was spawned.</returns>
		IUnit SpawnUnit(UnitType unitType, ITile tile, Quaternion initRotation, IPlayer player);

		/// <summary>
		/// Creates new building in the world.
		/// </summary>
		/// <param name="buildingType">Type of the new building.</param>
		/// <param name="topLeft">Coordinates of the top leftmost tile the building will occupy.</param>
		/// <param name="initRotation">Initial rotation of the building when it is create.d</param>
		/// <param name="player">Owner of the building.</param>
		/// <returns>The new building if it was built, or null if the building could not be built.</returns>
		IBuilding BuildBuilding(BuildingType buildingType, IntVector2 topLeft, Quaternion initRotation, IPlayer player);

		IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, Quaternion initRotation, IPlayer player, IRangeTarget target);

		IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, Quaternion initRotation, IPlayer player, Vector3 movement);

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

		void ChangeRep(LevelRep newLevelRep);
	}
}
