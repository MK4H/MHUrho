using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MHUrho.Control;
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


namespace MHUrho.Logic
{

	public delegate void OnUpdateDelegate(float timeStep);

	internal class LevelManager : Component, ILevelManager
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

		public MyGame App { get; private set; }

		public Node LevelNode { get; private set; }

		public Map Map { get; private set; }

		public Minimap Minimap { get; private set; }

		public DefaultComponentFactory DefaultComponentFactory { get; private set; }

		public PackageManager PackageManager => PackageManager.Instance;

		public event OnUpdateDelegate Update;

		public IEnumerable<IUnit> Units => units.Values;

		public IEnumerable<IPlayer> Players => players.Values;

		public IEnumerable<IBuilding> Buildings => buildings.Values;

		public IGameController Input { get; protected set; }

		public CameraMover Camera { get; private set; }

		//TODO: Platform independent
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

		protected LevelManager(Node levelNode, MyGame app, Octree octree)
		{
			this.LevelNode = levelNode;

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

		public static async Task<LevelManager> Load(MyGame game, StLevel storedLevel) {

			Scene scene = null;
			Octree octree = null;
			LevelManager level = null;
			List<ILoader> loaders = new List<ILoader>();
			Node mapNode = null;

			MyGame.InvokeOnMainSafe(StartLoadingInMain);


			var mapLoader = await Task.Run(() => Map.Loader.StartLoading(level, mapNode, storedLevel.Map));
			loaders.Add(mapLoader);
			level.Map = mapLoader.Map;

			MyGame.InvokeOnMainSafe(CreateCamera);

			MyGame.InvokeOnMainSafe(LoadEntities);

			return level;

			void StartLoadingInMain()
			{
				PackageManager.Instance.LoadPackage(storedLevel.PackageName);

				scene = new Scene(game.Context);
				scene.UpdateEnabled = false;
				octree = scene.CreateComponent<Octree>();

				LoadSceneParts(game, scene);


				var levelNode = scene.CreateChild("LevelNode");
				levelNode.Enabled = false;
				level = new LevelManager(levelNode, game, octree);
				levelNode.AddComponent(level);

				level.Minimap = new Minimap(level, 4);

				//Load data
				mapNode = level.LevelNode.CreateChild("MapNode");
			}

			void CreateCamera()
			{
				//TODO:Maybe Save camera position
				level.Camera = LoadCamera(game, scene, level.Map, new Vector2(10,10));
			}

			void LoadEntities()
			{
				foreach (var unit in storedLevel.Units) {
					var unitLoader = Unit.Loader.StartLoading(level, PackageManager.Instance, level.LevelNode.CreateChild("UnitNode"), unit);
					level.RegisterEntity(unitLoader.Unit);
					level.units.Add(unitLoader.Unit.ID, unitLoader.Unit);
					loaders.Add(unitLoader);
				}

				foreach (var building in storedLevel.Buildings) {
					var buildingLoader =
						Building.Loader.StartLoading(level,
													PackageManager.Instance,
													level.LevelNode.CreateChild("BuildingNode"),
													building);
					level.RegisterEntity(buildingLoader.Building);
					level.buildings.Add(buildingLoader.Building.ID, buildingLoader.Building);;
					loaders.Add(buildingLoader);

				}

				foreach (var projectile in storedLevel.Projectiles) {
					var projectileLoader = Projectile.Loader.StartLoading(level,
																		level.LevelNode.CreateChild("ProjectileNode"),
																		projectile);

					level.RegisterEntity(projectileLoader.Projectile);
					level.projectiles.Add(projectileLoader.Projectile.ID, projectileLoader.Projectile);
					loaders.Add(projectileLoader);
				}

				//TODO: Remove this
				Player firstPlayer = null;

				foreach (var player in storedLevel.Players) {
					var playerLoader = Player.Loader.StartLoading(level, player);
					//TODO: If player needs controller, give him
					if (firstPlayer == null) {
						firstPlayer = playerLoader.Player;
					}

					level.LevelNode.AddComponent(playerLoader.Player);
					level.players.Add(playerLoader.Player.ID, playerLoader.Player);
					loaders.Add(playerLoader);
				}
				//TODO: Move this inside the foreach
				level.Input = game.ControllerFactory.CreateGameController(level.Camera, level, octree, firstPlayer);
				level.cameraController = game.ControllerFactory.CreateCameraController(level.Input, level.Camera);

				//Connect references
				foreach (var loader in loaders) {
					loader.ConnectReferences(level);
				}

				foreach (var loader in loaders) {
					loader.FinishLoading();
				}

				CurrentLevel = level;
				scene.UpdateEnabled = true;
				level.LevelNode.Enabled = true;
			}
		}

		public static async Task<LevelManager> LoadFrom(MyGame game, Stream stream, bool leaveOpen = false) {
			var storedLevel = await Task.Run<StLevel>(() => StLevel.Parser.ParseFrom(stream));
			LevelManager level = await Load(game, storedLevel);

			if (!leaveOpen) {
				stream.Close();
			}

			return level;
		}

		/// <summary>
		/// Loads default level to use in level builder as basis, loads specified packages plus default package
		/// </summary>
		/// <param name="mapSize">Size of the map to create</param>
		/// <param name="packages">packages to load</param>
		/// <returns>Loaded default level</returns>
		public static async Task<LevelManager> LoadDefaultLevel(MyGame game, IntVector2 mapSize, string gamePackageName)
		{
			InitializeLevel(game, gamePackageName, out Scene scene);

			Node mapNode = CurrentLevel.LevelNode.CreateChild("MapNode");

			Map map = await Task.Run(() => Map.CreateDefaultMap(CurrentLevel, mapNode, mapSize));
			CurrentLevel.Map = map;


			CurrentLevel.Minimap = new Minimap(CurrentLevel, 4);

			MyGame.InvokeOnMainSafe(CreateCamera);

			MyGame.InvokeOnMainSafe(StartLevel);

			return CurrentLevel;

			void CreateCamera()
			{
				CurrentLevel.Camera = LoadCamera(game, scene, map, new Vector2(10,10));
			}

			void StartLevel()
			{
				//TODO: Temporary player, COLOR 
				Player newPlayer = Player.CreateNewHumanPlayer(CurrentLevel.GetNewID(CurrentLevel.players), CurrentLevel, Color.Red);
				CurrentLevel.LevelNode.AddComponent(newPlayer);
				CurrentLevel.players.Add(newPlayer.ID, newPlayer);
				CurrentLevel.Input =
					game.ControllerFactory.CreateGameController(CurrentLevel.Camera, CurrentLevel, scene.GetComponent<Octree>(), newPlayer);

				CurrentLevel.cameraController = game.ControllerFactory.CreateCameraController(CurrentLevel.Input, CurrentLevel.Camera);

				CurrentLevel.Input.UIManager.AddPlayer(newPlayer);

				newPlayer = Player.CreateNewAIPlayer(CurrentLevel.GetNewID(CurrentLevel.players),
													CurrentLevel,
													PackageManager.Instance.ActiveGame.GetPlayerAIType("TestAI"),
													Color.Blue);
				CurrentLevel.LevelNode.AddComponent(newPlayer);
				CurrentLevel.players.Add(newPlayer.ID, newPlayer);
				CurrentLevel.Input.UIManager.AddPlayer(newPlayer);

				scene.UpdateEnabled = true;
				CurrentLevel.LevelNode.Enabled = true;
			}
			
		}

		public StLevel Save() {
			StLevel level = new StLevel() {
				Map = this.Map.Save(),
				PackageName = PackageManager.Instance.ActiveGame.Name
			};



			foreach (var unit in units.Values) {
				level.Units.Add(unit.Save());
			}

			foreach (var building in buildings.Values) {
				level.Buildings.Add(building.Save());
			}

			foreach (var projectile in projectiles.Values) {
				level.Projectiles.Add(projectile.Save());
			}


			foreach (var player in players.Values) {
				level.Players.Add(player.Save());
			}

			return level;
		}

		public void SaveTo(Stream stream, bool leaveOpen = false) {
			var storedLevel = Save();
			storedLevel.WriteTo(new Google.Protobuf.CodedOutputStream(stream, leaveOpen));
		}

		public void End()
		{
			List<IDisposable> toDispose = new List<IDisposable>();
			toDispose.AddRange(entities.Values);

			foreach (var thing in toDispose) {
				thing.Dispose();
			}

			HealthBar.DisposeMaterials();
			Input.Dispose();
			cameraController.Dispose();
			Camera.Dispose();
			Input = null;
			Map.Dispose();
			Minimap.Dispose();
			octree.Dispose();
			//TODO: Temporary
			var scene = Scene;
			scene.RemoveAllChildren();
			scene.RemoveAllComponents();
			scene.Remove();
			scene.Dispose();
			LevelNode.Dispose();
			Dispose();

			CurrentLevel = null;
			GC.Collect();
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

			var newUnit = unitType.CreateNewUnit(GetNewID(entities),unitNode, this, tile, player);
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

			Minimap.OnUpdate(timeStep);

			Update?.Invoke(timeStep);
		}

		static void InitializeLevel(MyGame game, string gamePackageName, out Scene scene)
		{
			PackageManager.Instance.LoadPackage(gamePackageName);

			scene = new Scene(game.Context);
			scene.UpdateEnabled = false;
			var octree = scene.CreateComponent<Octree>();
			var physics = scene.CreateComponent<PhysicsWorld>();
			//TODO: Test if i can just use it to manually call UpdateCollisions with all rigidBodies kinematic
			physics.Enabled = true;

			LoadSceneParts(game, scene);

			var levelNode = scene.CreateChild("LevelNode");
			levelNode.Enabled = false;
			CurrentLevel = new LevelManager(levelNode, game, octree);
			levelNode.AddComponent(CurrentLevel);

		}

		static void LoadSceneParts(MyGame game, Scene scene) {

			// Light
			using (Node lightNode = scene.CreateChild(name: "light")) {
				lightNode.Rotation = new Quaternion(45, 0, 0);
				//lightNode.Position = new Vector3(0, 5, 0);
				using (var light = lightNode.CreateComponent<Light>()) {
					light.LightType = LightType.Directional;
					//light.Range = 10;
					light.Brightness = 0.5f;
					light.CastShadows = true;
					light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
					light.ShadowCascade = new CascadeParameters(20.0f, 0f, 0f, 0.0f, 0.8f);
				}
			}

			// Ambient light
			using (var zoneNode = scene.CreateChild("Zone")) {
				using (var zone = zoneNode.CreateComponent<Zone>()) {
					zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
					zone.AmbientColor = new Color(0.5f, 0.5f, 0.5f);
					zone.FogColor = new Color(0.7f, 0.7f, 0.7f);
					zone.FogStart = game.Config.TerrainDrawDistance / 2;
					zone.FogEnd = game.Config.TerrainDrawDistance;
				}
			}
			

			
		}

		static CameraMover LoadCamera(MyGame game, Scene scene, IMap map, Vector2 cameraPosition) {
			// Camera

			CameraMover cameraMover = CameraMover.GetCameraController(scene, map, cameraPosition);

			// Viewport
			var viewport = new Viewport(game.Context, scene, cameraMover.Camera, null);
			viewport.SetClearColor(Color.White);
			game.Renderer.SetViewport(0, viewport);

			return cameraMover;
		}

		const int MaxTries = 10000000;
		int GetNewID<T>(IDictionary<int, T> dictionary) {
			int id, i = 0;
			while (dictionary.ContainsKey(id = rng.Next()) && id == 0) {
				i++;
				if (i > MaxTries) {
					//TODO: Exception
					throw new Exception("Could not find free ID");
				}
			}

			return id;
		}

		void RegisterEntity(Entity entity)
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
   

