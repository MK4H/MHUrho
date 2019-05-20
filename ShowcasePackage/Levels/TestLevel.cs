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

namespace ShowcasePackage.Levels
{
	public class TestLevelLogicType : LevelLogicTypePlugin {
		public override string Name => "TestLogic";
		public override int ID => 1;

		public override int MaxNumberOfPlayers => 6;
		public override int MinNumberOfPlayers => 1;


		public override LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow)
		{
			return new LevelLogicCustomSettings();
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return new TestLevel(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForEditorLoading(ILevelManager level)
		{
			return new TestLevel(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewLevel(ILevelManager level)
		{
			return new TestLevel(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForLoadingToPlaying(ILevelManager level)
		{
			return new TestLevel(level);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}
	}

	public class TestLevel : LevelLogicInstancePlugin
	{
		public TestLevel(ILevelManager level)
			: base(level)
		{ }


		public override void OnUpdate(float timeStep)
		{
			
		}

		public override void LoadState(PluginDataWrapper fromPluginData)
		{

		}

		public override void SaveState(PluginDataWrapper toPluginData)
		{

		}

		public override IPathFindAlgFactory GetPathFindAlgFactory()
		{
			return new AStarFactory();
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

		}


	}
}
