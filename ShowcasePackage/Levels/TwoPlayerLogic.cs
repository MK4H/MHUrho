using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.PathFinding.AStar;
using MHUrho.Plugins;
using MHUrho.Storage;
using ShowcasePackage.Misc;
using Urho.Gui;

namespace ShowcasePackage.Levels
{
	public class TwoPlayerLogicType : LevelLogicTypePlugin {
		public static string TypeName =  "TwoPlayers";
		public static int TypeID =  1;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public override int MaxNumberOfPlayers => 2;
		public override int MinNumberOfPlayers => 1;

		public ResourceType Wood { get; private set; }
		public ResourceType Gold { get; private set; }

		public override LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow, MHUrhoApp game)
		{
			return new ResourceCreationWindow(customSettingsWindow, game);
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return TwoPlayerLogic.CreatePlayingNew((ResourceCreationWindow)levelSettings, level, this);
		}

		public override LevelLogicInstancePlugin CreateInstanceForEditorLoading(ILevelManager level)
		{
			return TwoPlayerLogic.CreateEditingLoading(level, this);
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewLevel(ILevelManager level)
		{
			return TwoPlayerLogic.CreateEditingNew(level, this);
		}

		public override LevelLogicInstancePlugin CreateInstanceForLoadingToPlaying(ILevelManager level)
		{
			return TwoPlayerLogic.CreatePlayingLoading(level, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			Wood = package.GetResourceType("Wood");
			Gold = package.GetResourceType("Gold");
		}
	}

	public class TwoPlayerLogic : LevelInstancePluginBase
	{

		readonly Timeout updateResourcesTimeout;
		readonly TwoPlayerLogicType myType;

		ResourceCreationWindow initSettings;

		TwoPlayerLogic(ILevelManager level, TwoPlayerLogicType myType, ResourceCreationWindow initSettings = null)
			: base(level)
		{
			this.updateResourcesTimeout = new Timeout(1);
			this.myType = myType;
			this.initSettings = initSettings;
		}

		public static TwoPlayerLogic CreatePlayingNew(ResourceCreationWindow levelSettings, ILevelManager level, TwoPlayerLogicType myType)
		{
			return new TwoPlayerLogic(level, myType, levelSettings);
		}

		public static TwoPlayerLogic CreatePlayingLoading(ILevelManager level, TwoPlayerLogicType myType)
		{
			return new TwoPlayerLogic(level, myType);
		}

		public static TwoPlayerLogic CreateEditingNew(ILevelManager level, TwoPlayerLogicType myType)
		{
			return new TwoPlayerLogic(level, myType);
		}

		public static TwoPlayerLogic CreateEditingLoading(ILevelManager level, TwoPlayerLogicType myType)
		{
			return new TwoPlayerLogic(level, myType);
		}

		public override void OnUpdate(float timeStep)
		{
			if (updateResourcesTimeout.Update(timeStep, true)) {
				PackageUI.UpdateResourceDisplay(Level.HumanPlayer.Resources);
			}

			if (!Level.EditorMode) {
				bool endLevel = true;
				foreach (var player in Level.Players)
				{
					if (player.TeamID != Level.HumanPlayer.TeamID &&
						player.TeamID != Level.NeutralPlayer.TeamID)
					{
						endLevel = false;
					}
				}

				if (endLevel)
				{
					Level.Input.EndLevelToEndScreen(true);
				}
			}

		}

		public override void Initialize()
		{
			PackageUI = new PackageUI(Level.UIManager, Level, !Level.EditorMode);
		}

		public override void LoadState(PluginDataWrapper fromPluginData)
		{
			PackageUI = new PackageUI(Level.UIManager, Level, !Level.EditorMode);
		}

		public override void SaveState(PluginDataWrapper toPluginData)
		{

		}

		public override void OnStart()
		{
			if (initSettings != null) {
				//Cold start
				foreach (var player in Level.Players) {
					player.ChangeResourceAmount(myType.Wood, 10 + initSettings.Value);
					player.ChangeResourceAmount(myType.Gold, 10 + initSettings.Value);
					initSettings.Dispose();
				}
			}  
			PackageUI.UpdateResourceDisplay(Level.HumanPlayer.Resources);
		}

		public override IPathFindAlgFactory GetPathFindAlgFactory()
		{
			return new AStarFactory(Visualization.TouchedNodes);
		}

		public override ToolManager GetToolManager(ILevelManager levelManager, InputType inputType)
		{
			if (inputType != InputType.MouseAndKeyboard) {
				throw new NotImplementedException();
			}

			return new ToolManagerMandK(levelManager);
		}

		public override void Dispose()
		{
			PackageUI.Dispose();
		}

	}
}
