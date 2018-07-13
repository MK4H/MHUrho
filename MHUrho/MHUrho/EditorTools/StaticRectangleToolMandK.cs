using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools
{
	class StaticRectangleToolMandK : StaticRectangleTool, IMandKTool
	{
		public override IEnumerable<Button> Buttons => Enumerable.Empty<Button>();

		public IntVector2 Size { get; set; }

		readonly GameMandKController input;
		readonly MandKGameUI ui;
		readonly CameraMover camera;

		bool enabled;

		ITile fixedCenter;

		public StaticRectangleToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera, IntVector2 size)
			: base(input)
		{
			this.ui = ui;
			this.input = input;
			this.camera = camera;
			this.Size = size;
		}

		public override void Dispose() {

		}

		public override void Enable() {
			if (enabled) return;

			input.MouseMove += OnMouseMove;
			camera.OnFixedMove += OnCameraMove;
			enabled = true;
			
		}

		public override void ClearPlayerSpecificState() {

		}

		public override void Disable() {
			if (!enabled) return;

			input.MouseMove -= OnMouseMove;
			camera.OnFixedMove -= OnCameraMove;
			Map.DisableHighlight();
			enabled = false;
		}

		public void FixHighlight(ITile centerTile) {
			fixedCenter = centerTile;
		}

		public void FreeHighlight() {
			fixedCenter = null;
		}

		void OnCameraMove(Vector3 movement, Vector2 rotation, float timeStep)
		{
			if (ui.UIHovering || fixedCenter != null) return;

			var centerTile = input.GetTileUnderCursor();
			if (centerTile != null) {
				Map.HighlightRectangle(centerTile, Size, Color.Green);
			}
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e) {
			if (ui.UIHovering) return;

			if (fixedCenter != null) {
				Map.HighlightRectangle(fixedCenter, Size,  Color.Green);
				return;
			}

			var centerTile = input.GetTileUnderCursor();
			if (centerTile != null) {
				Map.HighlightRectangle(centerTile, Size, Color.Green);
			}

		}
	}
}
