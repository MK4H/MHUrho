using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
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
		/// <summary>
		/// Currently running level, CANNOT BE USED DURING LOADING
		/// </summary>
		public static LevelManager CurrentLevel { get; private set; }

		public float GameSpeed { get; set; } = 1f;

		public Map Map { get; private set; }

		public Minimap Minimap { get; private set; }

		public DefaultComponentFactory DefaultComponentFactory { get; private set; }

		public PackageManager PackageManager => PackageManager.Instance;

		public event OnUpdateDelegate Update;

		public IEnumerable<IUnit> Units => units.Values;

		public IEnumerable<IPlayer> Players => players.Values;

		public IEnumerable<IBuilding> Buildings => buildings.Values;

		CameraController cameraController;
		IGameController inputController;

		int minimapRefreshRate;
		float minimapRefreshDelay;

		readonly Dictionary<int, IUnit> units;
		readonly Dictionary<int, IPlayer> players;
		readonly Dictionary<int, IBuilding> buildings;
		readonly Dictionary<int, IProjectile> projectiles;

		readonly Dictionary<int, IEntity> entities;

		readonly Dictionary<int, IRangeTarget> rangeTargets;

		readonly Random rng;

		protected LevelManager(CameraController cameraController) {
			this.units = new Dictionary<int, IUnit>();
			this.players = new Dictionary<int, IPlayer>();
			this.buildings = new Dictionary<int, IBuilding>();
			this.projectiles = new Dictionary<int, IProjectile>();
			this.entities = new Dictionary<int, IEntity>();
			this.rangeTargets = new Dictionary<int, IRangeTarget>();
			this.rng = new Random();

			this.cameraController = cameraController;
			this.DefaultComponentFactory = new DefaultComponentFactory();
			ReceiveSceneUpdates = true;
		}

		public static LevelManager Load(MyGame game, StLevel storedLevel) {

			var scene = new Scene(game.Context);
			scene.CreateComponent<Octree>();

			LoadSceneParts(game, scene);
			var cameraController = LoadCamera(game, scene);



			LevelManager level = new LevelManager(cameraController);
			scene.AddComponent(level);

			PackageManager.Instance.LoadPackage(storedLevel.PackageName);

			List<ILoader> loaders = new List<ILoader>();

			//Load data
			Node mapNode = scene.CreateChild("MapNode");
			var mapLoader = Map.Loader.StartLoading(level, mapNode, storedLevel.Map);
			loaders.Add(mapLoader);
			level.Map = mapLoader.Map;

			foreach (var unit in storedLevel.Units) {
				var unitLoader = Unit.Loader.StartLoading(level, PackageManager.Instance, scene.CreateChild("UnitNode"), unit);
				level.units.Add(unitLoader.Unit.ID, unitLoader.Unit);
				level.entities.Add(unitLoader.Unit.ID, unitLoader.Unit);
				loaders.Add(unitLoader);
			}

			foreach (var building in storedLevel.Buildings) {
				var buildingLoader =
					Building.Loader.StartLoading(level, 
												 PackageManager.Instance, 
												 scene.CreateChild("BuildingNode"),
												 building);

				level.buildings.Add(buildingLoader.Building.ID, buildingLoader.Building);
				level.entities.Add(buildingLoader.Building.ID, buildingLoader.Building);
				loaders.Add(buildingLoader);

			}


			//TODO: Remove this
			Player firstPlayer = null;

			foreach (var player in storedLevel.Players) {
				var playerLoader = Player.Loader.StartLoading(level, player);
				//TODO: If player needs controller, give him
				if (firstPlayer == null) {
					firstPlayer = playerLoader.Player;
				}

				level.players.Add(playerLoader.Player.ID, playerLoader.Player);
				loaders.Add(playerLoader);
			}
			//TODO: Move this inside the foreach
			level.inputController = game.menuController.GetGameController(cameraController, level, firstPlayer);
			
			//Connect references
			foreach (var loader in loaders) {
				loader.ConnectReferences(level);
			}


			//Build geometry and other things

			foreach (var loader in loaders) {
				loader.FinishLoading();
			}


			CurrentLevel = level;
			return level;
		}

		public static LevelManager LoadFrom(MyGame game, Stream stream, bool leaveOpen = false) {
			var storedLevel = StLevel.Parser.ParseFrom(stream);
			var level = Load(game, storedLevel);
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
		public static LevelManager LoadDefaultLevel(MyGame game, IntVector2 mapSize, string gamePackageName) {
			PackageManager.Instance.LoadPackage(gamePackageName);
			
			var scene = new Scene(game.Context);
			scene.CreateComponent<Octree>();
			var physics = scene.CreateComponent<PhysicsWorld>();
			//TODO: Test if i can just use it to manually call UpdateCollisions with all rigidBodies kinematic
			physics.Enabled = true;
			
			LoadSceneParts(game, scene);
			var cameraController = LoadCamera(game, scene);

			CurrentLevel = new LevelManager(cameraController);
			scene.AddComponent(CurrentLevel);

			Node mapNode = scene.CreateChild("MapNode");

			Map map = Map.CreateDefaultMap(CurrentLevel, mapNode, mapSize);
			CurrentLevel.Map = map;

			CurrentLevel.Minimap = new Minimap(map);
			CurrentLevel.minimapRefreshRate = 4;
			CurrentLevel.minimapRefreshDelay = 1.0f / CurrentLevel.minimapRefreshRate; 

			//TODO: Temporary player
			var player = Player.CreateNewHumanPlayer(CurrentLevel.GetNewID(CurrentLevel.players), CurrentLevel, scene);
			CurrentLevel.players.Add(player.ID, player);
			CurrentLevel.inputController = game.menuController.GetGameController(cameraController, CurrentLevel, player);

			CurrentLevel.inputController.UIManager.AddPlayer(player);

			player = Player.CreateNewAIPlayer(CurrentLevel.GetNewID(CurrentLevel.players), CurrentLevel, scene, PackageManager.Instance.ActiveGame.GetPlayerAIType("TestAI"));
			CurrentLevel.players.Add(player.ID, player);
			CurrentLevel.inputController.UIManager.AddPlayer(player);

			return CurrentLevel;
		}

		public StLevel Save() {
			StLevel level = new StLevel() {
				GameSpeed = this.GameSpeed,
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

		public void End() {
			inputController.Dispose();
			inputController = null;
			Map.Dispose();
			Minimap.Dispose();
			Scene.Dispose();
			
			CurrentLevel = null;
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

			Node unitNode = Scene.CreateChild("Unit");

			var newUnit = unitType.CreateNewUnit(GetNewID(entities),unitNode, this, tile, player);
			entities.Add(newUnit.ID, newUnit);
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

			Node buildingNode = Scene.CreateChild("Building");

			var newBuilding = buildingType.BuildNewBuilding(GetNewID(entities), buildingNode, this, topLeft, player);
			entities.Add(newBuilding.ID, newBuilding);
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
				entities.Add(newProjectile.ID, newProjectile);
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
				entities.Add(newProjectile.ID, newProjectile);
				projectiles.Add(newProjectile.ID, newProjectile);
			}

			return newProjectile;

		}


		public bool RemoveUnit(IUnit unit)
		{
			bool removed = units.Remove(unit.ID) && entities.Remove(unit.ID);

			if (!unit.RemovedFromLevel) {
				unit.Kill();
			}

			return removed;
		}

		public bool RemoveBuilding(IBuilding building)
		{
			bool removed = buildings.Remove(building.ID) && entities.Remove(building.ID);

			if (!building.RemovedFromLevel) {
				building.Kill();
			}
			return removed;
		}

		public bool RemoveProjectile(IProjectile projectile)
		{
			bool removed = projectiles.Remove(projectile.ID) && entities.Remove(projectile.ID);

			if (!projectile.RemovedFromLevel) {
				projectile.Despawn();
			}
			return removed;
		}

		public IUnit GetUnit(int ID) {
			if (!units.TryGetValue(ID, out IUnit value)) {
				throw new ArgumentOutOfRangeException(nameof(ID), "Unit with this ID does not exist in the current level");
			}
			return value;
		}

		public IBuilding GetBuilding(int ID) {
			if (!buildings.TryGetValue(ID, out IBuilding value)) {
				throw new ArgumentOutOfRangeException(nameof(ID), "Building with this ID does not exist in the current level");
			}
			return value;
		}

		public IPlayer GetPlayer(int ID) {
			if (!players.TryGetValue(ID, out IPlayer player)) {
				throw new ArgumentOutOfRangeException(nameof(ID), "Player with this ID does not exist in the current level");
			}

			return player;
		}

		public IProjectile GetProjectile(int ID) {
			if (!projectiles.TryGetValue(ID, out IProjectile value)) {
				throw new ArgumentOutOfRangeException(nameof(ID), "Projectile with this ID does not exist in the current level");
			}
			return value;
		}

		public IEntity GetEntity(int ID) {
			if (!entities.TryGetValue(ID, out IEntity value)) {
				throw new ArgumentOutOfRangeException(nameof(ID), "Entity with this ID does not exist in the current level");
			}
			return value;
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

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);

			minimapRefreshDelay -= timeStep;

			if (minimapRefreshDelay < 0) {
				minimapRefreshDelay = 1.0f / minimapRefreshRate;
				Minimap.MoveTo(cameraController.CameraXZPosition.RoundToIntVector2());
				Minimap.Refresh();
			}

			Update?.Invoke(timeStep);
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
					zone.FogStart = 50;
					zone.FogEnd = 100;
				}
			}
			

			
		}

		static CameraController LoadCamera(MyGame game, Scene scene) {
			// Camera

			CameraController cameraController = CameraController.GetCameraController(scene);

			// Viewport
			var viewport = new Viewport(game.Context, scene, cameraController.Camera, null);
			viewport.SetClearColor(Color.White);
			game.Renderer.SetViewport(0, viewport);

			return cameraController;
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

	}
}
   

