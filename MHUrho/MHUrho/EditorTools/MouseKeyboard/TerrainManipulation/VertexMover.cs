using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Input.MouseKeyboard;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.EditorTools.MouseKeyboard.TerrainManipulation
{
    class VertexMover : TerrainManipulator
    {
		const float Sensitivity = 0.01f;

		readonly TerrainManipulatorTool tool;
		readonly VertexSelector selector;
		readonly IMap map;
		readonly GameController input;

		bool mouseDown;

		public VertexMover(TerrainManipulatorTool tool, VertexSelector selector, GameController input, IMap map)
		{
			this.tool = tool;
			this.selector = selector;
			this.map = map;
			this.input = input;
			this.mouseDown = false;
		}

		public override void Dispose()
		{
			selector.Dispose();
		}

		public override void OnEnabled()
		{
			map.HighlightCornerList(selector.SelectedVerticies, Color.Green);
		}

		public override void OnDisabled()
		{
			map.DisableHighlight();
			input.ShowCursor();
		}

		public override void OnMouseMoved(MHUrhoMouseMovedEventArgs args)
		{
			if (mouseDown) {
				map.ChangeHeight(selector.SelectedVerticies, -args.DY * Sensitivity);
				map.HighlightCornerList(selector.SelectedVerticies, Color.Green);
			}
		}

		public override void OnMouseDown(MouseButtonDownEventArgs args)
		{
			mouseDown = true;
			input.HideCursor();
		}

		public override void OnMouseUp(MouseButtonUpEventArgs args)
		{
			mouseDown = false;
			input.ShowCursor();
		}
	}
}
