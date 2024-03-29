﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.EditorTools.MouseKeyboard;
using MHUrho.Input;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MouseKeyboard;
using ShowcasePackage.Buildings;
using ShowcasePackage.Units;
using Urho;

namespace ShowcasePackage
{
	class ToolManagerMouseKeyboard : ToolManager
	{
		readonly ILevelManager level;

		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover cameraMover;

		public ToolManagerMouseKeyboard(ILevelManager level)
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
			if (level.EditorMode) {
				LoadTool(new TerrainManipulatorTool(input, ui, cameraMover, new IntRect(0, 100, 50, 150)));
				LoadTool(new TileTypeTool(input, ui, cameraMover, new IntRect(0, 150, 50, 200)));
				LoadTool(new UnitSelectorTool(input, ui, cameraMover, new IntRect(0, 200, 50, 250)));
				LoadTool(new SpawnerTool(input, ui, cameraMover, new IntRect(0, 0, 50, 50)));
				LoadTool(new BuilderTool(input, ui, cameraMover, new IntRect(0, 50, 50, 100)));
			}
			else {
				LoadTool(new UnitSelectorTool(input, ui, cameraMover, new IntRect(0, 200, 50, 250)));
				LoadTool(new BuilderTool(input, ui, cameraMover, new IntRect(0, 50, 50, 100)));
			}
		}
	}
}
