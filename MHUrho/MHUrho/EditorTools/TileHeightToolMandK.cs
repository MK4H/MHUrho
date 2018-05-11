using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools
{
	class TileHeightToolMandK : TileHeightTool, IMandKTool {

		public override IEnumerable<Button> Buttons => new Button[0];

		private const float Sensitivity = 0.01f;

		//private List<Button> buttons;
		private GameMandKController input;
		private Map Map => input.Level.Map;
		private StaticRectangleToolMandK highlight;

		private bool enabled;
		private bool mouseButtonDown;

		private ITile centerTile;

		public TileHeightToolMandK(GameMandKController input) {
			this.input = input;
			highlight = new StaticRectangleToolMandK(input, new IntVector2(3, 3));
		}

		public override void Enable() {
			if (enabled) return;

			
			input.MouseDown += MouseDown;
			input.MouseUp += MouseUp;
			input.MouseMove += MouseMove;
			highlight.Enable();
			//input.UIManager.SelectionBarShowButtons(buttons);
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			input.MouseDown -= MouseDown;
			input.MouseUp -= MouseUp;
			input.MouseMove -= MouseMove;
			highlight.Disable();
			enabled = false;
		}

		public override void ClearPlayerSpecificState() {

		}

		public override void Dispose() {
			Disable();
			highlight?.Dispose();
		}

		

		private void MouseDown(MouseButtonDownEventArgs e) {
			centerTile = input.GetTileUnderCursor();
			if (centerTile != null) {
				input.HideCursor();
				mouseButtonDown = true;
				highlight.FixHighlight(centerTile);
			}
		}

		private void MouseUp(MouseButtonUpEventArgs e) {
			if (centerTile != null) {
				input.ShowCursor(new Vector3(centerTile.Center.X, Map.GetTerrainHeightAt(centerTile.Center), centerTile.Center.Y));
				mouseButtonDown = false;
				centerTile = null;
				highlight.FreeHighlight();
			}
		}

		private void MouseMove(MouseMovedEventArgs e) {
			if (mouseButtonDown) {
				Map.ChangeTileHeight(centerTile, highlight.Size, -e.DY * Sensitivity);
			}
		}

		
	}
}
