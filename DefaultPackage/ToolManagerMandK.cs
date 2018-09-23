using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.UserInterface;

namespace DefaultPackage
{
	class ToolManagerMandK : ToolManager
	{
		readonly ILevelManager level;

		readonly GameMandKController input;
		readonly MandKGameUI ui;
		readonly CameraMover cameraMover;

		public ToolManagerMandK(ILevelManager level)
			:base(level.Input.UIManager)
		{
			this.level = level;
			if (level.Input.InputType != MHUrho.Input.InputType.MouseAndKeyboard) {
				throw new ArgumentException("Wrong input type for this toolManager", nameof(level));
			}

			input = (GameMandKController) level.Input;
			ui = (MandKGameUI) level.UIManager;
			cameraMover = level.Camera;
		}

		public override void LoadTools()
		{
			LoadTool(new TerrainManipulatorToolMandK(input, ui, cameraMover));
			LoadTool(new TileTypeToolMandK(input, ui, cameraMover));
			LoadTool(new UnitSelectorToolMandK(input, ui, cameraMover));
			LoadTool(new UnitSpawningToolMandK(input, ui, cameraMover));
			LoadTool(new BuildingBuilderToolMandK(input, ui, cameraMover));
		}
	}
}
