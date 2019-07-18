using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MHUrho.Control;
using MHUrho.EditorTools;
using MHUrho.EntityInfo;
using MHUrho.Input;
using MHUrho.Packaging;
using Urho;
using Urho.Physics;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using Urho.Actions;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using MHUrho.Plugins;
using MHUrho.UserInterface;
using Google.Protobuf;
using MHUrho.CameraMovement;


namespace MHUrho.Logic
{
	/// <summary>
	/// Encapsulates methods that are invoked on each scene update.
	/// </summary>
	/// <param name="timeStep"></param>
	public delegate void OnUpdateDelegate(float timeStep);

	/// <summary>
	/// Encapsulates methods that are invoked on level ending.
	/// </summary>
	public delegate void OnEndDelegate();

	/// <summary>
	/// Main class representing the current level
	/// </summary>
	partial class LevelManager : Component, ILevelManager, IDisposable
	{
		/// <summary>
		/// Implementation of visitor that checks if the given entity is of the correct type.
		/// </summary>
		/// <typeparam name="T">The type that we are checking against.</typeparam>
		class TypeCheckVisitor<T> :IEntityVisitor<bool> {
			public bool Visit(IUnit unit)
			{
				return typeof(T) == typeof(IUnit);
			}

			public bool Visit(IBuilding building)
			{
				return typeof(T) == typeof(IBuilding);
			}

			public bool Visit(IProjectile projectile)
			{
				return typeof(T) == typeof(IProjectile);
			}
		}


		/// <summary>
		/// Currently running level, CANNOT BE USED DURING LOADING
		/// </summary>
		public static LevelManager CurrentLevel { get; private set; }

		/// <inheritdoc/>
		public LevelRep LevelRep { get; private set; }

		/// <inheritdoc/>
		public MHUrhoApp App { get; private set; }

		/// <inheritdoc/>
		public Node LevelNode { get; private set; }

		/// <inheritdoc/>
		public IMap Map => map;

		/// <inheritdoc/>
		public Minimap Minimap { get; private set; }

		/// <summary>
		/// Factory that provides default component loading.
		/// </summary>
		public DefaultComponentFactory DefaultComponentFactory { get; private set; }

		/// <inheritdoc/>
		public PackageManager PackageManager => App.PackageManager;

		/// <inheritdoc/>
		public GamePack Package => PackageManager.ActivePackage;

		/// <inheritdoc/>
		public bool EditorMode { get; private set; }

		/// <inheritdoc/>
		public IEnumerable<IUnit> Units => units.Values;

		/// <inheritdoc/>
		public IEnumerable<IPlayer> Players => players.Values;

		/// <inheritdoc/>
		public IEnumerable<IBuilding> Buildings => buildings.Values;

		/// <inheritdoc/>
		public IGameController Input { get; private  set; }

		/// <inheritdoc/>
		public GameUIManager UIManager => Input.UIManager;

		/// <inheritdoc/>
		public CameraMover Camera { get; private set; }

		/// <inheritdoc/>
		public ToolManager ToolManager { get; private set; }

		/// <inheritdoc/>
		public IPlayer NeutralPlayer { get; private set; }

		/// <inheritdoc/>
		public IPlayer HumanPlayer { get; private set; }

		/// <inheritdoc/>
		public LevelLogicInstancePlugin Plugin { get; private set; }

		/// <inheritdoc/>
		public event OnUpdateDelegate Update;

		/// <inheritdoc/>
		public event OnEndDelegate Ending;

		/// <inheritdoc/>
		public bool IsEnding { get; private set; }

		/// <summary>
		/// Controller that translates user input to camera movement.
		/// </summary>
		ICameraController cameraController;

		/// <summary>
		/// Engine component for raycasting.
		/// </summary>
		readonly Octree octree;

		/// <summary>
		/// All units present in the level.
		/// </summary>
		readonly Dictionary<int, IUnit> units;

		/// <summary>
		/// All players present in the level.
		/// </summary>
		readonly Dictionary<int, IPlayer> players;

		/// <summary>
		/// All buildings present in the level.
		/// </summary>
		readonly Dictionary<int, IBuilding> buildings;

		/// <summary>
		/// All projectiles active in the level.
		/// </summary>
		readonly Dictionary<int, IProjectile> projectiles;

		/// <summary>
		/// All entities (units, buildings, projectiles) in the game.
		/// </summary>
		readonly Dictionary<int, IEntity> entities;

		/// <summary>
		/// All range targets in the game.
		/// </summary>
		readonly Dictionary<int, IRangeTarget> rangeTargets;

		/// <summary>
		/// Mapping of game engine nodes to entities.
		/// </summary>
		readonly Dictionary<Node, IEntity> nodeToEntity;

		/// <summary>
		/// Random number generator to be used for generating IDs.
		/// </summary>
		readonly Random rng;
		/// <summary>
		/// The map of this level.
		/// </summary>
		Map map;

		/// <summary>
		/// Creates level manager that controls a level presented by <paramref name="levelRep"/> to the user,
		/// represented by <paramref name="levelNode"/> in the game engine.
		/// </summary>
		/// <param name="levelNode">The <see cref="Node"/> representing the level in the game engine.</param>
		/// <param name="levelRep">Presentation of the level for the user.</param>
		/// <param name="app">Current running application.</param>
		/// <param name="octree">Engine component used for raycasting.</param>
		/// <param name="editorMode">If the level is in editor mode or playig mode.</param>
		protected LevelManager(Node levelNode, LevelRep levelRep, MHUrhoApp app, Octree octree, bool editorMode)
		{
			this.LevelNode = levelNode;
			this.LevelRep = levelRep;
			this.EditorMode = editorMode;
			//Plugin is set in the loader after the creation of LevelManager
			//this.Plugin = plugin;

			this.units = new Dictionary<int, IUnit>();
			this.players = new Dictionary<int, IPlayer>();
			this.buildings = new Dictionary<int, IBuilding>();
			this.projectiles = new Dictionary<int, IProjectile>();
			this.entities = new Dictionary<int, IEntity>();
			this.rangeTargets = new Dictionary<int, IRangeTarget>();
			this.nodeToEntity = new Dictionary<Node, IEntity>();
			this.rng = new Random();

			this.App = app;
			this.octree = octree;
			this.DefaultComponentFactory = new DefaultComponentFactory();
			this.IsEnding = false;
			ReceiveSceneUpdates = true;
		}

		/// <summary>
		/// Returns loader that can load the level into playing mode.
		/// </summary>
		/// <param name="levelRep">Presentation of the level for the user.</param>
		/// <param name="storedLevel">Stored serialized state of the level.</param>
		/// <param name="players">Data to initialize the players.</param>
		/// <param name="customSettings">Data to initialize the level logic instance plugin.</param>
		/// <param name="parentProgress">Progress watcher for the parent process.</param>
		/// <param name="loadingSubsectionSize">Size of this loading process as a part of the parent process.</param>
		/// <returns>Loader that can load the level into playing mode.</returns>
		public static ILevelLoader GetLoaderForPlaying(LevelRep levelRep,
														StLevel storedLevel,
														PlayerSpecification players,
														LevelLogicCustomSettings customSettings,
														IProgressEventWatcher parentProgress = null,
														double loadingSubsectionSize = 100)
		{
			return new SavedLevelPlayingLoader(levelRep, storedLevel, players, customSettings, parentProgress, loadingSubsectionSize);
		}

		/// <summary>
		/// Returns loader that can load the level into editing mode, where players are just placeholders.
		/// </summary>
		/// <param name="levelRep">Presentation of the level for the user.</param>
		/// <param name="storedLevel">Stored serialized state of the level.</param>
		/// <param name="parentProgress">Progress watcher for the parent process.</param>
		/// <param name="loadingSubsectionSize">Size of this loading process as a part of the parent process.</param>
		/// <returns>Loader that can load the level into editing mode.</returns>
		public static ILevelLoader GetLoaderForEditing(LevelRep levelRep,
														StLevel storedLevel,
														IProgressEventWatcher parentProgress = null,
														double loadingSubsectionSize = 100)
		{
			return new SavedLevelEditorLoader(levelRep, storedLevel, parentProgress, loadingSubsectionSize);
		}

		/// <summary>
		/// Returns loader that can generate new level based on provided data.
		/// </summary>
		/// <param name="levelRep">Presentation of the level for the user.</param>
		/// <param name="mapSize">Size of the map to generate.</param>
		/// <param name="parentProgress">Progress watcher for the parent process.</param>
		/// <param name="loadingSubsectionSize">Size of this loading process as a part of the parent process.</param>
		/// <returns>Loader that can generate new level.</returns>
		public static ILevelLoader GetLoaderForDefaultLevel(LevelRep levelRep,
															IntVector2 mapSize,
															IProgressEventWatcher parentProgress = null,
															double loadingSubsectionSize = 100)
		{
			return new DefaultLevelLoader(levelRep, mapSize, parentProgress, loadingSubsectionSize);
		}

		/// <summary>
		/// Saves the level into an instance of <see cref="StLevel"/> for serialization.
		/// </summary>
		/// <returns>Level state stored in instance of <see cref="StLevel"/> for serialization.</returns>
		public StLevel Save() {
			StLevel level = new StLevel() {
				LevelName = LevelRep.Name,
				Map = this.Map.Save(),
				PackageName = PackageManager.ActivePackage.Name,
				Plugin = new StLevelPlugin()
			};


			level.Plugin = new StLevelPlugin {TypeID = LevelRep.LevelLogicType.ID, Data = new PluginData()};

			try {
				Plugin?.SaveState(new PluginDataWrapper(level.Plugin.Data, this));
			}
			catch (Exception e) {
				//NOTE: Maybe add cap to prevent message flood
				string message = $"Saving level plugin data failed with Exception: {e.Message}";
				Urho.IO.Log.Write(LogLevel.Error, message);
				throw new SavingException(message, e);
			}

			//Saving exception can be thrown during the enumerations

			level.Units.AddRange(from unit in units.Values
								select unit.Save());

			level.Buildings.AddRange(from building in buildings.Values
									select building.Save());

			level.Projectiles.AddRange(from projectile in projectiles.Values
										select projectile.Save());

			level.Players = new StPlayers
							{
								HumanPlayerID = HumanPlayer.ID,
								NeutralPlayerID = NeutralPlayer.ID
							};

			level.Players.Players.AddRange(from player in players.Values 
											select player.Save());

			return level;
		}

		/// <summary>
		/// Serializes the state of the level to the given <paramref name="stream"/>.
		/// </summary>
		/// <param name="stream">The stream to serialize the level into.</param>
		/// <param name="leaveOpen">If the stream should be left open after the serialization.</param>
		public void SaveTo(Stream stream, bool leaveOpen = false) {
			var storedLevel = Save();

			storedLevel.WriteTo(stream);

			if (!leaveOpen) {
				stream.Dispose();
			}
		}

		/// <summary>
		/// Ends the level and releases all resources held by the level and all it's parts.
		/// </summary>
		public new void Dispose()
		{
			IsEnding = true;
			Scene.UpdateEnabled = false;
			try {
				Ending?.Invoke();
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(Ending)}: {e.Message}");
			}

			List<IDisposable> toDispose = new List<IDisposable>();
			toDispose.AddRange(entities.Values);
			toDispose.AddRange(players.Values);


			foreach (var thing in toDispose)
			{
				thing?.Dispose();
			}

			//Everything that is loaded anywhere else but the constructor may not be loaded at the time of disposing
			PackageManager.ActivePackage.ClearCaches();
			ToolManager?.Dispose();
			Input?.Dispose();
			cameraController?.Dispose();
			Camera?.Dispose();
			Input = null;
			map?.Dispose();
			Minimap?.Dispose();
			octree?.Dispose();

			//Have to get the reference before i remove the level from the scene by RemoveAllChildren on the scene
			Scene scene = Scene;
			scene.RemoveAllChildren();
			scene.RemoveAllComponents();
			scene.Remove();
			scene.Dispose();
			LevelNode?.Dispose();

			base.Dispose();
			CurrentLevel = null;
			GC.Collect();
		}

		/// <summary>
		/// Ends the level and releases all resources held by the level and all it's parts.
		/// </summary>
		public void End()
		{
			Dispose();
		}



		/// <inheritdoc />
		public IUnit SpawnUnit(UnitType unitType, ITile tile, Quaternion initRotation, IPlayer player) {

			if (!unitType.CanSpawnAt(tile)) {
				return null;
			}

			IUnit newUnit = null;
			try {
				newUnit = unitType.CreateNewUnit(GetNewID(entities), this, tile, initRotation, player);
			}
			catch (CreationException) {
				return null;
			}

			//Could not spawn unit, user restrictions
			if (newUnit == null) {
				return null;
			}

			RegisterEntity(newUnit);
			units.Add(newUnit.ID,newUnit);
			player.AddUnit(newUnit);
			tile.AddUnit(newUnit);

			return newUnit;
		}

		/// <inheritdoc />
		public IBuilding BuildBuilding(BuildingType buildingType, IntVector2 topLeft, Quaternion initRotation, IPlayer player) {
			if (!buildingType.CanBuild(topLeft, player, this)) {
				return null;
			}

			IBuilding newBuilding;
			try {
				newBuilding = buildingType.BuildNewBuilding(GetNewID(entities), this, topLeft, initRotation, player);
			}
			catch (CreationException) {
				return null;
			}

			//Could not build building because of user restrictions
			if (newBuilding == null) {
				return null;
			}

			RegisterEntity(newBuilding);
			buildings.Add(newBuilding.ID,newBuilding);
			players[player.ID].AddBuilding(newBuilding);

			return newBuilding;
		}

		/// <inheritdoc />
		public IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, Quaternion initRotation, IPlayer player, IRangeTarget target)
		{

			IProjectile newProjectile;
			try {
				newProjectile = projectileType.ShootProjectile(GetNewID(entities), this, player, position, initRotation, target);
			}
			catch (CreationException) {
				return null;
			}
			//Could not spawn projectile, maybe out of range
			if (newProjectile == null) {
				return null;
			}


			RegisterEntity(newProjectile);
			projectiles.Add(newProjectile.ID, newProjectile);
				
			return newProjectile;
		}


		/// <inheritdoc />
		public IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, Quaternion initRotation, IPlayer player, Vector3 movement) {

			var newProjectile = projectileType.ShootProjectile(GetNewID(entities), this, player, position, initRotation, movement);
			if (newProjectile != null) {
				RegisterEntity(newProjectile);
				projectiles.Add(newProjectile.ID, newProjectile);
			}

			return newProjectile;

		}

		/// <inheritdoc />
		public bool RemoveUnit(IUnit unit)
		{
			bool removed = units.Remove(unit.ID) && RemoveEntity(unit);

			if (!unit.IsRemovedFromLevel) {
				unit.RemoveFromLevel();
			}

			return removed;
		}

		/// <inheritdoc />
		public bool RemoveBuilding(IBuilding building)
		{
			bool removed = buildings.Remove(building.ID) && RemoveEntity(building);

			if (!building.IsRemovedFromLevel) {
				building.RemoveFromLevel();
			}
			return removed;
		}

		/// <inheritdoc />
		public bool RemoveProjectile(IProjectile projectile)
		{
			bool removed = projectiles.Remove(projectile.ID) && RemoveEntity(projectile);

			if (!projectile.IsRemovedFromLevel) {
				projectile.RemoveFromLevel();
			}
			return removed;
		}

		/// <inheritdoc />
		public bool RemovePlayer(IPlayer player)
		{
			bool removed = players.Remove(player.ID);

			if (!player.IsRemovedFromLevel) {
				player.RemoveFromLevel();
			}

			if (player == HumanPlayer && !IsEnding) {
				Input.EndLevelToEndScreen(false);
			}

			return removed;
		}

		/// <inheritdoc />
		public IUnit GetUnit(int ID) {
			return TryGetUnit(ID, out IUnit value) ? 
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Unit with this ID does not exist in the current level");

		}

		/// <inheritdoc />
		public IUnit GetUnit(Node node)
		{
			return TryGetUnit(node, out IUnit value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(node), "Node does not contain any Units");
		}

		/// <inheritdoc />
		public bool TryGetUnit(int ID, out IUnit unit)
		{
			return units.TryGetValue(ID, out unit);
		}

		/// <inheritdoc />
		public bool TryGetUnit(Node node, out IUnit unit)
		{
			bool hasEntity = TryGetEntity(node, out IEntity entity);
			unit = null;
			return hasEntity && TryGetUnit(entity.ID, out unit);
		}

		/// <inheritdoc />
		public IBuilding GetBuilding(int ID) {
			return TryGetBuilding(ID, out IBuilding value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Building with this ID does not exist in the current level");
		}

		/// <inheritdoc />
		public IBuilding GetBuilding(Node node)
		{
			return TryGetBuilding(node, out IBuilding value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(node), "Node does not contain any Buildings");
		}

		/// <inheritdoc />
		public bool TryGetBuilding(int ID, out IBuilding building)
		{
			return buildings.TryGetValue(ID, out building);
		}

		/// <inheritdoc />
		public bool TryGetBuilding(Node node, out IBuilding building)
		{
			bool hasEntity = TryGetEntity(node, out IEntity entity);
			building = null;
			return hasEntity && TryGetBuilding(entity.ID, out building);
		}

		/// <inheritdoc />
		public IPlayer GetPlayer(int ID) {
			return TryGetPlayer(ID, out IPlayer value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Player with this ID does not exist in the current level");
		}

		/// <inheritdoc />
		public bool TryGetPlayer(int ID, out IPlayer player)
		{
			return players.TryGetValue(ID, out player);
		}

		/// <inheritdoc />
		public IProjectile GetProjectile(int ID) {
			return TryGetProjectile(ID, out IProjectile value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Projectile with this ID does not exist in the current level");
		}

		/// <inheritdoc />
		public IProjectile GetProjectile(Node node)
		{
			return TryGetProjectile(node, out IProjectile value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(node), "Node does not contain any Projectiles");
		}

		/// <inheritdoc />
		public bool TryGetProjectile(int ID, out IProjectile projectile)
		{
			return projectiles.TryGetValue(ID, out projectile);
		}

		/// <inheritdoc />
		public bool TryGetProjectile(Node node, out IProjectile projectile)
		{
			bool hasEntity = TryGetEntity(node, out IEntity entity);
			projectile = null;
			return hasEntity && TryGetProjectile(entity.ID, out projectile);
		}

		/// <inheritdoc />
		public IEntity GetEntity(int ID) {
			return TryGetEntity(ID, out IEntity value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Entity with this ID does not exist in the current level");

		}

		/// <inheritdoc />
		public IEntity GetEntity(Node node)
		{
			return TryGetEntity(node, out IEntity value)
						? value
						: throw new ArgumentOutOfRangeException(nameof(ID), "Node does not contain any entities");
		}

		/// <inheritdoc />
		public bool TryGetEntity(int ID, out IEntity entity)
		{
			return entities.TryGetValue(ID, out entity);
		}

		/// <inheritdoc />
		public bool TryGetEntity(Node node, out IEntity entity)
		{
			for (; node != LevelNode && node != null; node = node.Parent) {
				if (nodeToEntity.TryGetValue(node, out entity)) {
					return true;
				}
			}

			entity = null;
			return false;
		}

		/// <inheritdoc />
		public IRangeTarget GetRangeTarget(int ID) {
			if (!rangeTargets.TryGetValue(ID, out IRangeTarget value)) {
				throw new ArgumentOutOfRangeException(nameof(ID),"RangeTarget with this ID does not exist in the current level");
			}
			return value;
		}

		/// <inheritdoc />
		public int RegisterRangeTarget(IRangeTarget rangeTarget) {
			int newID = GetNewID(rangeTargets);
			rangeTarget.InstanceID = newID;
			rangeTargets.Add(rangeTarget.InstanceID, rangeTarget);
			return newID;
		}

		/// <summary>
		/// Loads range target that was stored and already has an ID.
		/// </summary>
		/// <param name="rangeTarget">The range target with ID.</param>
		internal void LoadRangeTarget(IRangeTarget rangeTarget)
		{
			rangeTargets.Add(rangeTarget.InstanceID, rangeTarget);
		}

		/// <inheritdoc />
		public bool UnRegisterRangeTarget(int ID) {
			return rangeTargets.Remove(ID);
		}

		/// <inheritdoc />
		public bool CanSee(Vector3 source, IEntity target, bool mapBlocks = true, bool buildingsBlock = true, bool unitsBlock = false)
		{
			Vector3 diff = target.Position - source;
			Ray visionRay = new Ray(source, diff);
			List<RayQueryResult> results =  octree.Raycast(visionRay,
															RayQueryLevel.Aabb,
															diff.Length * 1.2f); //distance between source and target with 20% reserve
			TypeCheckVisitor<IUnit> unitCheck = new TypeCheckVisitor<IUnit>();
			TypeCheckVisitor<IBuilding> buildingCheck = new TypeCheckVisitor<IBuilding>();
			foreach (var result in results) {
				// If we hit a map
				if (Map.IsRaycastToMap(result)) {
					// If map blocks sight, return false, else check next result
					if (mapBlocks) {
						return false;
					}
					continue;
				}

				if (!nodeToEntity.TryGetValue(result.Node, out IEntity entity)) continue;

				if (entity == target) {
					return true;
				}

				// Entity is not target, check if it is unit
				if (entity.Accept(unitCheck)) {
					// it is unit, if units block sight, return false, else check next result
					if (unitsBlock) {
						return false;
					}
					continue;
				}

				// Entity is not a unit, check if it is a building
				if (entity.Accept(buildingCheck)) {
					// Entity is a building, if buildings block sight, return false, else check next result
					if (buildingsBlock) {
						return false;
					}
					continue;
				}
				
				// it was not the map, nor unit, nor building, just check next, ignore
			}

			return false;
		}

		/// <inheritdoc />
		public void Pause()
		{
			Scene.UpdateEnabled = false;
			LevelNode.SetEnabledRecursive(false);
		}

		/// <inheritdoc />
		public void UnPause()
		{
			Scene.UpdateEnabled = true;
			LevelNode.SetEnabledRecursive(true);
		}

		/// <inheritdoc />
		public void ChangeRep(LevelRep newLevelRep)
		{
			LevelRep.DetachFromLevel();
			LevelRep = newLevelRep;
		}

		/// <summary>
		/// Handles scene updates.
		/// </summary>
		/// <param name="timeStep">Time elapsed since the last scene update.</param>
		protected override void OnUpdate(float timeStep) {
			if (IsDeleted || !EnabledEffective) return;

			Minimap.OnUpdate(timeStep);

			try {
				Plugin.OnUpdate(timeStep);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"Level plugin call {nameof(Plugin.OnUpdate)} failed with Exception: {e.Message}");
			}

			if (IsEnding) {
				return;
			}

			try {
				
				Update?.Invoke(timeStep);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(Update)}: {e.Message}");
			}

		}

	
		/// <summary>
		/// Maximum tries of generating random ID, after which we presume we exhausted the supply of IDs.
		/// </summary>
		const int MaxTries = 10000000;

		/// <summary>
		/// Generates a new random ID that is not already present in the <paramref name="dictionary"/>.
		/// </summary>
		/// <typeparam name="T">The value type of the dictionary.</typeparam>
		/// <param name="dictionary">The dictionary of used IDs.</param>
		/// <returns>New random ID that is not already in the <paramref name="dictionary"/>.</returns>
		int GetNewID<T>(IDictionary<int, T> dictionary) {
			int id, i = 0;
			while (dictionary.ContainsKey(id = rng.Next()) || id == 0) {
				i++;
				if (i > MaxTries) {
					//TODO: Exception
					throw new Exception("Could not find free ID");
				}
			}

			return id;
		}

		/// <summary>
		/// Registers entity to the entity registry and to the node to entity mapping.
		/// </summary>
		/// <param name="entity">The entity to register.</param>
		void RegisterEntity(IEntity entity)
		{
			entities.Add(entity.ID, entity);
			nodeToEntity.Add(entity.Node, entity);
		}

		/// <summary>
		/// Removes entity from the level.
		/// </summary>
		/// <param name="entity">The entity to remove.</param>
		/// <returns>True if the entity was removed, false if the entity was not registered in the level.</returns>
		bool RemoveEntity(IEntity entity)
		{
			bool removed = entities.Remove(entity.ID);
			if (!IsEnding) {
				return nodeToEntity.Remove(entity.Node) && removed;
			}
			else {
				//DO NOT TOUCH THE NODES, they may be deleted
				return removed;
			}
			
		}

	}
}
   

