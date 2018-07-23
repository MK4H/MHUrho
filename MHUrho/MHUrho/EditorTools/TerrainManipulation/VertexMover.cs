using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.EditorTools.TerrainManipulation
{
    class VertexMover : TerrainManipulator
    {
		const float Sensitivity = 0.01f;

		readonly TerrainManipulatorTool tool;
		readonly VertexSelector selector;
		readonly IMap map;
		readonly GameMandKController input;

		public VertexMover(TerrainManipulatorTool tool, VertexSelector selector, GameMandKController input, IMap map)
		{
			this.tool = tool;
			this.selector = selector;
			this.map = map;
			this.input = input;
		}

		public override void Dispose()
		{
			selector.Dispose();
		}

		public override void OnEnabled()
		{
			input.HideCursor();
		}

		public override void OnMouseMoved(MHUrhoMouseMovedEventArgs e)
		{
			map.ChangeHeight(selector.SelectedVerticies, -e.DY * Sensitivity);
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			tool.DeselectManipulator();
		}
	}
}
