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

		public DefaultComponentFactory DefaultComponentFactory { get; private set; }

		public PackageManager PackageManager => PackageManager.Instance;

		public event OnUpdateDelegate Update;

		public IEnumerable<Unit> Units => units.Values;

		public IEnumerable<Player> Players => players.Values;

		public IEnumerable<Building> Buildings => buildings.Values;

		private CameraController cameraController;
		private IGameController inputController;

		private readonly Dictionary<int, Unit> units;
		private readonly Dictionary<int, Player> players;
		private readonly Dictionary<int, Building> buildings;
		private readonly Dictionary<int, Projectile> projectiles;

		private readonly Dictionary<int, Entity> entities;

		private readonly Dictionary<int, IRangeTarget> rangeTargets;

		private readonly Random rng;

		protected LevelManager(CameraController cameraController) {
			this.units = new Dictionary<int, Unit>();
			this.players = new Dictionary<int, Player>();
			this.buildings = new Dictionary<int, Building>();
			this.projectiles = new Dictionary<int, Projectile>();
			this.entities = new Dictionary<int, Entity>();
			this.rangeTargets = new Dictionary<int, IRangeTarget>();
			this.rng = new Random();

			this.cameraController = cameraController;
			this.DefaultComponentFactory = new DefaultComponentFactory();

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
				loaders.Add(unitLoader);
			}

			foreach (var building in storedLevel.Buildings) {
				var buildingLoader =
					Building.Loader.StartLoading(level, 
												 PackageManager.Instance, 
												 scene.CreateChild("BuildingNode"),
												 building);

				level.buildings.Add(buildingLoader.Building.ID, buildingLoader.Building);
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

			//TODO: Temporary player
			var player = new Player(CurrentLevel, CurrentLevel.GetNewID(CurrentLevel.players));
			CurrentLevel.players.Add(player.ID, player);
			CurrentLevel.inputController = game.menuController.GetGameController(cameraController, CurrentLevel, player);

			return CurrentLevel;
		}

		public StLevel Save() {
			StLevel level = new StLevel() {
				GameSpeed = this.GameSpeed,
				Map = this.Map.Save(),
				PackageName = PackageManager.Instance.ActiveGame.Name
			};


			var stUnits = level.Units;
			foreach (var unit in units) {
				stUnits.Add(unit.Value.Save());
			}

			var stPlayers = level.Players;
			foreach (var player in players.Values) {
				stPlayers.Add(player.Save());
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
			Scene.RemoveAllChildren();
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
		public Unit SpawnUnit(UnitType unitType, ITile tile, IPlayer player) {

			if (!unitType.CanSpawnAt(tile)) {
				return null;
			}

			Node unitNode = Scene.CreateChild("Unit");

			var newUnit = unitType.CreateNewUnit(GetNewID(entities),unitNode, this, tile, player);
			entities.Add(newUnit.ID, newUnit);
			units.Add(newUnit.ID,newUnit);
			players[player.ID].AddUnit(newUnit);
			tile.AddPassingUnit(newUnit);

			return newUnit;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buildingType"></param>
		/// <param name="topLeft"></param>
		/// <param name="player"></param>
		/// <returns>The new building if building was built, or null if the building could not be built</returns>
		public Building BuildBuilding(BuildingType buildingType, IntVector2 topLeft, IPlayer player) {
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

		public Projectile SpawnProjectile(ProjectileType projectileType, Vector3 position, IPlayer player, IRangeTarget target) {

			var newProjectile = projectileType.ShootProjectile(GetNewID(entities), this, player, position, target);
			entities.Add(newProjectile.ID, newProjectile);
			projectiles.Add(newProjectile.ID, newProjectile);

			return newProjectile;
		}

		public Projectile SpawnProjectile(ProjectileType projectileType, Vector3 position, IPlayer player, Vector3 movement) {

			var newProjectile = projectileType.ShootProjectile(GetNewID(entities), this, player, position, movement);
			entities.Add(newProjectile.ID, newProjectile);
			projectiles.Add(newProjectile.ID, newProjectile);

			return newProjectile;

		}
	

		public Unit GetUnit(int ID) {
			if (!units.TryGetValue(ID, out Unit value)) {
				throw new ArgumentOutOfRangeException("Unit with this ID does not exist in the current level");
			}
			return value;
		}

		public Building GetBuilding(int ID) {
			if (!buildings.TryGetValue(ID, out Building value)) {
				throw new ArgumentOutOfRangeException("Building with this ID does not exist in the current level");
			}
			return value;
		}

		public Player GetPlayer(int ID) {
			if (!players.TryGetValue(ID, out Player player)) {
				throw new ArgumentOutOfRangeException("Player with this ID does not exist in the current level");
			}

			return player;
		}

		public Projectile GetProjectile(int ID) {
			if (!projectiles.TryGetValue(ID, out Projectile value)) {
				throw new ArgumentOutOfRangeException("Projectile with this ID does not exist in the current level");
			}
			return value;
		}

		public Entity GetEntity(int ID) {
			if (!entities.TryGetValue(ID, out Entity value)) {
				throw new ArgumentOutOfRangeException("Entity with this ID does not exist in the current level");
			}
			return value;
		}

		public IRangeTarget GetRangeTarget(int ID) {
			if (!rangeTargets.TryGetValue(ID, out IRangeTarget value)) {
				throw new ArgumentOutOfRangeException("RangeTarget with this ID does not exist in the current level");
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

		public bool UnRegisterRangeTarget(int ID) {
			return rangeTargets.Remove(ID);
		}

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);

			Update?.Invoke(timeStep);
		}

		private static async void LoadSceneParts(MyGame game, Scene scene) {
			// Box	
			Node boxNode = scene.CreateChild(name: "Box node");
			boxNode.Position = new Vector3(x: 0, y: 0, z: 5);
			boxNode.SetScale(0f);
			boxNode.Rotation = new Quaternion(x: 60, y: 0, z: 30);

			StaticModel boxModel = boxNode.CreateComponent<StaticModel>();
			boxModel.Model = game.ResourceCache.GetModel("Models/Box.mdl");
			boxModel.SetMaterial(game.ResourceCache.GetMaterial("Materials/BoxMaterial.xml"));
			boxModel.CastShadows = true;

			// Light
			Node lightNode = scene.CreateChild(name: "light");
			//lightNode.Position = new Vector3(0, 5, 0);
			lightNode.Rotation = new Quaternion(45, 0, 0);
			var light = lightNode.CreateComponent<Light>();
			light.LightType = LightType.Directional;
			//light.Range = 10;
			light.Brightness = 0.5f;
			light.CastShadows = true;
			light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
			light.ShadowCascade = new CascadeParameters(20.0f, 0f, 0f, 0.0f, 0.8f);

			// Ambient light
			var zoneNode = scene.CreateChild("Zone");
			var zone = zoneNode.CreateComponent<Zone>();

			zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
			zone.AmbientColor = new Color(0.5f, 0.5f, 0.5f);
			zone.FogColor = new Color(0.7f, 0.7f, 0.7f);
			zone.FogStart = 50;
			zone.FogEnd = 100;

			//TODO: Remove this
			await boxNode.RunActionsAsync(new EaseBounceOut(new ScaleTo(duration: 1f, scale: 1)));
			await boxNode.RunActionsAsync(new RepeatForever(
				new RotateBy(duration: 1, deltaAngleX: 90, deltaAngleY: 0, deltaAngleZ: 0)));

		}

		private static CameraController LoadCamera(MyGame game, Scene scene) {
			// Camera

			CameraController cameraController = CameraController.GetCameraController(scene);

			// Viewport
			var viewport = new Viewport(game.Context, scene, cameraController.Camera, null);
			viewport.SetClearColor(Color.White);
			game.Renderer.SetViewport(0, viewport);

			return cameraController;
		}

		private const int MaxTries = 10000000;
		private int GetNewID<T>(IDictionary<int, T> dictionary) {
			int id, i = 0;
			while (dictionary.ContainsKey(id = rng.Next())) {
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
   

