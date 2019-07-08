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
	public class TestLevelLogicType : LevelLogicTypePlugin {
		public static string TypeName =  "TestLogic";
		public static int TypeID =  1;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public override int MaxNumberOfPlayers => 2;
		public override int MinNumberOfPlayers => 1;


		public override LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow)
		{
			return new LevelLogicCustomSettings();
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return TestLevel.CreatePlayingNew(levelSettings, level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForEditorLoading(ILevelManager level)
		{
			return TestLevel.CreateEditingLoading(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForNewLevel(ILevelManager level)
		{
			return TestLevel.CreateEditingNew(level);
		}

		public override LevelLogicInstancePlugin CreateInstanceForLoadingToPlaying(ILevelManager level)
		{
			return TestLevel.CreatePlayingLoading(level);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}
	}

	public class TestLevel : LevelInstancePluginBase
	{

		readonly Timeout updateResourcesTimeout;

		TestLevel(ILevelManager level)
			: base(level)
		{
			updateResourcesTimeout = new Timeout(1);
		}

		public static TestLevel CreatePlayingNew(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return new TestLevel(level);
		}

		public static TestLevel CreatePlayingLoading(ILevelManager level)
		{
			return new TestLevel(level);
		}

		public static TestLevel CreateEditingNew(ILevelManager level)
		{
			return new TestLevel(level);
		}

		public static TestLevel CreateEditingLoading(ILevelManager level)
		{
			return new TestLevel(level);
		}

		public override void OnUpdate(float timeStep)
		{
			if (updateResourcesTimeout.Update(timeStep, true)) {
				PackageUI.UpdateResourceDisplay(Level.HumanPlayer.Resources);
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
			PackageUI.UpdateResourceDisplay(Level.HumanPlayer.Resources);
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
			PackageUI.Dispose();
		}

	}
}
