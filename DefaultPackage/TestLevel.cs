using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho.Gui;

namespace DefaultPackage
{
	class TestLevel : LevelLogicPlugin
	{
		const string Name = "TestLogic";

		public override bool IsMyName(string logicName)
		{
			//TODO: Real check
			return true;
		}

		public override void OnUpdate(float timeStep)
		{
			
		}

		public override void LoadState(PluginDataWrapper fromPluginData)
		{
			//TODO: This
		}

		public override void SaveState(PluginDataWrapper toPluginData)
		{
			//TODO: This
		}

		public override void GetCustomSettings(Window customSettingsWindow)
		{
			//TODO: This
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
