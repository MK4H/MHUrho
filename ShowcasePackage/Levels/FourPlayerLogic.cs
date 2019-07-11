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
	public class FourPlayerLogicType : LevelLogicTypePlugin {
		public override string Name => "FourPlayersFixedResources";
		public override int ID => 2;

		public override int MaxNumberOfPlayers => 4;
		public override int MinNumberOfPlayers => 1;

		public ResourceType Wood { get; private set; }
		public ResourceType Gold { get; private set; }
		
		public override LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow, MHUrhoApp game)
		{
			return new LevelLogicCustomSettings();
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return FourPlayerLogic.CreatePlayingNew(levelSettings, level, this);
		}

		public override LevelLogicInstancePlugin CreateInstanceForEditorLoading(ILevelManager level)
		{
			return FourPlayerLogic.CreateEditingLoading(level, this);
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewLevel(ILevelManager level)
		{
			return FourPlayerLogic.CreateEditingNew(level, this);
		}

		public override LevelLogicInstancePlugin CreateInstanceForLoadingToPlaying(ILevelManager level)
		{
			return FourPlayerLogic.CreatePlayingLoading(level, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			Wood = package.GetResourceType("Wood");
			Gold = package.GetResourceType("Gold");
		}
	}

	public class FourPlayerLogic : LevelInstancePluginBase
	{
		readonly Timeout updateResourcesTimeout;

		readonly FourPlayerLogicType myType;

		public FourPlayerLogic(ILevelManager level, FourPlayerLogicType myType)
			: base(level)
		{
			updateResourcesTimeout = new Timeout(1);
			this.myType = myType;
		}

		public static FourPlayerLogic CreatePlayingNew(LevelLogicCustomSettings levelSettings, ILevelManager level, FourPlayerLogicType myType)
		{
			return new FourPlayerLogic(level, myType);
		}

		public static FourPlayerLogic CreatePlayingLoading(ILevelManager level, FourPlayerLogicType myType)
		{
			return new FourPlayerLogic(level, myType);
		}

		public static FourPlayerLogic CreateEditingNew(ILevelManager level, FourPlayerLogicType myType)
		{
			return new FourPlayerLogic(level, myType);
		}

		public static FourPlayerLogic CreateEditingLoading(ILevelManager level, FourPlayerLogicType myType)
		{
			return new FourPlayerLogic(level, myType);
		}


		public override void Initialize()
		{
			foreach (var player in Level.Players) {
				player.ChangeResourceAmount(myType.Wood, 10);
				player.ChangeResourceAmount(myType.Gold, 10);
			}
			PackageUI = new PackageUI(Level.UIManager, Level, !Level.EditorMode);
		}

		public override void LoadState(PluginDataWrapper fromPluginData)
		{
			PackageUI = new PackageUI(Level.UIManager, Level, !Level.EditorMode);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{

		}

		public override void OnStart()
		{
			PackageUI.UpdateResourceDisplay(Level.HumanPlayer.Resources);
		}

		public override void OnUpdate(float timeStep)
		{
			if (updateResourcesTimeout.Update(timeStep, true))
			{
				PackageUI.UpdateResourceDisplay(Level.HumanPlayer.Resources);
			}

			if (!Level.EditorMode)
			{
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

		public override void Dispose()
		{
			PackageUI.Dispose();
		}

		public override IPathFindAlgFactory GetPathFindAlgFactory()
		{
			return new AStarFactory();
		}

		public override ToolManager GetToolManager(ILevelManager levelManager, InputType inputType)
		{
			if (inputType != InputType.MouseAndKeyboard)
			{
				throw new NotImplementedException();
			}

			return new ToolManagerMandK(levelManager);
		}
	}
}
