using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools.MouseKeyboard.MapHighlighting;
using MHUrho.Input;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MouseKeyboard;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools.MouseKeyboard.MapHighlighting
{
	public class StaticSizeHighlighter : MHUrho.EditorTools.Base.MapHighlighting.StaticSizeHighlighter
	{
		public int EdgeSize { get; set; }

		public IntVector2 Size => new IntVector2(EdgeSize, EdgeSize);

		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover camera;

		bool enabled;

		ITile fixedCenter;
		ITile previousCenter;

		Color color;

		public StaticSizeHighlighter(GameController input, GameUI ui, CameraMover camera, int edgeSize, Color color)
			:base(input)
		{
			this.ui = ui;
			this.input = input;
			this.camera = camera;
			this.EdgeSize = edgeSize;
			this.color = color;
		}

		

		public override void Dispose()
		{
			Disable();
		}

		public override void Enable() {
			if (enabled) return;

			previousCenter = null;
			input.MouseMove += OnMouseMove;
			camera.CameraMoved += OnCameraMove;
			ui.HoverBegin += OnUIHoverBegin;

			enabled = true;
			
		}

		public override void Disable() {
			if (!enabled) return;

			input.MouseMove -= OnMouseMove;
			camera.CameraMoved -= OnCameraMove;
			ui.HoverBegin -= OnUIHoverBegin;
			Map.DisableHighlight();
			enabled = false;
		}

		public void FixHighlight(ITile centerTile) {
			fixedCenter = centerTile;
		}

		public void FreeHighlight() {
			fixedCenter = null;
		}

		void OnCameraMove(CameraMovedEventArgs args)
		{
			if (ui.UIHovering || fixedCenter != null) return;

			HighlightSquareAndSignal();
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e) {
			if (ui.UIHovering) return;

			if (fixedCenter != null) {
				Map.HighlightRectangle(fixedCenter, new IntVector2(EdgeSize, EdgeSize),  color);
				return;
			}

			HighlightSquareAndSignal();

		}

		void OnUIHoverBegin()
		{
			Map.DisableHighlight();
		}

		void HighlightSquareAndSignal()
		{
			var centerTile = input.GetTileUnderCursor();
			if (centerTile != null) {
				Map.HighlightRectangle(centerTile, new IntVector2(EdgeSize, EdgeSize), color);
				if (centerTile != previousCenter) {
					OnSquareChanged(centerTile, EdgeSize);
					previousCenter = centerTile;
				}
			}
		}
	}
}
