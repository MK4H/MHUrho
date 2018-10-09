using System;
using System.Collections.Generic;
using System.IO;
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
using MHUrho.UnitComponents;
using Urho.Actions;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using MHUrho.Plugins;
using MHUrho.UserInterface;
using Google.Protobuf;


namespace MHUrho.Logic
{

	public delegate void OnUpdateDelegate(float timeStep);

	partial class LevelManager : Component, ILevelManager, IDisposable
	{
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

		public LevelRep LevelRep { get; private set; }

		public MyGame App { get; private set; }

		public Node LevelNode { get; private set; }

		public Map Map { get; private set; }

		public Minimap Minimap { get; private set; }

		public DefaultComponentFactory DefaultComponentFactory { get; private set; }

		public PackageManager PackageManager => PackageManager.Instance;


		public bool EditorMode { get; private set; }

		public IEnumerable<IUnit> Units => units.Values;

		public IEnumerable<IPlayer> Players => players.Values;

		public IEnumerable<IBuilding> Buildings => buildings.Values;

		public IGameController Input { get; private  set; }

		public GameUIManager UIManager => Input.UIManager;

		public CameraMover Camera { get; private set; }

		public ToolManager ToolManager { get; private set; }

		public IPlayer NeutralPlayer { get; private set; }

		public IPlayer HumanPlayer { get; private set; }

		public LevelLogicInstancePlugin Plugin { get; private set; }

		public event OnUpdateDelegate Update;

		ICameraController cameraController;

		readonly Octree octree;

		readonly Dictionary<int, IUnit> units;
		readonly Dictionary<int, IPlayer> players;
		readonly Dictionary<int, IBuilding> buildings;
		readonly Dictionary<int, IProjectile> projectiles;

		readonly Dictionary<int, IEntity> entities;

		readonly Dictionary<int, IRangeTarget> rangeTargets;

		readonly Dictionary<Node, IEntity> nodeToEntity;

		readonly Random rng;

		protected LevelManager(Node levelNode, LevelRep levelRep, MyGame app, Octree octree, bool editorMode)
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
			ReceiveSceneUpdates = true;
		}

		public static ILevelLoader GetLoader()
		{
			return new Loader();
		}

		public StLevel Save() {
			StLevel level = new StLevel() {
				LevelName = LevelRep.Name,
				Map = this.Map.Save(),
				PackageName = PackageManager.Instance.ActivePackage.Name,
				Plugin = new StLevelPlugin()
			};


			level.Plugin = new StLevelPlugin {TypeID = LevelRep.LevelLogicType.ID, Data = new PluginData()};

			Plugin?.SaveState(new PluginDataWrapper(level.Plugin.Data, this));

			foreach (var unit in units.Values) {
				level.Units.Add(unit.Save());
			}

			foreach (var building in buildings.Values) {
				level.Buildings.Add(building.Save());
			}

			foreach (var projectile in projectiles.Values) {
				level.Projectiles.Add(projectile.Save());
			}

			level.Players = new StPlayers
							{
								HumanPlayerID = HumanPlayer.ID,
								NeutralPlayerID = NeutralPlayer.ID
							};
			foreach (var player in players.Values) {
				level.Players.Players.Add(player.Save());
			}

			return level;
		}

		public void SaveTo(Stream stream, bool leaveOpen = false) {
			var storedLevel = Save();

			storedLevel.WriteTo(stream);

			if (!leaveOpen) {
				stream.Dispose();
			}
		}

		public new void Dispose()
		{
			List<IDisposable> toDispose = new List<IDisposable>();
			toDispose.AddRange(entities.Values);

			foreach (var thing in toDispose)
			{
				thing.Dispose();
			}

			PackageManager.ActivePackage.ClearCaches();
			ToolManager.Dispose();
			Input.Dispose();
			cameraController.Dispose();
			Camera.Dispose();
			Input = null;
			Map.Dispose();
			Minimap.Dispose();
			octree.Dispose();

			//Have to get the reference before i remove the level from the scene by RemoveAllChildren on the scene
			Scene scene = Scene;
			scene.RemoveAllChildren();
			scene.RemoveAllComponents();
			scene.Remove();
			scene.Dispose();
			LevelNode.Dispose();

			LevelRep.LevelEnded();

			base.Dispose();

			CurrentLevel = null;
			GC.Collect();
		}

		public void End()
		{
			Dispose();
		}

		

		/// <summary>
		/// Spawns new unit of given <paramref name="unitType"/> into the world map at <paramref name="tile"/>
		/// </summary>
		/// <param name="unitType">The unit to be added</param>
		/// <param name="tile">Tile to spawn the unit at</param>
		/// <param name="player">owner of the new unit</param>
		/// <returns>The new unit if a unit was spawned, or null if no unit was spawned</returns>
		public IUnit SpawnUnit(UnitType unitType, ITile tile, IPlayer player) {

			if (!unitType.CanSpawnAt(tile)) {
				return null;
			}

			Node unitNode = LevelNode.CreateChild("Unit");

			var newUnit = unitType.CreateNewUnit(GetNewID(entities), unitNode, this, tile, player);
			RegisterEntity(newUnit);
			units.Add(newUnit.ID,newUnit);
			player.AddUnit(newUnit);
			tile.AddUnit(newUnit);

			return newUnit;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buildingType"></param>
		/// <param name="topLeft"></param>
		/// <param name="player"></param>
		/// <returns>The new building if building was built, or null if the building could not be built</returns>
		public IBuilding BuildBuilding(BuildingType buildingType, IntVector2 topLeft, IPlayer player) {
			if (!buildingType.CanBuildIn(buildingType.GetBuildingTilesRectangle(topLeft), this)) {
				return null;
			}

			Node buildingNode = LevelNode.CreateChild("Building");

			var newBuilding = buildingType.BuildNewBuilding(GetNewID(entities), buildingNode, this, topLeft, player);
			RegisterEntity(newBuilding);
			buildings.Add(newBuilding.ID,newBuilding);
			players[player.ID].AddBuilding(newBuilding);

			return newBuilding;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="projectileType"></param>
		/// <param name="position"></param>
		/// <param name="player"></param>
		/// <param name="target"></param>
		/// <returns>null if the projectile could not be spawned, the new projectile otherwise</returns>
		public IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, IPlayer player, IRangeTarget target) {

			var newProjectile = projectileType.ShootProjectile(GetNewID(entities), this, player, position, target);

			if (newProjectile != null) {
				RegisterEntity(newProjectile);
				projectiles.Add(newProjectile.ID, newProjectile);
			}
			
			return newProjectile;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="projectileType"></param>
		/// <param name="position"></param>
		/// <param name="player"></param>
		/// <param name="movement"></param>
		/// <returns>null if the projectile could not be spawned, the new projectile otherwise</returns>
		public IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 position, IPlayer player, Vector3 movement) {

			var newProjectile = projectileType.ShootProjectile(GetNewID(entities), this, player, position, movement);
			if (newProjectile != null) {
				RegisterEntity(newProjectile);
				projectiles.Add(newProjectile.ID, newProjectile);
			}

			return newProjectile;

		}


		public bool RemoveUnit(IUnit unit)
		{
			bool removed = units.Remove(unit.ID) && RemoveEntity(unit);

			if (!unit.RemovedFromLevel) {
				unit.RemoveFromLevel();
			}

			return removed;
		}

		public bool RemoveBuilding(IBuilding building)
		{
			bool removed = buildings.Remove(building.ID) && RemoveEntity(building);

			if (!building.RemovedFromLevel) {
				building.RemoveFromLevel();
			}
			return removed;
		}

		public bool RemoveProjectile(IProjectile projectile)
		{
			bool removed = projectiles.Remove(projectile.ID) && RemoveEntity(projectile);

			if (!projectile.RemovedFromLevel) {
				projectile.RemoveFromLevel();
			}
			return removed;
		}

		public IUnit GetUnit(int ID) {
			return TryGetUnit(ID, out IUnit value) ? 
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Unit with this ID does not exist in the current level");

		}

		public IUnit GetUnit(Node node)
		{
			return TryGetUnit(node, out IUnit value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(node), "Node does not contain any Units");
		}

		public bool TryGetUnit(int ID, out IUnit unit)
		{
			return units.TryGetValue(ID, out unit);
		}

		public bool TryGetUnit(Node node, out IUnit unit)
		{
			bool hasEntity = TryGetEntity(node, out IEntity entity);
			unit = null;
			return hasEntity && TryGetUnit(entity.ID, out unit);
		}

		public IBuilding GetBuilding(int ID) {
			return TryGetBuilding(ID, out IBuilding value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Building with this ID does not exist in the current level");
		}

		public IBuilding GetBuilding(Node node)
		{
			return TryGetBuilding(node, out IBuilding value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(node), "Node does not contain any Buildings");
		}

		public bool TryGetBuilding(int ID, out IBuilding building)
		{
			return buildings.TryGetValue(ID, out building);
		}

		public bool TryGetBuilding(Node node, out IBuilding building)
		{
			bool hasEntity = TryGetEntity(node, out IEntity entity);
			building = null;
			return hasEntity && TryGetBuilding(entity.ID, out building);
		}

		public IPlayer GetPlayer(int ID) {
			return TryGetPlayer(ID, out IPlayer value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Player with this ID does not exist in the current level");
		}

		public bool TryGetPlayer(int ID, out IPlayer player)
		{
			return players.TryGetValue(ID, out player);
		}

		public IProjectile GetProjectile(int ID) {
			return TryGetProjectile(ID, out IProjectile value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Projectile with this ID does not exist in the current level");
		}

		public IProjectile GetProjectile(Node node)
		{
			return TryGetProjectile(node, out IProjectile value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(node), "Node does not contain any Projectiles");
		}

		public bool TryGetProjectile(int ID, out IProjectile projectile)
		{
			return projectiles.TryGetValue(ID, out projectile);
		}

		public bool TryGetProjectile(Node node, out IProjectile projectile)
		{
			bool hasEntity = TryGetEntity(node, out IEntity entity);
			projectile = null;
			return hasEntity && TryGetProjectile(entity.ID, out projectile);
		}

		public IEntity GetEntity(int ID) {
			return TryGetEntity(ID, out IEntity value) ?
						value :
						throw new ArgumentOutOfRangeException(nameof(ID), "Entity with this ID does not exist in the current level");

		}

		public IEntity GetEntity(Node node)
		{
			return TryGetEntity(node, out IEntity value)
						? value
						: throw new ArgumentOutOfRangeException(nameof(ID), "Node does not contain any entities");
		}

		public bool TryGetEntity(int ID, out IEntity entity)
		{
			return entities.TryGetValue(ID, out entity);
		}

		public bool TryGetEntity(Node node, out IEntity entity)
		{
			return nodeToEntity.TryGetValue(node, out entity);
		}

		public IRangeTarget GetRangeTarget(int ID) {
			if (!rangeTargets.TryGetValue(ID, out IRangeTarget value)) {
				throw new ArgumentOutOfRangeException(nameof(ID),"RangeTarget with this ID does not exist in the current level");
			}
			return value;
		}

		/// <summary>
		/// Registers <paramref name="rangeTarget"/> to rangeTargets, assigns it a new ID and returns this new ID
		/// 
		/// Called by rangeTarget constructor
		/// </summary>
		/// <param name="rangeTarget">range target to register</param>
		/// <returns>the new ID</returns>
		public int RegisterRangeTarget(IRangeTarget rangeTarget) {
			int newID = GetNewID(rangeTargets);
			rangeTarget.InstanceID = newID;
			rangeTargets.Add(rangeTarget.InstanceID, rangeTarget);
			return newID;
		}

		internal void LoadRangeTarget(IRangeTarget rangeTarget)
		{
			rangeTargets.Add(rangeTarget.InstanceID, rangeTarget);
		}

		public bool UnRegisterRangeTarget(int ID) {
			return rangeTargets.Remove(ID);
		}

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

		public void Pause()
		{
			Scene.UpdateEnabled = false;
			LevelNode.SetEnabledRecursive(false);
		}

		public void UnPause()
		{
			Scene.UpdateEnabled = true;
			LevelNode.SetEnabledRecursive(true);
		}

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);

			if (!EnabledEffective) return;

			Plugin.OnUpdate(timeStep);

			Minimap.OnUpdate(timeStep);

			Update?.Invoke(timeStep);
		}

	
		const int MaxTries = 10000000;
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

		void RegisterEntity(IEntity entity)
		{
			entities.Add(entity.ID, entity);
			nodeToEntity.Add(entity.Node, entity);
		}

		bool RemoveEntity(IEntity entity)
		{
			bool removed = entities.Remove(entity.ID);
			return nodeToEntity.Remove(entity.Node) && removed;
		}

	}
}
   

