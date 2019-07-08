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

		/// <summary>
		/// The instance representing the level when user is picking levels.
		/// </summary>
		LevelRep LevelRep { get; }

		/// <summary>
		/// The current application.
		/// </summary>
		MHUrhoApp App { get; }

		/// <summary>
		/// Game world map.
		/// </summary>
		IMap Map { get; }

		/// <summary>
		/// UI minimap.
		/// </summary>
		Minimap Minimap { get; }

		/// <summary>
		/// The scene graph of this level.
		/// </summary>
		Scene Scene { get; }

		/// <summary>
		/// The root of this level's part of the scene graph.
		/// </summary>
		Node LevelNode { get; }

		/// <summary>
		/// PackageManager of this application.
		/// </summary>
		PackageManager PackageManager { get; }

		/// <summary>
		/// The package this level belongs to.
		/// </summary>
		GamePack Package { get; }

		/// <summary>
		/// If the level is loaded for editing.
		/// </summary>
		bool EditorMode { get; }

		/// <summary>
		/// If the <see cref="End()"/> was called and we are currently cleaning up.
		/// </summary>
		bool IsEnding { get; }

		/// <summary>
		/// Units present in this level.
		/// </summary>
		IEnumerable<IUnit> Units { get; }

		/// <summary>
		/// Players present in this level.
		/// </summary>
		IEnumerable<IPlayer> Players { get; }

		/// <summary>
		/// Buildings present in this level.
		/// </summary>
		IEnumerable<IBuilding> Buildings { get; }

		/// <summary>
		/// Input subsystem for this level.
		/// </summary>
		IGameController Input { get; }

		/// <summary>
		/// UI subsystem for this level.
		/// </summary>
		GameUIManager UIManager { get; }

		/// <summary>
		/// Camera control for this level.
		/// </summary>
		CameraMover Camera { get; }

		/// <summary>
		/// Tool control for this level.
		/// </summary>
		ToolManager ToolManager { get; }

		/// <summary>
		/// Neutral player, the player that owns and controls the units and buildings making up the level scenery.
		/// </summary>
		IPlayer NeutralPlayer { get;  }

		/// <summary>
		/// Player instance representing the user.
		/// </summary>
		IPlayer HumanPlayer { get; }

		/// <summary>
		/// Logic plugin for the level.
		/// </summary>
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

		/// <summary>
		/// Stores the current level into <see cref="StLevel"/> and writes it into the provided <paramref name="stream"/>.
		/// If <paramref name="leaveOpen"/> is true, leaves the <paramref name="stream"/> open, otherwise closes it after writing.
		/// </summary>
		/// <param name="stream">The stream to write the serialized level into.</param>
		/// <param name="leaveOpen">If we should leave the stream open after the writing or close it.</param>
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

		/// <summary>
		/// Creates new projectile in the game world.
		/// </summary>
		/// <param name="projectileType">The type of the new projectile.</param>
		/// <param name="position">The initial position of the new projectile.</param>
		/// <param name="initRotation">The initial rotation of the new projectile.</param>
		/// <param name="player">The player owning the new projectile.</param>
		/// <param name="target">The target the new projectile is shooting at.</param>
		/// <returns>The instance of the new projectile if we were able to create it, null otherwise.</returns>
		IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, Quaternion initRotation, IPlayer player, IRangeTarget target);

		/// <summary>
		/// Creates new projectile in the game world.
		/// </summary>
		/// <param name="projectileType">The type of the new projectile.</param>
		/// <param name="position">The initial position of the new projectile.</param>
		/// <param name="initRotation">The initial rotation of the new projectile.</param>
		/// <param name="player">The player owning the new projectile.</param>
		/// <param name="movement">The initial movement of the projectile.</param>
		/// <returns>The instance of the new projectile if we were able to create it, null otherwise.</returns>
		IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, Quaternion initRotation, IPlayer player, Vector3 movement);

		/// <summary>
		/// Removes <paramref name="unit"/> from the level if it is present.
		/// </summary>
		/// <param name="unit">The unit to remove.</param>
		/// <returns>True if the <paramref name="unit"/> was removed from the level, false if there was no such unit in this level.</returns>
		bool RemoveUnit(IUnit unit);

		/// <summary>
		/// Removes <paramref name="building"/> from the level if it is present.
		/// </summary>
		/// <param name="building">The building to remove.</param>
		/// <returns>True if the <paramref name="building"/> was removed from the level, false if there was no such building in this level.</returns>
		bool RemoveBuilding(IBuilding building);

		/// <summary>
		/// Removes <paramref name="projectile"/> from the level if it is present.
		/// </summary>
		/// <param name="projectile">The projectile to remove.</param>
		/// <returns>True if the <paramref name="projectile"/> was removed from the level, false if there was no such projectile in this level.</returns>
		bool RemoveProjectile(IProjectile projectile);

		/// <summary>
		/// Removes <paramref name="player"/> from the level if it is present.
		/// </summary>
		/// <param name="player">The projectile to remove.</param>
		/// <returns>True if the <paramref name="player"/> was removed from the level, false if there was no such player in this level.</returns>
		bool RemovePlayer(IPlayer player);

		/// <summary>
		/// Returns the unit with ID equal <paramref name="ID"/>.
		/// Throws an exception if there is no unit with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">The id of the unit to retrieve.</param>
		/// <returns>The unit with the given <paramref name="ID"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when there is no unit with the given <paramref name="ID"/></exception>
		IUnit GetUnit(int ID);

		/// <summary>
		/// Returns the unit that is represented by the <paramref name="node"/> or one of its predecessors or one of its predecessors in the scene graph.
		/// Throws an exception if the node does not represent a unit.
		/// </summary>
		/// <param name="node">The node that is representing the unit in the scene graph.</param>
		/// <returns>The unit that is represented by the <paramref name="node"/> or one of its predecessors in the scene graph.</returns>
		IUnit GetUnit(Node node);

		/// <summary>
		/// Tries to get the unit with ID equal <paramref name="ID"/>.
		/// If there is a unit with the given <paramref name="ID"/>, returns true and sets <paramref name="unit"/> to reference that unit.
		/// If there is not a unit with the given <paramref name="ID"/>, returns false and sets the <paramref name="unit"/> to null.
		/// </summary>
		/// <param name="ID">The ID of the unit to retrieve.</param>
		/// <param name="unit">The unit with the given <paramref name="ID"/>, or null if no such unit exists.</param>
		/// <returns>True if a unit with the given <paramref name="ID"/> exists, false otherwise.</returns>
		bool TryGetUnit(int ID, out IUnit unit);

		/// <summary>
		/// Tries to get the unit represented by the given <paramref name="node"/> or one of its predecessors in the scene graph.
		/// If there is a unit represented by the given <paramref name="node"/> or one of its predecessors, returns true and sets <paramref name="unit"/> to reference that unit.
		/// If there is not a unit represented by the given <paramref name="node"/> or one of its predecessors, returns false and sets the <paramref name="unit"/> to null.
		/// </summary>
		/// <param name="node">The node possibly representing the unit in the scene graph.</param>
		/// <param name="unit">The unit represented by the given <paramref name="node"/> or one of its predecessors, or null if no such unit exists.</param>
		/// <returns>True if a unit represented by the given <paramref name="node"/> or one of its predecessors exists, false otherwise.</returns>
		bool TryGetUnit(Node node, out IUnit unit);

		/// <summary>
		/// Returns the building with ID equal <paramref name="ID"/>.
		/// Throws an exception if there is no building with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">The id of the building to retrieve.</param>
		/// <returns>The building with the given <paramref name="ID"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when there is no building with the given <paramref name="ID"/></exception>
		IBuilding GetBuilding(int ID);

		/// <summary>
		/// Returns the building that is represented by the <paramref name="node"/> or one of its predecessors or one of its predecessors in the scene graph.
		/// Throws an exception if the node does not represent a building.
		/// </summary>
		/// <param name="node">The node that is representing the building in the scene graph.</param>
		/// <returns>The building that is represented by the <paramref name="node"/> or one of its predecessors in the scene graph.</returns>
		IBuilding GetBuilding(Node node);

		/// <summary>
		/// Tries to get the building with ID equal <paramref name="ID"/>.
		/// If there is a building with the given <paramref name="ID"/>, returns true and sets <paramref name="building"/> to reference that building.
		/// If there is not a building with the given <paramref name="ID"/>, returns false and sets the <paramref name="building"/> to null.
		/// </summary>
		/// <param name="ID">The ID of the building to retrieve.</param>
		/// <param name="building">The building with the given <paramref name="ID"/>, or null if no such building exists.</param>
		/// <returns>True if a building with the given <paramref name="ID"/> exists, false otherwise.</returns>
		bool TryGetBuilding(int ID, out IBuilding building);

		/// <summary>
		/// Tries to get the building represented by the given <paramref name="node"/> or one of its predecessors in the scene graph.
		/// If there is a building represented by the given <paramref name="node"/> or one of its predecessors, returns true and sets <paramref name="building"/> to reference that building.
		/// If there is not a building represented by the given <paramref name="node"/> or one of its predecessors, returns false and sets the <paramref name="building"/> to null.
		/// </summary>
		/// <param name="node">The node possibly representing the building in the scene graph.</param>
		/// <param name="building">The building represented by the given <paramref name="node"/> or one of its predecessors, or null if no such building exists.</param>
		/// <returns>True if a building represented by the given <paramref name="node"/> or one of its predecessors exists, false otherwise.</returns>
		bool TryGetBuilding(Node node, out IBuilding building);


		/// <summary>
		/// Returns the player with ID equal <paramref name="ID"/>.
		/// Throws an exception if there is no player with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">The id of the player to retrieve.</param>
		/// <returns>The player with the given <paramref name="ID"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when there is no player with the given <paramref name="ID"/></exception>
		IPlayer GetPlayer(int ID);

		/// <summary>
		/// Tries to get the player with ID equal <paramref name="ID"/>.
		/// If there is a player with the given <paramref name="ID"/>, returns true and sets <paramref name="player"/> to reference that player.
		/// If there is not a player with the given <paramref name="ID"/>, returns false and sets the <paramref name="player"/> to null.
		/// </summary>
		/// <param name="ID">The ID of the player to retrieve.</param>
		/// <param name="player">The player with the given <paramref name="ID"/>, or null if no such player exists.</param>
		/// <returns>True if a player with the given <paramref name="ID"/> exists, false otherwise.</returns>
		bool TryGetPlayer(int ID, out IPlayer player);


		/// <summary>
		/// Returns the projectile with ID equal <paramref name="ID"/>.
		/// Throws an exception if there is no projectile with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">The id of the projectile to retrieve.</param>
		/// <returns>The projectile with the given <paramref name="ID"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when there is no projectile with the given <paramref name="ID"/></exception>
		IProjectile GetProjectile(int ID);

		/// <summary>
		/// Returns the projectile that is represented by the <paramref name="node"/> or one of its predecessors in the scene graph.
		/// Throws an exception if the node does not represent a projectile.
		/// </summary>
		/// <param name="node">The node that is representing the projectile in the scene graph.</param>
		/// <returns>The projectile that is represented by the <paramref name="node"/> or one of its predecessors in the scene graph.</returns>
		IProjectile GetProjectile(Node node);

		/// <summary>
		/// Tries to get the projectile with ID equal <paramref name="ID"/>.
		/// If there is a projectile with the given <paramref name="ID"/>, returns true and sets <paramref name="projectile"/> to reference that projectile.
		/// If there is not a projectile with the given <paramref name="ID"/>, returns false and sets the <paramref name="projectile"/> to null.
		/// </summary>
		/// <param name="ID">The ID of the projectile to retrieve.</param>
		/// <param name="projectile">The projectile with the given <paramref name="ID"/>, or null if no such projectile exists.</param>
		/// <returns>True if a projectile with the given <paramref name="ID"/> exists, false otherwise.</returns>
		bool TryGetProjectile(int ID, out IProjectile projectile);

		/// <summary>
		/// Tries to get the projectile represented by the given <paramref name="node"/> or one of its predecessors in the scene graph.
		/// If there is a projectile represented by the given <paramref name="node"/> or one of its predecessors, returns true and sets <paramref name="projectile"/> to reference that projectile.
		/// If there is not a projectile represented by the given <paramref name="node"/> or one of its predecessors, returns false and sets the <paramref name="projectile"/> to null.
		/// </summary>
		/// <param name="node">The node possibly representing the projectile in the scene graph.</param>
		/// <param name="projectile">The projectile represented by the given <paramref name="node"/> or one of its predecessors, or null if no such projectile exists.</param>
		/// <returns>True if a projectile represented by the given <paramref name="node"/> or one of its predecessors exists, false otherwise.</returns>
		bool TryGetProjectile(Node node, out IProjectile projectile);

		/// <summary>
		/// Returns the entity with ID equal <paramref name="ID"/>.
		/// Throws an exception if there is no entity with the given <paramref name="ID"/>.
		/// </summary>
		/// <param name="ID">The id of the entity to retrieve.</param>
		/// <returns>The entity with the given <paramref name="ID"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when there is no entity with the given <paramref name="ID"/></exception>
		IEntity GetEntity(int ID);

		/// <summary>
		/// Returns the entity that is represented by the <paramref name="node"/> or one of its predecessors in the scene graph.
		/// Throws an exception if the node does not represent a entity.
		/// </summary>
		/// <param name="node">The node that is representing the entity in the scene graph.</param>
		/// <returns>The entity that is represented by the <paramref name="node"/> or one of its predecessors in the scene graph.</returns>
		IEntity GetEntity(Node node);

		/// <summary>
		/// Tries to get an entity (unit, building or projectile) with ID equal <paramref name="ID"/>.
		/// If there is an entity with the given <paramref name="ID"/>, returns true and sets <paramref name="entity"/> to reference that entity.
		/// If there is not an entity with the given <paramref name="ID"/>, returns false and sets the <paramref name="entity"/> to null.
		/// </summary>
		/// <param name="ID">The ID of the entity to retrieve.</param>
		/// <param name="entity">The entity with the given <paramref name="ID"/>, or null if no such entity exists.</param>
		/// <returns>True if a entity with the given <paramref name="ID"/> exists, false otherwise.</returns>
		bool TryGetEntity(int ID, out IEntity entity);

		/// <summary>
		/// Tries to get the entity represented by the given <paramref name="node"/> or one of its predecessors in the scene graph.
		/// If there is an entity represented by the given <paramref name="node"/> or one of its predecessors, returns true and sets <paramref name="entity"/> to reference that entity.
		/// If there is not an entity represented by the given <paramref name="node"/> or one of its predecessors, returns false and sets the <paramref name="entity"/> to null.
		/// </summary>
		/// <param name="node">The node possibly representing the entity in the scene graph.</param>
		/// <param name="entity">The entity represented by the given <paramref name="node"/> or one of its predecessors, or null if no such entity exists.</param>
		/// <returns>True if an entity represented by the given <paramref name="node"/> or one of its predecessors exists, false otherwise.</returns>
		bool TryGetEntity(Node node, out IEntity entity);

		/// <summary>
		/// Gets the range target with an ID equal to <paramref name="ID"/>.
		/// If no such range target exists, throws an exception.
		/// </summary>
		/// <param name="ID">The ID of the wanted target.</param>
		/// <returns>The target with the given <paramref name="ID"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when there does is no target with the given <paramref name="ID"/>.</exception>
		IRangeTarget GetRangeTarget(int ID);

		/// <summary>
		/// Registers <paramref name="rangeTarget"/> to rangeTargets, assigns it a new ID and returns this new ID
		/// </summary>
		/// <param name="rangeTarget">Range target to register.</param>
		/// <returns>The new ID of the registered target.</returns>
		int RegisterRangeTarget(IRangeTarget rangeTarget);

		/// <summary>
		/// Removes the target with <paramref name="ID"/> from the set of registered targets.
		/// Returns true if target was removed, false if there was no such target.
		/// </summary>
		/// <param name="ID">The ID of the target to remove.</param>
		/// <returns>True if the target was remove, false if there was no target with the given <paramref name="ID"/>.</returns>
		bool UnRegisterRangeTarget(int ID);

		/// <summary>
		/// Casts a ray between <paramref name="source"/> and the <paramref name="target"/>. This ray is blocked by
		/// graphical graphical models of the map, buildings and/or units if the respective <paramref name="mapBlocks"/>, <paramref name="buildingsBlock"/>
		/// and <paramref name="unitsBlock"/> is true.
		/// </summary>
		/// <param name="source">The position of the observer.</param>
		/// <param name="target">The thing the observer is trying to see.</param>
		/// <param name="mapBlocks">If the map graphical representation blocks the observer.</param>
		/// <param name="buildingsBlock">If graphical representation of buildings blocks the observer.</param>
		/// <param name="unitsBlock">If graphical representation of units blocks the observer.</param>
		/// <returns>If observer positioned at <paramref name="source"/> can see the <paramref name="target"/> while being blocked by
		/// <paramref name="mapBlocks"/>, <paramref name="buildingsBlock"/> and <paramref name="unitsBlock"/>.</returns>
		bool CanSee(Vector3 source, IEntity target, bool mapBlocks = true, bool buildingsBlock = true, bool unitsBlock = false);

		/// <summary>
		/// Stops the scene updates in the scene graph. This stops logic and drawing alike.
		/// </summary>
		void Pause();

		/// <summary>
		///  Starts the scene updates in the scene graph again. This starts logic and drawing updates.
		/// </summary>
		void UnPause();

		/// <summary>
		/// Stops the level and releases all resources held by the level.
		/// </summary>
		void End();

		/// <summary>
		/// Changes the instance representing this level to the user to <paramref name="newLevelRep"/>.
		/// </summary>
		/// <param name="newLevelRep">The new instance representing this level to the user.</param>
		void ChangeRep(LevelRep newLevelRep);
	}
}
