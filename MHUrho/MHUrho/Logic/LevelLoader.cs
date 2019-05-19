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
using MHUrho.CameraMovement;
using MHUrho.Plugins;
using MHUrho.UserInterface;

namespace MHUrho.Logic
{
	partial class LevelManager {

		abstract class BaseLoader : ILevelLoader {

			public event Action<string> TextUpdate {
				add {
					Progress.TextUpdate += value;
				}
				remove {
					Progress.TextUpdate -= value;
				}
			}
			public event Action<double> PercentageUpdate {
				add {
					Progress.PercentageUpdate += value;
				}
				remove {
					Progress.PercentageUpdate -= value;
				}
			}

			public event Action<IProgressNotifier> Finished;
			public event Action<IProgressNotifier, string> Failed;

			public string Text => Progress.Text;
			public double Percentage => Progress.Percentage;

			ILevelManager ILevelLoader.Level => Level;

			protected MHUrhoApp Game => MHUrhoApp.Instance;

			protected readonly bool EditorMode;

			protected readonly LevelRep LevelRep;

			protected readonly  ProgressWatcher Progress;

			protected LevelManager Level;

			protected BaseLoader(LevelRep levelRep, bool editorMode, IProgressEventWatcher parentProgress, double loadingSubsectionSize)
			{
				this.LevelRep = levelRep;
				this.EditorMode = editorMode;
				this.Progress = new ProgressWatcher(parentProgress, loadingSubsectionSize);
				this.Progress.Finished += LoadingFinished;
				this.Progress.Failed += LoadingFailed;
			}

			public abstract Task<ILevelManager> StartLoading();

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

					//TODO: Test if i can just use it to manually call UpdateCollisions with all rigidBodies kinematic
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

			protected abstract LevelLogicInstancePlugin GetPlugin(LevelManager level);

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

		class DefaultLevelLoader : BaseLoader {
			const double initLPartSize = 2;
			const double mapLPartSize = 70;
			const double playersLPartSize = 20;
			const double controlLPartSize = 7;
			//1 for finishloading

			public DefaultLevelLoader(LevelRep levelRep, IntVector2 mapSize, IProgressEventWatcher parentProgress = null, double loadingSubsectionSize = 100)
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

			readonly IntVector2 mapSize;

			public override async Task<ILevelManager> StartLoading()
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"Loading default level. MapSize: {mapSize}, LevelName: {LevelRep.Name}, GamePack: {LevelRep.GamePack}");
				LoadingSanityCheck();

				try
				{
					Progress.SendTextUpdate("Initializing level");
					Level = await MHUrhoApp.InvokeOnMainSafeAsync<LevelManager>(InitializeLevel);
					Progress.SendUpdate(initLPartSize, "Initialized level");

					PlayerInsignia.InitInsignias(PackageManager.Instance);

					Progress.SendTextUpdate("Loading map");
					Node mapNode = await MHUrhoApp.InvokeOnMainSafeAsync(() => Level.LevelNode.CreateChild("MapNode"));


					//This will take a long time, run it in another thread			
					Level.map = await Task.Run<Map>(() => WorldMap.Map.CreateDefaultMap(CurrentLevel, mapNode, Level.octree, Level.Plugin.GetPathFindAlgFactory(), mapSize, new ProgressWatcher(Progress, mapLPartSize)));
					//Map percentage is updated by Subsection watcher
					Progress.SendTextUpdate("Loaded map");

					Level.Minimap = new Minimap(Level, 4);

					MHUrhoApp.InvokeOnMainSafe(CreateCamera);


					Progress.SendTextUpdate("Creating players");
					MHUrhoApp.InvokeOnMainSafe(CreatePlayers);
					Progress.SendUpdate(playersLPartSize, "Created players");

					Progress.SendTextUpdate("Giving player controls");
					MHUrhoApp.InvokeOnMainSafe(CreateControl);
					Progress.SendUpdate(controlLPartSize, "Player controls created");

					Progress.SendTextUpdate("Starting level");
					MHUrhoApp.InvokeOnMainSafe(StartLevel);

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

			void LoadingSanityCheck()
			{

			}
		}

		abstract class SavedLevelLoader : BaseLoader
		{

			protected const double initLPartSize = 2;
			protected const double mapLPartSize = 40;
			protected const double unitsLPartSize = 10;
			protected const double buildingLPartSize = 10;
			protected const double projectilesLPartSize = 10;
			protected const double playersLPartSize = 10;
			protected const double connectingReferencesLPartSize = 5;
			protected const double finishingLoadingLPartSize = 3;

			protected SavedLevelLoader(LevelRep levelRep, StLevel storedLevel, bool editorMode, IProgressEventWatcher parentProgress, double loadingSubsectionSize)
				: base(levelRep, editorMode, parentProgress, loadingSubsectionSize)
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

				try
				{
					Loaders = new List<ILoader>();
					Progress.SendTextUpdate("Initializing level");
					Level = await MHUrhoApp.InvokeOnMainSafeAsync<LevelManager>(InitializeLevel);
					Progress.SendUpdate(initLPartSize, "Initialized level");

					PlayerInsignia.InitInsignias(PackageManager.Instance);


					var mapLoader = await LoadMap();
					Loaders.Add(mapLoader);
					Level.map = mapLoader.Map;
					Level.Minimap = new Minimap(Level, 4);


					MHUrhoApp.InvokeOnMainSafe(CreateCamera);

					//ALT: Maybe give each its own subsection watcher
					MHUrhoApp.InvokeOnMainSafe(LoadUnits);
					MHUrhoApp.InvokeOnMainSafe(LoadBuildings);
					MHUrhoApp.InvokeOnMainSafe(LoadProjectiles);
					MHUrhoApp.InvokeOnMainSafe(LoadPlayers);
					MHUrhoApp.InvokeOnMainSafe(LoadToolsAndControllers);
					MHUrhoApp.InvokeOnMainSafe(LoadLevelPlugin);
					MHUrhoApp.InvokeOnMainSafe(ConnectReferences);
					MHUrhoApp.InvokeOnMainSafe(FinishLoading);
					MHUrhoApp.InvokeOnMainSafe(StartLevel);

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
				Progress.SendTextUpdate("Connecting references");
				//Connect references
				foreach (var loader in Loaders)
				{
					loader.ConnectReferences();
				}

				Progress.SendUpdate(connectingReferencesLPartSize, "Connected references");
			}

			protected virtual void FinishLoading()
			{
				Progress.SendTextUpdate("Finishing loading");
				foreach (var loader in Loaders)
				{
					loader.FinishLoading();
				}
				Progress.SendUpdate(finishingLoadingLPartSize, "FinishedLoading");
			}

			protected virtual void StartLevel()
			{
				Progress.SendTextUpdate("Starting level");

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
				//ALT:Maybe Save camera position
				LoadCamera(Level, new Vector2(10, 10));
			}

			Task<IMapLoader> LoadMap()
			{
				Node mapNode = Level.LevelNode.CreateChild("MapNode");
				return Task.Run(() => {
					var loader = WorldMap.Map.GetLoader(Level,
														mapNode,
														Level.octree,
														Level.Plugin.GetPathFindAlgFactory(),
														StoredLevel.Map,
														new ProgressWatcher(Progress, mapLPartSize));
					loader.StartLoading();
					return loader;
				});
			}


		}

		class SavedLevelPlayingLoader : SavedLevelLoader
		{

			readonly PlayerSpecification players;
			readonly LevelLogicCustomSettings customSettings;

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

			protected override void FinishLoading()
			{
				customSettings.Dispose();
				base.FinishLoading();
			}
		}

		class SavedLevelEditorLoader : SavedLevelLoader
		{
			public SavedLevelEditorLoader(LevelRep levelRep, StLevel storedLevel, IProgressEventWatcher parentProgress = null, double loadingSubsectionSize = 100)
				: base(levelRep, storedLevel, true, parentProgress, loadingSubsectionSize)
			{ }

			protected override LevelLogicInstancePlugin GetPlugin(LevelManager level)
			{
				//Loading saved level prototype, so i need to load it
				return LevelRep.LevelLogicType.CreateInstancePluginForEditorLoading(level);
			}

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
