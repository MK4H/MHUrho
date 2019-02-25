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
using Urho.Gui;

namespace DefaultPackage
{
	public class TestLevelLogic2Type : LevelLogicTypePlugin {
		public override string Name => "TestLogic2";
		public override int ID => 2;

		public override int MaxNumberOfPlayers => 6;
		public override int MinNumberOfPlayers => 1;

		public override void Initialize(XElement extensionElement, GamePack package)
		{

		}


		public override LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow)
		{
			return new LevelLogicCustomSettings();
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return new TestLevelLogic2(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForEditorLoading(ILevelManager level)
		{
			return new TestLevelLogic2(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewLevel(ILevelManager level)
		{
			return new TestLevelLogic2(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForLoadingToPlaying(ILevelManager level)
		{
			return new TestLevelLogic2(level);
		}
	}

	public class TestLevelLogic2 : LevelLogicInstancePlugin {
		public TestLevelLogic2(ILevelManager level)
			: base(level)
		{ }

		public override void SaveState(PluginDataWrapper pluginData)
		{

		}

		public override void LoadState(PluginDataWrapper pluginData)
		{

		}

		public override void Dispose()
		{

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
