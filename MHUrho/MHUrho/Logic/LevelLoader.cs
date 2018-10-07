using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MHUrho.EntityInfo;
using MHUrho.Input;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;
using Google.Protobuf;
using MHUrho.Plugins;
using MHUrho.UserInterface;

namespace MHUrho.Logic
{
	partial class LevelManager {
		class Loader : ILevelLoader {

			abstract class CommonLevelLoader {

				protected readonly Loader Loader;

				protected MyGame Game => MyGame.Instance;
				protected LoadingWatcher LoadingWatcher => Loader.loadingWatcher;

				protected readonly bool EditorMode;

				protected readonly LevelRep LevelRep;

				protected LevelManager Level;

				protected CommonLevelLoader(Loader loader, LevelRep levelRep, bool editorMode)
				{
					this.Loader = loader;
					this.LevelRep = levelRep;
					this.EditorMode = editorMode;
				}

				public abstract Task<ILevelManager> StartLoading();

				protected LevelManager InitializeLevel()
				{
					LoadingWatcher.TextUpdate("Initializing level");
					var scene = new Scene(Game.Context) {UpdateEnabled = false};
					var octree = scene.CreateComponent<Octree>();
					var physics = scene.CreateComponent<PhysicsWorld>();
					//TODO: Test if i can just use it to manually call UpdateCollisions with all rigidBodies kinematic
					physics.Enabled = true;

					LoadSceneParts(scene);

					var levelNode = scene.CreateChild("LevelNode");
					levelNode.Enabled = false;
					CurrentLevel = new LevelManager(levelNode, LevelRep, Game, octree, EditorMode);
					levelNode.AddComponent(CurrentLevel);

					CurrentLevel.Plugin = GetPlugin(CurrentLevel);

					return CurrentLevel;
				}

				protected abstract LevelLogicInstancePlugin GetPlugin(LevelManager level);

				void LoadSceneParts(Scene scene)
				{

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
							zone.FogStart = Game.Config.TerrainDrawDistance / 2;
							zone.FogEnd = Game.Config.TerrainDrawDistance;
						}
					}



				}

				protected void LoadCamera(LevelManager level, Vector2 cameraPosition)
				{
					// Camera
					CameraMover cameraMover = CameraMover.GetCameraController(level.Scene, level.Map, cameraPosition);

					// Viewport
					var viewport = new Viewport(Game.Context, level.Scene, cameraMover.Camera, null);
					viewport.SetClearColor(Color.White);
					Game.Renderer.SetViewport(0, viewport);

					level.Camera = cameraMover;
				}

				protected Player CreatePlaceholderPlayer()
				{
					var newPlayer = Player.CreatePlaceholderPlayer(Level.GetNewID(Level.players),
																	Level,
																	PlayerInsignia.Insignias[Level.players.Count]);
					Level.LevelNode.AddComponent(newPlayer);
					Level.players.Add(newPlayer.ID, newPlayer);

					return newPlayer;
				}

				/// <summary>
				/// Registers all players from <see cref="Level.Players"/> to <see cref="UIManager"/>
				/// by calling <see cref="UIManager.AddPlayer"/>
				///
				/// Selects the player who currently has the input (<see cref="Player"/>)
				/// </summary>
				protected void RegisterPlayersToUI()
				{
					foreach (var player in Level.Players) {
						Level.UIManager.AddPlayer(player);
					}
					Level.UIManager.SelectPlayer(Level.Input.Player);
				}
			}

			class DefaultLevelLoader : CommonLevelLoader {

				public DefaultLevelLoader(Loader loader, LevelRep levelRep, IntVector2 mapSize)
					:base(loader, levelRep, true)
				{
					this.mapSize = mapSize;

					if (LevelRep.MaxNumberOfPlayers < 1) {
						throw new ArgumentException("Level without players does not make sense", nameof(levelRep));
					}

					if (mapSize.X < Map.MinSize.X || Map.MaxSize.X < mapSize.X || 
						mapSize.Y < Map.MinSize.Y || Map.MaxSize.Y < mapSize.Y ) {
						throw new ArgumentOutOfRangeException(nameof(mapSize),
															mapSize,
															"MapSize was out of bounds set by Map.MinSize and Map.MaxSize");
					}

					if (mapSize.X % Map.ChunkSize.X != 0 || mapSize.Y % Map.ChunkSize.Y != 0) {
						throw new ArgumentException("MapSize has to be an integer multiple of Map.ChunkSize", nameof(mapSize));
					}
				}

				readonly IntVector2 mapSize;

				public override async Task<ILevelManager> StartLoading()
				{
					Urho.IO.Log.Write(LogLevel.Debug,
									$"Loading default level. MapSize: {mapSize}, LevelName: {LevelRep.Name}, GamePack: {LevelRep.GamePack}");
					LoadingSanityCheck();

					LoadingWatcher.TextUpdate("Initializing level");
					Level = await MyGame.InvokeOnMainSafeAsync<LevelManager>(InitializeLevel);

					PlayerInsignia.InitInsignias(PackageManager.Instance);

					LoadingWatcher.TextUpdate("Loading map");
					Level.Map = await MyGame.InvokeOnMainSafe(CreateDefaultMap);

					Level.Minimap = new Minimap(Level, 4);

					MyGame.InvokeOnMainSafe(CreateCamera);

					LoadingWatcher.TextUpdate("Creating players");
					MyGame.InvokeOnMainSafe(CreatePlayers);

					LoadingWatcher.TextUpdate("Giving player controls");
					MyGame.InvokeOnMainSafe(CreateControl);

					LoadingWatcher.TextUpdate("Starting level");
					MyGame.InvokeOnMainSafe(StartLevel);

					LoadingWatcher.FinishedLoading();
					return Level;
				}

				protected override LevelLogicInstancePlugin GetPlugin(LevelManager level)
				{
					return LevelRep.LevelLogicType.CreateInstancePluginForBrandNewLevel(level);
				}

				void CreateCamera()
				{
					LoadCamera(Level, new Vector2(10, 10));
				}

				void CreatePlayers()
				{
					Player firstPlayer = CreatePlaceholderPlayer();
					//First player gets the input and is the first one selected
					Level.Input =
						Game.ControllerFactory.CreateGameController(Level.Camera, Level, Level.Scene.GetComponent<Octree>(), firstPlayer);

					//TODO: Give him his insignias
					//Neutral player placeholder
					Level.NeutralPlayer = CreatePlaceholderPlayer();

					//AI player placeholders
					for (int i = 1; i < LevelRep.MaxNumberOfPlayers; i++)
					{
						CreatePlaceholderPlayer();
					}

					RegisterPlayersToUI();
				}

				void CreateControl()
				{
					Level.cameraController = Game.ControllerFactory.CreateCameraController(Level.Input, Level.Camera);
					Level.ToolManager = Level.Plugin.GetToolManager(Level, Level.Input.InputType);
					Level.ToolManager.LoadTools();
				}

				void StartLevel()
				{
					Level.UIManager.ShowUI();
					Level.Scene.UpdateEnabled = true;
					Level.LevelNode.Enabled = true;
				}

				Task<Map> CreateDefaultMap()
				{
					Node mapNode = Level.LevelNode.CreateChild("MapNode");

					//This will take a long time, run it in another thread
					return Task.Run<Map>(() => Map.CreateDefaultMap(CurrentLevel, mapNode, Level.octree, mapSize, LoadingWatcher.GetWatcherForSubsection(30)));
				}

				void LoadingSanityCheck()
				{
				
				}
			}

			abstract class SavedLevelLoader : CommonLevelLoader {

				protected SavedLevelLoader(Loader loader, LevelRep levelRep, StLevel storedLevel, bool editorMode)
					:base(loader, levelRep, editorMode)
				{
					this.StoredLevel = storedLevel;
				}

				protected readonly StLevel StoredLevel;
				protected string PackageName => StoredLevel.PackageName;

				protected List<ILoader> Loaders;

				public override async Task<ILevelManager> StartLoading()
				{
					Urho.IO.Log.Write(LogLevel.Debug,
									$"Loading stored level. LevelName: {LevelRep.Name}, GamePack: {LevelRep.GamePack}, EditorMode: {EditorMode}");

					Loaders = new List<ILoader>();
					LoadingWatcher.TextUpdate("Initializing level");
					Level = await MyGame.InvokeOnMainSafeAsync<LevelManager>(InitializeLevel);

					PlayerInsignia.InitInsignias(PackageManager.Instance);


					var mapLoader = await LoadMap();
					Loaders.Add(mapLoader);
					Level.Map = mapLoader.Map;
					Level.Minimap = new Minimap(Level, 4);


					MyGame.InvokeOnMainSafe(CreateCamera);

					//TODO: Maybe give each its own subsection watcher
					//TODO: Add percentage updates
					MyGame.InvokeOnMainSafe(LoadUnits);
					MyGame.InvokeOnMainSafe(LoadBuildings);
					MyGame.InvokeOnMainSafe(LoadProjectiles);
					MyGame.InvokeOnMainSafe(LoadPlayers);
					MyGame.InvokeOnMainSafe(LoadToolsAndControllers);
					MyGame.InvokeOnMainSafe(LoadLevelPlugin);
					MyGame.InvokeOnMainSafe(ConnectReferences);
					MyGame.InvokeOnMainSafe(FinishLoading);
					MyGame.InvokeOnMainSafe(StartLevel);

					LoadingWatcher.FinishedLoading();
					return Level;				
				}

				protected virtual void LoadUnits()
				{
					LoadingWatcher.TextUpdate("Loading units");
					foreach (var unit in StoredLevel.Units)
					{
						var unitLoader = Unit.GetLoader(Level, Level.LevelNode.CreateChild("UnitNode"), unit);
						unitLoader.StartLoading();
						Level.RegisterEntity(unitLoader.Unit);
						Level.units.Add(unitLoader.Unit.ID, unitLoader.Unit);
						Loaders.Add(unitLoader);
					}
				}

				protected virtual void LoadBuildings()
				{
					LoadingWatcher.TextUpdate("Loading buildings");
					foreach (var building in StoredLevel.Buildings)
					{
						var buildingLoader =
							Building.GetLoader(Level,
												Level.LevelNode.CreateChild("BuildingNode"),
												building);
						buildingLoader.StartLoading();
						Level.RegisterEntity(buildingLoader.Building);
						Level.buildings.Add(buildingLoader.Building.ID, buildingLoader.Building); ;
						Loaders.Add(buildingLoader);

					}
				}

				protected virtual void LoadProjectiles()
				{
					LoadingWatcher.TextUpdate("Loading projectiles");
					foreach (var projectile in StoredLevel.Projectiles)
					{
						var projectileLoader = Projectile.GetLoader(Level,
																	Level.LevelNode.CreateChild("ProjectileNode"),
																	projectile);
						projectileLoader.StartLoading();
						Level.RegisterEntity(projectileLoader.Projectile);
						Level.projectiles.Add(projectileLoader.Projectile.ID, projectileLoader.Projectile);
						Loaders.Add(projectileLoader);
					}
				}

				protected abstract void LoadPlayers();

				protected virtual void LoadToolsAndControllers()
				{
					Level.cameraController = Game.ControllerFactory.CreateCameraController(Level.Input, Level.Camera);
					Level.ToolManager = Level.Plugin.GetToolManager(Level, Level.Input.InputType);
					Level.ToolManager.LoadTools();
				}

				protected virtual void LoadLevelPlugin()
				{
					Level.Plugin.LoadState(new PluginDataWrapper(StoredLevel.Plugin.Data, Level));
				}

				protected virtual void ConnectReferences()
				{
					LoadingWatcher.TextUpdate("Connecting references");
					//Connect references
					foreach (var loader in Loaders)
					{
						loader.ConnectReferences();
					}
				}

				protected virtual void FinishLoading()
				{
					LoadingWatcher.TextUpdate("Finishing loading");
					foreach (var loader in Loaders)
					{
						loader.FinishLoading();
					}
				}

				protected virtual void StartLevel()
				{
					LoadingWatcher.TextUpdate("Starting level");

					CurrentLevel = Level;
					Level.UIManager.ShowUI();
					Level.Scene.UpdateEnabled = true;
					Level.LevelNode.Enabled = true;
				}

				protected void LoadPlayer(IPlayerLoader playerLoader)
				{
					playerLoader.StartLoading();

					Level.LevelNode.AddComponent(playerLoader.Player);
					Level.players.Add(playerLoader.Player.ID, playerLoader.Player);
					Loaders.Add(playerLoader);
				}

				void CreateCamera()
				{
					//TODO:Maybe Save camera position
					LoadCamera(Level, new Vector2(10, 10));
				}

				Task<IMapLoader> LoadMap()
				{
					Node mapNode = Level.LevelNode.CreateChild("MapNode");
					return Task.Run(() => {
						var loader = Map.GetLoader(Level,
													mapNode,
													Level.octree,
													StoredLevel.Map,
													LoadingWatcher.GetWatcherForSubsection(30));
						loader.StartLoading();
						return loader;
									});
				}

				
			}

			class SavedLevelPlayingLoader : SavedLevelLoader {

				readonly PlayerSpecification players;
				readonly LevelLogicCustomSettings customSettings;

				public SavedLevelPlayingLoader(Loader loader,
												LevelRep levelRep,
												StLevel storedLevel,
												PlayerSpecification players,
												LevelLogicCustomSettings customSettings)
					: base(loader, levelRep, storedLevel, false)
				{
					this.players = players;
					this.customSettings = customSettings;

					if ((players == PlayerSpecification.LoadFromSavedGame) !=
						(customSettings == LevelLogicCustomSettings.LoadFromSavedGame)) {
						throw new
							ArgumentException("Argument mismatch, one argument is loaded from save and the other is not");
					}
				}

				protected override LevelLogicInstancePlugin GetPlugin(LevelManager level)
				{
					if (customSettings == LevelLogicCustomSettings.LoadFromSavedGame) {
						//Loading saved game in play, no new custom settings, just load it
						return LevelRep.LevelLogicType.CreateInstancePluginForLoadingToPlaying(level);
					}
					else {
						//Loading saved level prototype, so i need to load it with custom settings
						return LevelRep.LevelLogicType.CreateInstancePluginForNewPlaying(customSettings, level);
					}

				}

				protected override void LoadPlayers()
				{
					LoadingWatcher.TextUpdate("Loading players");
					//If players == null, we are loading a saved level already in play with set AIs
					IPlayer playerWithInput = null;
					if (players == PlayerSpecification.LoadFromSavedGame) {
						foreach (var storedPlayer in StoredLevel.Players.Players)
						{
							LoadPlayer(Player.GetLoaderStoredType(Level, storedPlayer));
						}

						playerWithInput = Level.GetPlayer(StoredLevel.Players.PlayerWithInputID);
						Level.NeutralPlayer = Level.GetPlayer(StoredLevel.Players.NeutralPlayerID);
					}
					//We are loading new level from a prototype or a level in play with new AIs
					else {
						var storedPlayer = StoredLevel.Players.Players.GetEnumerator();
						var newPlayerInfo = players.GetEnumerator();
						for (; storedPlayer.MoveNext() && newPlayerInfo.MoveNext();) {
							IPlayerLoader newPlayer =
								Player.GetLoaderFillType(Level, storedPlayer.Current, newPlayerInfo.Current, false);
							LoadPlayer(newPlayer);

							if (newPlayerInfo.Current.HasInput && newPlayerInfo.Current.IsNeutral)
							{
								throw new
									ArgumentException("Corrupted save file, neutral player cannot have the input");
							}
							else if (newPlayerInfo.Current.IsNeutral) {
								Level.NeutralPlayer = newPlayer.Player;
							}
							else if (newPlayerInfo.Current.HasInput) {
								playerWithInput = newPlayer.Player;
							}
						}

						storedPlayer.Dispose();
						newPlayerInfo.Dispose();
					}

					if (playerWithInput == null) {
						throw new
							ArgumentException("Corrupted save file, no player has input");
					}

					Level.Input = Game.ControllerFactory.CreateGameController(Level.Camera, Level, Level.octree, playerWithInput);
					RegisterPlayersToUI();


					//Player selection is disabled by default in play mode, but it can be enabled again
					Level.UIManager.DisablePlayerSelection();
				}

				protected override void FinishLoading()
				{
					customSettings.Dispose();
					base.FinishLoading();
				}
			}

			class SavedLevelEditorLoader : SavedLevelLoader {
				public SavedLevelEditorLoader(Loader loader, LevelRep levelRep, StLevel storedLevel)
					: base(loader, levelRep, storedLevel, true)
				{ }

				protected override LevelLogicInstancePlugin GetPlugin(LevelManager level)
				{
					//Loading saved level prototype, so i need to load it
					return LevelRep.LevelLogicType.CreateInstancePluginForEditorLoading(level);
				}

				protected override void LoadPlayers()
				{				
					int numberOfPlayers = 0;

					foreach (var existingPlayer in StoredLevel.Players.Players) {
						numberOfPlayers++;
						IPlayerLoader newPlayer = Player.GetLoaderToEditor(Level, existingPlayer);
						LoadPlayer(newPlayer);

						//While editing, even the neutral player can have input
						//When starting play, user will set who has input manually anyway
						if (newPlayer.Player.ID == StoredLevel.Players.NeutralPlayerID) {
							Level.NeutralPlayer = newPlayer.Player;
						}
						else if (newPlayer.Player.ID == StoredLevel.Players.PlayerWithInputID) {
							Level.Input = Game.ControllerFactory.CreateGameController(Level.Camera, Level, Level.octree, newPlayer.Player);
						}
					}

					//There will always be the neutral player and the player with input (representing the human behind the keyboard)
					if (numberOfPlayers < 2) {
						throw new
							ArgumentException("Corrupted save file, there was less than 2 players (neutral or human missing)");
					}

					//Some player always has to have the input
					if (Level.Input == null) {
						throw new ArgumentException("Corrupted save file, no player had input");
					}

					for (; numberOfPlayers < LevelRep.MaxNumberOfPlayers; numberOfPlayers++) {
						CreatePlaceholderPlayer();
					}


					RegisterPlayersToUI();
				}
			}

			public ILoadingWatcher LoadingWatcher => loadingWatcher;

			public Task<ILevelManager> CurrentLoading { get; private set; }

			readonly LoadingWatcher loadingWatcher;



			CommonLevelLoader loaderType;

			public Loader()
			{
				this.CurrentLoading = null;
				loadingWatcher = new LoadingWatcher();
			}

			public Task<ILevelManager> LoadForEditing(LevelRep levelRep, StLevel storedLevel)
			{
				loaderType = new SavedLevelEditorLoader(this, levelRep, storedLevel);

				CurrentLoading = loaderType.StartLoading();
				return CurrentLoading;
			}

			public Task<ILevelManager> LoadForPlaying(LevelRep levelRep,
													StLevel storedLevel,
													PlayerSpecification players,
													LevelLogicCustomSettings customSettings)
			{
				loaderType = new SavedLevelPlayingLoader(this, levelRep, storedLevel, players, customSettings);

				CurrentLoading = loaderType.StartLoading();
				return CurrentLoading;
			}

			/// <summary>
			/// Loads default level to use in level builder as basis, loads specified packages plus default package
			/// </summary>
			/// <param name="mapSize">Size of the map to create</param>
			/// <param name="packages">packages to load</param>
			/// <returns>Loaded default level</returns>
			public Task<ILevelManager> LoadDefaultLevel(LevelRep levelRep, IntVector2 mapSize)
			{
				var newLoaderType = new DefaultLevelLoader(this, levelRep, mapSize);
				loaderType = newLoaderType;

				CurrentLoading = newLoaderType.StartLoading();
				return CurrentLoading;
			}

		
		}
	}
    
}
