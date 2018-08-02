using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.UserInterface;

namespace MHUrho.EditorTools
{
    public class ToolManagerMandK : ToolManager {

		readonly GameMandKController input;
		readonly MandKGameUI ui;
		readonly CameraMover cameraMover;

		public ToolManagerMandK(GameMandKController input, MandKGameUI ui, CameraMover cameraMover)
		{
			this.input = input;
			this.ui = ui;
			this.cameraMover = cameraMover;
			LoadTools();

			foreach (var tool in Tools) {
				ui.AddTool(tool);
			}
		}

		public override void DisableTools()
		{
			ui.DeselectTools();
		}

		//TODO: Parametrize which tools to load
		void LoadTools()
		{
			Tools.Add(new TerrainManipulatorToolMandK(input, ui, cameraMover));
			Tools.Add(new TileTypeToolMandK(input, ui, cameraMover));
			Tools.Add(new UnitSelectorToolMandK(input, ui, cameraMover));
			Tools.Add(new UnitSpawningToolMandK(input, ui, cameraMover));
			Tools.Add(new BuildingBuilderToolMandK(input, ui, cameraMover));
		}

		
	}
}
