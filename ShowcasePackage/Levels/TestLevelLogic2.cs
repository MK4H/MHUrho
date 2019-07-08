using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
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
	public class TestLevelLogic2Type : LevelLogicTypePlugin {
		public override string Name => "TestLogic2";
		public override int ID => 2;

		public override int MaxNumberOfPlayers => 3;
		public override int MinNumberOfPlayers => 1;

		
		public override LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow)
		{
			return new LevelLogicCustomSettings();
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return TestLevelLogic2.CreatePlayingNew(levelSettings, level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForEditorLoading(ILevelManager level)
		{
			return TestLevelLogic2.CreateEditingLoading(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewLevel(ILevelManager level)
		{
			return TestLevelLogic2.CreateEditingNew(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForLoadingToPlaying(ILevelManager level)
		{
			return TestLevelLogic2.CreatePlayingLoading(level);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}
	}

	public class TestLevelLogic2 : LevelInstancePluginBase
	{
		readonly Timeout updateResourcesTimeout;

		public TestLevelLogic2(ILevelManager level)
			: base(level)
		{
			updateResourcesTimeout = new Timeout(1);
		}

		public static TestLevelLogic2 CreatePlayingNew(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return new TestLevelLogic2(level);
		}

		public static TestLevelLogic2 CreatePlayingLoading(ILevelManager level)
		{
			return new TestLevelLogic2(level);
		}

		public static TestLevelLogic2 CreateEditingNew(ILevelManager level)
		{
			return new TestLevelLogic2(level);
		}

		public static TestLevelLogic2 CreateEditingLoading(ILevelManager level)
		{
			return new TestLevelLogic2(level);
		}


		public override void Initialize()
		{
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
