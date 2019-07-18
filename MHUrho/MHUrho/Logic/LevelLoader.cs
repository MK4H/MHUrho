using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MHUrho.EntityInfo;
using MHUrho.Input;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;
using Urho.Physics;
using Google.Protobuf;
using MHUrho.CameraMovement;
using MHUrho.Plugins;
using MHUrho.UserInterface;

namespace MHUrho.Logic
{
	partial class LevelManager {

		/// <summary>
		/// Predecessor of all level loaders
		/// </summary>
		abstract class BaseLoader : ILevelLoader {

			/// <inheritdoc />
			public event Action<string> TextUpdate {
				add {
					Progress.TextUpdate += value;
				}
				remove {
					Progress.TextUpdate -= value;
				}
			}
			/// <inheritdoc />
			public event Action<double> PercentageUpdate {
				add {
					Progress.PercentageUpdate += value;
				}
				remove {
					Progress.PercentageUpdate -= value;
				}
			}

			/// <inheritdoc />
			public event Action<IProgressNotifier> Finished;
			/// <inheritdoc />
			public event Action<IProgressNotifier, string> Failed;

			/// <inheritdoc />
			public string Text => Progress.Text;
			/// <inheritdoc />
			public double Percentage => Progress.Percentage;

			/// <inheritdoc />
			ILevelManager ILevelLoader.Level => Level;

			/// <summary>
			/// Instance representing the application.
			/// </summary>
			protected MHUrhoApp Game => MHUrhoApp.Instance;

			/// <summary>
			/// If we are loading into editor mode, or play mode.
			/// </summary>
			protected readonly bool EditorMode;

			/// <summary>
			/// Rep of the loading level.
			/// </summary>
			protected readonly LevelRep LevelRep;

			/// <summary>
			/// Progress of the loading.
			/// </summary>
			protected readonly  ProgressWatcher Progress;

			/// <summary>
			/// Loading level.
			/// </summary>
			protected LevelManager Level;

			/// <summary>
			/// Task factory to post tasks onto the main thread.
			/// </summary>
			readonly TaskFactory TaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.None,
																			TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			/// <summary>
			/// Posts the action <paramref name="func"/> onto the main thread.
			/// </summary>
			/// <param name="func">The function to invoke on the main thread.</param>
			/// <returns>The task representing the posted function.</returns>
			protected Task PostToMainThread(Action func)
			{
				return TaskFactory.StartNew(func);
			}

			/// <summary>
			/// Posts the action <paramref name="func"/> onto the main thread.
			/// </summary>
			/// <param name="func">The function to invoke on the main thread.</param>
			/// <returns>The task representing the posted function.</returns>
			protected Task<T> PostToMainThread<T>(Func<T> func)
			{
				return TaskFactory.StartNew(func);
			}

			/// <summary>
			/// Creates loader to load the level represented by <paramref name="levelRep"/> into the mode specified by <paramref name="editorMode"/>.
			/// Can be added as part of bigger process with <paramref name="parentProgress"/> and <paramref name="loadingSubsectionSize"/>.
			/// </summary>
			/// <param name="levelRep">The rep of the level to load.</param>
			/// <param name="editorMode">If the level should be loaded into editor mode or playing mode.</param>
			/// <param name="parentProgress">Progress watcher for the parent process.</param>
			/// <param name="loadingSubsectionSize">Size of this loading process as a part of the parent process.</param>
			protected BaseLoader(LevelRep levelRep, bool editorMode, IProgressEventWatcher parentProgress, double loadingSubsectionSize)
			{
				this.LevelRep = levelRep;
				this.EditorMode = editorMode;
				this.Progress = new ProgressWatcher(parentProgress, loadingSubsectionSize);
				this.Progress.Finished += LoadingFinished;
				this.Progress.Failed += LoadingFailed;
			}

			/// <summary>
			/// Starts the loading.
			/// </summary>
			/// <returns>Task representing the loading process.</returns>
			public abstract Task<ILevelManager> StartLoading();

			/// <summary>
			/// Initializes the basic level structure.
			/// </summary>
			/// <returns>LevelManager with initialized based level structure.</returns>
			protected LevelManager InitializeLevel()
			{
				//Before level is asociated with screen, the global try/catch will not dispose of the screen
				Scene scene = null;
				Octree octree = null;
				try
				{
					scene = new Scene(Game.Context) { UpdateEnabled = false };
					octree = scene.CreateComponent<Octree>();
					var physics = scene.CreateComponent<PhysicsWorld>();

					physics.Enabled = true;

					LoadSceneParts(scene);
				}
				catch (Exception e)
				{
					Urho.IO.Log.Write(LogLevel.Error, $"Screen creation failed with: {e.Message}");
					octree?.Dispose();
					scene?.RemoveAllChildren();
					scene?.Remove();
					scene?.Dispose();
					throw;
				}

				var levelNode = scene.CreateChild("LevelNode");
				levelNode.Enabled = false;
				CurrentLevel = new LevelManager(levelNode, LevelRep, Game, octree, EditorMode);
				levelNode.AddComponent(CurrentLevel);

				CurrentLevel.Plugin = GetPlugin(CurrentLevel);

				return CurrentLevel;
			}

			/// <summary>
			/// Gets instance plugin of the level logic for the <paramref name="level"/>.
			/// </summary>
			/// <param name="level">The level to get the instance plugin for.</param>
			/// <returns>Instance plugin to control the <paramref name="level"/>.</returns>
			protected abstract LevelLogicInstancePlugin GetPlugin(LevelManager level);

			/// <summary>
			/// Loads parts common to scene of every level
			/// </summary>
			/// <param name="scene">The scene of the level</param>
			void LoadSceneParts(Scene scene)
			{

				// Light
				using (Node lightNode = scene.CreateChild(name: "light"))
				{
					lightNode.Rotation = new Quaternion(45, 0, 0);
					//lightNode.Position = new Vector3(0, 5, 0);
					using (var light = lightNode.CreateComponent<Light>())
					{
						light.LightType = LightType.Directional;
						//light.Range = 10;
						light.Brightness = 0.5f;
						light.CastShadows = true;
						light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
						light.ShadowCascade = new CascadeParameters(20.0f, 0f, 0f, 0.0f, 0.8f);
					}
				}

				// Ambient light
				using (var zoneNode = scene.CreateChild("Zone"))
				{
					using (var zone = zoneNode.CreateComponent<Zone>())
					{
						zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
						zone.AmbientColor = new Color(0.5f, 0.5f, 0.5f);
						zone.FogColor = new Color(0.7f, 0.7f, 0.7f);
						zone.FogStart = Game.Config.TerrainDrawDistance / 2;
						zone.FogEnd = Game.Config.TerrainDrawDistance;
					}
				}



			}

			/// <summary>
			/// Creates a camera and an associated viewport to display the camera output into.
			/// </summary>
			/// <param name="level">Level into which the camera is loaded.</param>
			/// <param name="cameraPosition">Initial position of the camera</param>
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

			/// <summary>
			/// Creates a player to serve as a placeholder for future player with AI.
			/// Placeholder serves only as a container of units, buildings and projectiles, with no behavior.
			///
			/// Player is added to the level.
			/// </summary>
			/// <param name="insignia">Icons and healthbars for the players units.</param>
			/// <returns>The new placeholder player, initialized and placed into the level.</returns>
			protected Player CreatePlaceholderPlayer(PlayerInsignia insignia)
			{
				var newPlayer = Player.CreatePlaceholderPlayer(Level.GetNewID(Level.players),
																Level,
																insignia);
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
				foreach (var player in Level.Players)
				{
					Level.UIManager.AddPlayer(player);
				}
				Level.UIManager.SelectPlayer(Level.Input.Player);
			}

			/// <summary>
			/// Method for forwarding <see cref="Progress"/> <see cref="ProgressWatcher.Finished"/> events to our <see cref="Finished"/> event.
			/// </summary>
			/// <param name="progress">Should always be our <see cref="Progress"/>.</param>
			void LoadingFinished(IProgressNotifier progress)
			{
				try
				{
					Finished?.Invoke(this);
				}
				catch (Exception e)
				{
					Urho.IO.Log.Write(Urho.LogLevel.Warning,
									$"There was an unexpected exception during the invocation of {nameof(Finished)}: {e.Message}");
				}
			}

			/// <summary>
			/// Method for forwarding <see cref="Progress"/> <see cref="ProgressWatcher.Failed"/> events to our <see cref="Failed"/> event.
			/// </summary>
			/// <param name="progress">Should always be our <see cref="Progress"/>.</param>
			void LoadingFailed(IProgressNotifier progress, string message)
			{
				try
				{
					Failed?.Invoke(this, message);
				}
				catch (Exception e)
				{
					Urho.IO.Log.Write(Urho.LogLevel.Warning,
									$"There was an unexpected exception during the invocation of {nameof(Finished)}: {e.Message}");
				}
			}

		}

		/// <summary>
		/// Loads level in a default state for player to edit.
		/// </summary>
		class DefaultLevelLoader : BaseLoader {

			//Estimates on relative durations of different parts of loading
			const double initLPartSize = 2;
			const double mapLPartSize = 70;
			const double playersLPartSize = 20;
			const double controlLPartSize = 7;
			//1 for finishloading

			/// <summary>
			/// Creates loader to generate the level represented by <paramref name="levelRep"/>.
			/// </summary>
			/// <param name="levelRep">Presentation part of the level.</param>
			/// <param name="mapSize">Size of the map to generate the level with.</param>
			/// <param name="parentProgress">Progress watcher for the parent process.</param>
			/// <param name="loadingSubsectionSize">Size of this loading process as a part of the parent process.</param>
			public DefaultLevelLoader(LevelRep levelRep, 
									IntVector2 mapSize, 
									IProgressEventWatcher parentProgress = null, 
									double loadingSubsectionSize = 100)
				: base(levelRep, true, parentProgress, loadingSubsectionSize)
			{
				this.mapSize = mapSize;

				if (LevelRep.MaxNumberOfPlayers < 1)
				{
					throw new ArgumentException("Level without players does not make sense", nameof(levelRep));
				}

				if (mapSize.X < WorldMap.Map.MinSize.X || WorldMap.Map.MaxSize.X < mapSize.X ||
					mapSize.Y < WorldMap.Map.MinSize.Y || WorldMap.Map.MaxSize.Y < mapSize.Y)
				{
					throw new ArgumentOutOfRangeException(nameof(mapSize),
														mapSize,
														"MapSize was out of bounds set by Map.MinSize and Map.MaxSize");
				}

				if (mapSize.X % WorldMap.Map.ChunkSize.X != 0 || mapSize.Y % WorldMap.Map.ChunkSize.Y != 0)
				{
					throw new ArgumentException("MapSize has to be an integer multiple of Map.ChunkSize", nameof(mapSize));
				}
			}

			/// <summary>
			/// Size of the map to generate.
			/// </summary>
			readonly IntVector2 mapSize;

			/// <summary>
			/// Starts the loading process.
			/// </summary>
			/// <returns>Task representing the loading process.</returns>
			public override async Task<ILevelManager> StartLoading()
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"Loading default level. MapSize: {mapSize}, LevelName: {LevelRep.Name}, GamePack: {LevelRep.GamePack}");

				try
				{
					Progress.SendTextUpdate("Initializing level");
					Level = await PostToMainThread<LevelManager>(InitializeLevel);
					Progress.SendUpdate(initLPartSize, "Initialized level");

					PlayerInsignia.InitInsignias(Game.PackageManager);

					Progress.SendTextUpdate("Loading map");
							
					Level.map = await PostToMainThread(CreateDefaultMap);
					//Map percentage is updated by Subsection watcher
					Progress.SendTextUpdate("Loaded map");

					Level.Minimap = new Minimap(Level, 4);

					await PostToMainThread(CreateCamera);


					Progress.SendTextUpdate("Creating players");
					await PostToMainThread(CreatePlayers);
					Progress.SendUpdate(playersLPartSize, "Created players");

					Progress.SendTextUpdate("Giving player controls");
					await PostToMainThread(CreateControl);
					Progress.SendUpdate(controlLPartSize, "Player controls created");

					Progress.SendTextUpdate("Starting level");
					await PostToMainThread(StartLevel);

					Progress.SendFinished();
					return Level;
				}
				catch (Exception e)
				{
					string message = $"Level loading failed with: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					Level?.Dispose();
					CurrentLevel = null;
					Progress.SendFailed(message);
					return null;
				}

			}

			/// <inheritdoc />
			protected override LevelLogicInstancePlugin GetPlugin(LevelManager level)
			{
				return LevelRep.LevelLogicType.CreateInstancePluginForBrandNewLevel(level);
			}

			/// <summary>
			/// Creates a camera in the generated level.
			/// </summary>
			void CreateCamera()
			{
				LoadCamera(Level, new Vector2(10, 10));
			}

			/// <summary>
			/// Creates placeholder players in the generated level.
			/// </summary>
			void CreatePlayers()
			{

				InsigniaGetter insignias = new InsigniaGetter();
				Level.HumanPlayer = CreatePlaceholderPlayer(insignias.GetNextUnusedInsignia());
				//human player gets the input and is the first one selected
				Level.Input =
					Game.ControllerFactory.CreateGameController(Level.Camera, Level, Level.Scene.GetComponent<Octree>(), Level.HumanPlayer);

				//Neutral player placeholder
				Level.NeutralPlayer = CreatePlaceholderPlayer(insignias.GetUnusedInsignia(insignias.NeutralPlayerIndex));

				//AI player placeholders
				for (int i = 1; i < LevelRep.MaxNumberOfPlayers; i++)
				{
					CreatePlaceholderPlayer(insignias.GetNextUnusedInsignia());
				}

				RegisterPlayersToUI();
			}

			/// <summary>
			/// Generates the default map.
			/// </summary>
			/// <returns>The generated default map.</returns>
			Map CreateDefaultMap()
			{
				Node mapNode = Level.LevelNode.CreateChild("MapNode");
				return WorldMap.Map.CreateDefaultMap(CurrentLevel, mapNode, Level.octree, Level.Plugin.GetPathFindAlgFactory(), mapSize, new ProgressWatcher(Progress, mapLPartSize));
			}

			/// <summary>
			/// Creates camera control and tools.
			/// </summary>
			void CreateControl()
			{
				Level.cameraController = Game.ControllerFactory.CreateCameraController(Level.Input, Level.Camera);
				Level.ToolManager = Level.Plugin.GetToolManager(Level, Level.Input.InputType);
				Level.ToolManager.LoadTools();
			}

			/// <summary>
			/// Starts the level
			/// </summary>
			void StartLevel()
			{
				Level.Plugin.Initialize();
				Level.Plugin.OnStart();
				Level.UIManager.ShowUI();
				Level.Scene.UpdateEnabled = true;
				Level.LevelNode.Enabled = true;
			}
		}

		/// <summary>
		/// Base class for loaders that load the level from a saved game.
		/// </summary>
		abstract class SavedLevelLoader : BaseLoader
		{
			//Estimates of the duration of parts of the loading process
			// for sending updates
			protected const double initLPartSize = 2;
			protected const double mapLPartSize = 40;
			protected const double unitsLPartSize = 10;
			protected const double buildingLPartSize = 10;
			protected const double projectilesLPartSize = 10;
			protected const double playersLPartSize = 10;
			protected const double connectingReferencesLPartSize = 5;
			protected const double finishingLoadingLPartSize = 3;

			/// <summary>
			/// Creates a loader that loads the level saved in the <paramref name="storedLevel"/> to mode based on <paramref name="editorMode"/>.
			/// </summary>
			/// <param name="levelRep">Presentation part of the level.</param>
			/// <param name="storedLevel">Saved level.</param>
			/// <param name="editorMode">If the level should be loaded to editor mode or playing mode.</param>
			/// <param name="parentProgress">Progress watcher for the parent process.</param>
			/// <param name="loadingSubsectionSize">Size of this loading process as a part of the parent process.</param>
			protected SavedLevelLoader(LevelRep levelRep, StLevel storedLevel, bool editorMode, IProgressEventWatcher parentProgress, double loadingSubsectionSize)
				: base(levelRep, editorMode, parentProgress, loadingSubsectionSize)
			{
				this.StoredLevel = storedLevel;
			}

			/// <summary>
			/// Saved level that we are loading.
			/// </summary>
			protected readonly StLevel StoredLevel;

			/// <summary>
			/// Loaders of the parts of the loading level.
			/// </summary>
			protected List<ILoader> Loaders;

			/// <summary>
			/// Starts the loading process of the level.
			/// </summary>
			/// <returns>Task representing the loading process of the level.</returns>
			public override async Task<ILevelManager> StartLoading()
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"Loading stored level. LevelName: {LevelRep.Name}, GamePack: {LevelRep.GamePack}, EditorMode: {EditorMode}");

				try
				{
					Loaders = new List<ILoader>();
					Progress.SendTextUpdate("Initializing level");
					Level = await PostToMainThread(InitializeLevel);
					Progress.SendUpdate(initLPartSize, "Initialized level");

					PlayerInsignia.InitInsignias(Game.PackageManager);


					var mapLoader = await PostToMainThread(StartMapLoader);
					Loaders.Add(mapLoader);
					Level.map = mapLoader.Map;
					Level.Minimap = new Minimap(Level, 4);


					await PostToMainThread(CreateCamera);

					//ALT: Maybe give each its own subsection watcher
					await PostToMainThread(LoadUnits);
					await PostToMainThread(LoadBuildings);
					await PostToMainThread(LoadProjectiles);
					await PostToMainThread(LoadPlayers);
					await PostToMainThread(LoadToolsAndControllers);
					await PostToMainThread(LoadLevelPlugin);
					await PostToMainThread(ConnectReferences);
					await PostToMainThread(FinishLoading);
					await PostToMainThread(StartLevel);

					Progress.SendFinished();
					return Level;
				}
				catch (Exception e)
				{
					string message = $"Level loading failed with: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					Level?.Dispose();
					CurrentLevel = null;
					Progress.SendFailed(message);
					return null;
				}
			}

			/// <summary>
			/// Loads units saved in the level.
			/// </summary>
			protected virtual void LoadUnits()
			{
				Progress.SendTextUpdate("Loading units");
				foreach (var unit in StoredLevel.Units)
				{
					var unitLoader = Unit.GetLoader(Level, unit);
					unitLoader.StartLoading();
					Level.RegisterEntity(unitLoader.Unit);
					Level.units.Add(unitLoader.Unit.ID, unitLoader.Unit);
					Loaders.Add(unitLoader);
				}

				Progress.SendUpdate(unitsLPartSize, "Loaded units");
			}

			/// <summary>
			/// Loads buildings that were saved in the level.
			/// </summary>
			protected virtual void LoadBuildings()
			{
				Progress.SendTextUpdate("Loading buildings");
				foreach (var building in StoredLevel.Buildings)
				{
					var buildingLoader = Building.GetLoader(Level, building);
					buildingLoader.StartLoading();
					Level.RegisterEntity(buildingLoader.Building);
					Level.buildings.Add(buildingLoader.Building.ID, buildingLoader.Building);
					Loaders.Add(buildingLoader);
				}
				Progress.SendUpdate(buildingLPartSize, "Loaded buildings");
			}

			/// <summary>
			/// Loads projectiles that were saved in the level.
			/// </summary>
			protected virtual void LoadProjectiles()
			{
				Progress.SendTextUpdate("Loading projectiles");
				foreach (var projectile in StoredLevel.Projectiles)
				{
					var projectileLoader = Projectile.GetLoader(Level, projectile);
					projectileLoader.StartLoading();
					Level.RegisterEntity(projectileLoader.Projectile);
					Level.projectiles.Add(projectileLoader.Projectile.ID, projectileLoader.Projectile);
					Loaders.Add(projectileLoader);
				}
				Progress.SendUpdate(projectilesLPartSize, "Loaded projectiles");
			}

			/// <summary>
			/// Loads players that were saved in the level.
			/// </summary>
			protected abstract void LoadPlayers();

			/// <summary>
			/// Creates camera controller and loads the tools for user control of the level.
			/// </summary>
			protected virtual void LoadToolsAndControllers()
			{
				Level.cameraController = Game.ControllerFactory.CreateCameraController(Level.Input, Level.Camera);
				Level.ToolManager = Level.Plugin.GetToolManager(Level, Level.Input.InputType);
				Level.ToolManager.LoadTools();
			}

			/// <summary>
			/// Loads instance plugin for the level logic.
			/// </summary>
			protected virtual void LoadLevelPlugin()
			{
				Level.Plugin.LoadState(new PluginDataWrapper(StoredLevel.Plugin.Data, Level));
			}

			/// <summary>
			/// Connects stored references of the all the loaded parts of the level.
			/// </summary>
			protected virtual void ConnectReferences()
			{
				Progress.SendTextUpdate("Connecting references");
				//Connect references
				foreach (var loader in Loaders)
				{
					loader.ConnectReferences();
				}

				Progress.SendUpdate(connectingReferencesLPartSize, "Connected references");
			}

			/// <summary>
			/// Finishes loading and cleans up.
			/// </summary>
			protected virtual void FinishLoading()
			{
				Progress.SendTextUpdate("Finishing loading");
				foreach (var loader in Loaders)
				{
					loader.FinishLoading();
				}
				Progress.SendUpdate(finishingLoadingLPartSize, "FinishedLoading");
			}

			/// <summary>
			/// Starts the level.
			/// </summary>
			protected virtual void StartLevel()
			{
				Progress.SendTextUpdate("Starting level");

				CurrentLevel = Level;
				Level.Plugin.OnStart();
				Level.UIManager.ShowUI();
				Level.Scene.UpdateEnabled = true;
				Level.LevelNode.Enabled = true;
			}

			/// <summary>
			/// Loads a player.
			/// </summary>
			/// <param name="playerLoader">The player loader for loading of the player.</param>
			protected void LoadPlayer(IPlayerLoader playerLoader)
			{
				playerLoader.StartLoading();

				Level.LevelNode.AddComponent(playerLoader.Player);
				Level.players.Add(playerLoader.Player.ID, playerLoader.Player);
				Loaders.Add(playerLoader);
			}

			/// <summary>
			/// Creates a camera in the loaded level.
			/// </summary>
			void CreateCamera()
			{
				//ALT:Maybe Save camera position
				LoadCamera(Level, new Vector2(10, 10));
			}

			/// <summary>
			/// Creates map loader and does the first step of loading.
			/// We are nod doing concurrency, just splitting it up into more steps.
			/// </summary>
			/// <returns>Map loader after the fist step.</returns>
			IMapLoader StartMapLoader()
			{
				Node mapNode = Level.LevelNode.CreateChild("MapNode");
				var loader = WorldMap.Map.GetLoader(Level,
													mapNode,
													Level.octree,
													Level.Plugin.GetPathFindAlgFactory(),
													StoredLevel.Map,
													new ProgressWatcher(Progress, mapLPartSize));
				//Does the first step of loading
				loader.StartLoading();
				return loader;

			}


		}

		/// <summary>
		/// Loads a saved level for playing.
		/// </summary>
		class SavedLevelPlayingLoader : SavedLevelLoader
		{
			/// <summary>
			/// Data for player initialization.
			/// </summary>
			readonly PlayerSpecification players;

			/// <summary>
			/// Data for level logic plugin initialization.
			/// </summary>
			readonly LevelLogicCustomSettings customSettings;


			/// <summary>
			/// Creates a loader that loads saved level into a playing mode.
			/// </summary>
			/// <param name="levelRep">Presentation part of the level.</param>
			/// <param name="storedLevel">Saved level.</param>
			/// <param name="players">Data for player initialization.</param>
			/// <param name="customSettings">Data for level logic initialization.</param>
			/// <param name="parentProgress">Progress watcher for the parent process.</param>
			/// <param name="loadingSubsectionSize">Size of this loading process as a part of the parent process.</param>
			public SavedLevelPlayingLoader(LevelRep levelRep,
											StLevel storedLevel,
											PlayerSpecification players,
											LevelLogicCustomSettings customSettings, 
											IProgressEventWatcher parentProgress = null, 
											double loadingSubsectionSize = 100)
				: base(levelRep, storedLevel, false, parentProgress, loadingSubsectionSize)
			{
				this.players = players;
				this.customSettings = customSettings;

				if ((players == PlayerSpecification.LoadFromSavedGame) !=
					(customSettings == LevelLogicCustomSettings.LoadFromSavedGame))
				{
					throw new
						ArgumentException("Argument mismatch, one argument is loaded from save and the other is not");
				}
			}

			/// <inheritdoc/>
			protected override LevelLogicInstancePlugin GetPlugin(LevelManager level)
			{
				if (customSettings == LevelLogicCustomSettings.LoadFromSavedGame)
				{
					//Loading saved game in play, no new custom settings, just load it
					return LevelRep.LevelLogicType.CreateInstancePluginForLoadingToPlaying(level);
				}
				else
				{
					//Loading saved level prototype, so i need to load it with custom settings
					return LevelRep.LevelLogicType.CreateInstancePluginForNewPlaying(customSettings, level);
				}

			}

			/// <summary>
			/// Loads players and initializes them with the <see cref="players"/> data.
			/// </summary>
			protected override void LoadPlayers()
			{
				Progress.SendTextUpdate("Loading players");
				//If players == null, we are loading a saved level already in play with set AIs
				InsigniaGetter insigniaGetter = new InsigniaGetter();
				if (players == PlayerSpecification.LoadFromSavedGame)
				{
					foreach (var storedPlayer in StoredLevel.Players.Players)
					{
						LoadPlayer(Player.GetLoader(Level, storedPlayer, insigniaGetter));
					}

					Level.HumanPlayer = Level.GetPlayer(StoredLevel.Players.HumanPlayerID);
					Level.NeutralPlayer = Level.GetPlayer(StoredLevel.Players.NeutralPlayerID);
				}
				//We are loading new level from a prototype or a level in play with new AIs
				else
				{

					foreach (var playerInfo in players)
					{
						IPlayerLoader newPlayer =
							Player.GetLoaderFromInfo(Level, StoredLevel.Players.Players, playerInfo, insigniaGetter);
						LoadPlayer(newPlayer);

						if (playerInfo.IsHuman && playerInfo.IsNeutral)
						{
							throw new
								ArgumentException("Corrupted save file, neutral player cannot have the input");
						}
						else if (playerInfo.IsNeutral)
						{
							Level.NeutralPlayer = newPlayer.Player;
						}
						else if (playerInfo.IsHuman)
						{
							Level.HumanPlayer = newPlayer.Player;
						}
					}
				}

				if (Level.HumanPlayer == null)
				{
					throw new
						ArgumentException("Corrupted save file, no human has input");
				}

				Level.Input = Game.ControllerFactory.CreateGameController(Level.Camera, Level, Level.octree, Level.HumanPlayer);
				RegisterPlayersToUI();


				//Player selection is disabled by default in play mode, but it can be enabled again
				Level.UIManager.DisablePlayerSelection();

				Progress.SendUpdate(playersLPartSize, "Loaded players");
			}

			/// <inheritdoc/>
			protected override void FinishLoading()
			{
				customSettings.Dispose();
				base.FinishLoading();
			}
		}

		/// <summary>
		/// Loads the saved level for editing.
		/// </summary>
		class SavedLevelEditorLoader : SavedLevelLoader
		{
			/// <summary>
			/// Creates a loader that loads the saved level for editing.
			/// </summary>
			/// <param name="levelRep">Presentation part of the level.</param>
			/// <param name="storedLevel">Saved level.</param>
			/// <param name="parentProgress">Progress watcher for the parent process.</param>
			/// <param name="loadingSubsectionSize">Size of this loading process as a part of the parent process.</param>
			public SavedLevelEditorLoader(LevelRep levelRep, StLevel storedLevel, IProgressEventWatcher parentProgress = null, double loadingSubsectionSize = 100)
				: base(levelRep, storedLevel, true, parentProgress, loadingSubsectionSize)
			{ }

			/// <inheritdoc />
			protected override LevelLogicInstancePlugin GetPlugin(LevelManager level)
			{
				//Loading saved level prototype, so i need to load it
				return LevelRep.LevelLogicType.CreateInstancePluginForEditorLoading(level);
			}

			/// <summary>
			/// Loads players, replacing them with placeholder players for editing.
			/// </summary>
			protected override void LoadPlayers()
			{
				Progress.SendTextUpdate("Loading players");

				int numberOfPlayers = 0;
				InsigniaGetter insigniaGetter = new InsigniaGetter();
				foreach (var existingPlayer in StoredLevel.Players.Players)
				{
					numberOfPlayers++;
					IPlayerLoader newPlayer = Player.GetLoaderToPlaceholder(Level, existingPlayer, insigniaGetter);
					LoadPlayer(newPlayer);

					//While editing, even the neutral player can have input
					//When starting play, user will set who has input manually anyway
					if (newPlayer.Player.ID == StoredLevel.Players.NeutralPlayerID)
					{
						Level.NeutralPlayer = newPlayer.Player;
					}
					else if (newPlayer.Player.ID == StoredLevel.Players.HumanPlayerID)
					{
						Level.HumanPlayer = newPlayer.Player;
						Level.Input = Game.ControllerFactory.CreateGameController(Level.Camera, Level, Level.octree, newPlayer.Player);
					}
				}

				//There will always be the neutral player and the player with input (representing the human behind the keyboard)
				if (numberOfPlayers < 2)
				{
					throw new
						ArgumentException("Corrupted save file, there was less than 2 players (neutral or human missing)");
				}

				//Some player always has to have the input
				if (Level.Input == null)
				{
					throw new ArgumentException("Corrupted save file, no player had input");
				}

				for (; numberOfPlayers < LevelRep.MaxNumberOfPlayers; numberOfPlayers++)
				{
					CreatePlaceholderPlayer(insigniaGetter.GetNextUnusedInsignia());
				}


				RegisterPlayersToUI();

				Progress.SendUpdate(playersLPartSize, "Loaded players");
			}
		}
	}
    
}
