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
using ShowcasePackage.Buildings;
using Urho;

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
			LoadTool(new TerrainManipulatorTool(input, ui, cameraMover, new IntRect(0, 100, 50, 150)));
			LoadTool(new TileTypeTool(input, ui, cameraMover, new IntRect(0, 150, 50, 200)));
			LoadTool(new UnitSelectorTool(input, ui, cameraMover, new IntRect(0, 200, 50, 250)));
			LoadTool(new UnitSpawningTool(input, ui, cameraMover, new IntRect(0, 0, 50, 50)));
			LoadTool(new BuilderTool(input, ui, cameraMover, new IntRect(0, 50, 50, 100)));
		}
	}
}
