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

					return CurrentLevel;
				}

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
			}

			class DefaultLevelLoader : CommonLevelLoader {

				public DefaultLevelLoader(Loader loader, LevelRep levelRep, IntVector2 mapSize)
					:base(loader, levelRep, true)
				{
					this.mapSize = mapSize;
				}

				readonly IntVector2 mapSize;

				public override async Task<ILevelManager> StartLoading()
				{
					LoadingWatcher.TextUpdate("Initializing level");
					Level = await MyGame.InvokeOnMainSafeAsync<LevelManager>(InitializeLevel);

					PlayerInsignia.InitInsignias(PackageManager.Instance);

					LoadingWatcher.TextUpdate("Loading map");
					Level.Map = await MyGame.InvokeOnMainSafe(CreateDefaultMap);

					Level.Minimap = new Minimap(Level, 4);

					MyGame.InvokeOnMainSafe(CreateCamera);

					LoadingWatcher.TextUpdate("Starting level");
					MyGame.InvokeOnMainSafe(StartLevel);

					LoadingWatcher.FinishedLoading();
					return Level;
				}

				void CreateCamera()
				{
					LoadCamera(Level, new Vector2(10, 10));
				}

				void StartLevel()
				{
					//TODO: Temporary player creation
					Player newPlayer = Player.CreateNewHumanPlayer(Level.GetNewID(Level.players), Level, PlayerInsignia.Insignias[0]);
					Level.LevelNode.AddComponent(newPlayer);
					Level.players.Add(newPlayer.ID, newPlayer);
					Level.Input =
						Game.ControllerFactory.CreateGameController(Level.Camera, Level, Level.Scene.GetComponent<Octree>(), newPlayer);

					Level.cameraController = Game.ControllerFactory.CreateCameraController(Level.Input, Level.Camera);
					Level.ToolManager = Level.Plugin.GetToolManager(Level, Level.Input.InputType);

					Level.Input.UIManager.AddPlayer(newPlayer);
					Level.Input.UIManager.SelectPlayer(newPlayer);

					newPlayer = Player.CreateNewAIPlayer(Level.GetNewID(Level.players),
														Level,
														PackageManager.Instance.ActivePackage.GetPlayerAIType("TestAI"),
														PlayerInsignia.Insignias[1]);
					Level.LevelNode.AddComponent(newPlayer);
					Level.players.Add(newPlayer.ID, newPlayer);
					Level.Input.UIManager.AddPlayer(newPlayer);

					Level.Scene.UpdateEnabled = true;
					Level.LevelNode.Enabled = true;
				}

				Task<Map> CreateDefaultMap()
				{
					Node mapNode = Level.LevelNode.CreateChild("MapNode");

					//This will take a long time, run it in another thread
					return Task.Run<Map>(() => Map.CreateDefaultMap(CurrentLevel, mapNode, Level.octree, mapSize, LoadingWatcher.GetWatcherForSubsection(30)));
				}
			}

			class SavedLevelLoader : CommonLevelLoader {

				public SavedLevelLoader(Loader loader, LevelRep levelRep, StLevel storedLevel, bool editorMode)
					:base(loader, levelRep, editorMode)
				{
					this.storedLevel = storedLevel;
				}

				readonly StLevel storedLevel;
				string PackageName => storedLevel.PackageName;

				List<ILoader> loaders;

				public override async Task<ILevelManager> StartLoading()
				{
					loaders = new List<ILoader>();
					LoadingWatcher.TextUpdate("Initializing level");
					Level = await MyGame.InvokeOnMainSafeAsync<LevelManager>(InitializeLevel);

					PlayerInsignia.InitInsignias(PackageManager.Instance);


					var mapLoader = await LoadMap();
					loaders.Add(mapLoader);
					Level.Map = mapLoader.Map;

					MyGame.InvokeOnMainSafe(CreateCamera);

					MyGame.InvokeOnMainSafe(LoadEntities);

					LoadingWatcher.FinishedLoading();
					return Level;				
				}
				void CreateCamera()
				{
					//TODO:Maybe Save camera position
					LoadCamera(Level, new Vector2(10, 10));
				}

				void LoadEntities()
				{
					//TODO: Maybe give it its own subsection watcher
					//TODO: Add percentage updates
					LoadingWatcher.TextUpdate("Loading units");
					foreach (var unit in storedLevel.Units)
					{
						var unitLoader = Unit.GetLoader(Level, Level.LevelNode.CreateChild("UnitNode"), unit);
						unitLoader.StartLoading();
						Level.RegisterEntity(unitLoader.Unit);
						Level.units.Add(unitLoader.Unit.ID, unitLoader.Unit);
						loaders.Add(unitLoader);
					}

					LoadingWatcher.TextUpdate("Loading buildings");
					foreach (var building in storedLevel.Buildings)
					{
						var buildingLoader =
							Building.GetLoader(Level,
												Level.LevelNode.CreateChild("BuildingNode"),
												building);
						buildingLoader.StartLoading();
						Level.RegisterEntity(buildingLoader.Building);
						Level.buildings.Add(buildingLoader.Building.ID, buildingLoader.Building); ;
						loaders.Add(buildingLoader);

					}

					LoadingWatcher.TextUpdate("Loading projectiles");
					foreach (var projectile in storedLevel.Projectiles)
					{
						var projectileLoader = Projectile.GetLoader(Level,
																	Level.LevelNode.CreateChild("ProjectileNode"),
																	projectile);
						projectileLoader.StartLoading();
						Level.RegisterEntity(projectileLoader.Projectile);
						Level.projectiles.Add(projectileLoader.Projectile.ID, projectileLoader.Projectile);
						loaders.Add(projectileLoader);
					}

					LoadingWatcher.TextUpdate("Loading players");
					//TODO: Remove this
					Player firstPlayer = null;

					foreach (var player in storedLevel.Players)
					{
						var playerLoader = Player.GetLoader(Level, player);
						playerLoader.StartLoading();
						//TODO: If player needs controller, give him
						if (firstPlayer == null)
						{
							firstPlayer = playerLoader.Player;
						}

						Level.LevelNode.AddComponent(playerLoader.Player);
						Level.players.Add(playerLoader.Player.ID, playerLoader.Player);
						loaders.Add(playerLoader);
					}
					//TODO: Move this inside the foreach
					Level.Input = Game.ControllerFactory.CreateGameController(Level.Camera, Level, Level.octree, firstPlayer);
					Level.cameraController = Game.ControllerFactory.CreateCameraController(Level.Input, Level.Camera);
					Level.ToolManager = Level.Plugin.GetToolManager(Level, Level.Input.InputType);

					Level.Plugin.LoadState(new PluginDataWrapper(storedLevel.Plugin.Data, Level));

					LoadingWatcher.TextUpdate("Connecting references");
					//Connect references
					foreach (var loader in loaders)
					{
						loader.ConnectReferences();
					}

					LoadingWatcher.TextUpdate("Finishing loading");
					foreach (var loader in loaders)
					{
						loader.FinishLoading();
					}

					LoadingWatcher.TextUpdate("Starting level");

					CurrentLevel = Level;
					Level.Scene.UpdateEnabled = true;
					Level.LevelNode.Enabled = true;
				}

				Task<IMapLoader> LoadMap()
				{
					Node mapNode = Level.LevelNode.CreateChild("MapNode");
					return Task.Run(() => {
						var loader = Map.GetLoader(Level,
													mapNode,
													Level.octree,
													storedLevel.Map,
													LoadingWatcher.GetWatcherForSubsection(30));
						loader.StartLoading();
						return loader;
									});
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

			public Task<ILevelManager> Load(LevelRep levelRep, StLevel storedLevel, bool editorMode)
			{
				loaderType = new SavedLevelLoader(this, levelRep, storedLevel, editorMode);

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
