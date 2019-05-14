using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.EditorTools.MandK;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MandK;

namespace ShowcasePackage
{
	class ToolManagerMandK : ToolManager
	{
		readonly ILevelManager level;

		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover cameraMover;

		public ToolManagerMandK(ILevelManager level)
			:base(level.Input.UIManager)
		{
			this.level = level;
			if (level.Input.InputType != MHUrho.Input.InputType.MouseAndKeyboard) {
				throw new ArgumentException("Wrong input type for this toolManager", nameof(level));
			}

			input = (GameController) level.Input;
			ui = (GameUI) level.UIManager;
			cameraMover = level.Camera;
		}

		public override void LoadTools()
		{
			LoadTool(new TerrainManipulatorTool(input, ui, cameraMover));
			LoadTool(new TileTypeTool(input, ui, cameraMover));
			LoadTool(new UnitSelectorTool(input, ui, cameraMover));
			LoadTool(new UnitSpawningTool(input, ui, cameraMover));
			LoadTool(new BuildingBuilderTool(input, ui, cameraMover));
		}
	}
}
