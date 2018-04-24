using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools
{
	class StaticRectangleToolMandK : StaticRectangleTool, IMandKTool
	{
		public override IEnumerable<Button> Buttons => Enumerable.Empty<Button>();

		public IntVector2 Size { get; set; }

		private GameMandKController input;
		private Map Map => input.LevelManager.Map;

		private bool enabled;

		private ITile fixedCenter;

		public StaticRectangleToolMandK(GameMandKController input, IntVector2 size) {
			this.input = input;
			this.Size = size;
		}

		public override void Dispose() {

		}

		public override void Enable() {
			if (enabled) return;

			input.MouseMove += OnMouseMove;
			enabled = true;
			
		}

		public override void ClearPlayerSpecificState() {

		}

		public override void Disable() {
			if (!enabled) return;

			input.MouseMove -= OnMouseMove;
			Map.DisableHighlight();
			enabled = false;
		}

		public void FixHighlight(ITile centerTile) {
			fixedCenter = centerTile;
		}

		public void FreeHighlight() {
			fixedCenter = null;
		}

		private void OnMouseMove(MouseMovedEventArgs e) {
			if (fixedCenter != null) {
				Map.HighlightArea(fixedCenter, Size, WorldMap.HighlightMode.Full, Color.Green);
				return;
			}

			var centerTile = input.GetTileUnderCursor();
			if (centerTile != null) {
				Map.HighlightArea(centerTile, Size, WorldMap.HighlightMode.Full, Color.Green);
			}

		}
	}
}
