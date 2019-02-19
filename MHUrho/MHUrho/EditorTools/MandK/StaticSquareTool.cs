using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools.MandK
{
	public class StaticSquareTool : MHUrho.EditorTools.Base.StaticSquareTool, IMandKTool
	{
		public int EdgeSize { get; set; }

		public IntVector2 Size => new IntVector2(EdgeSize, EdgeSize);

		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover camera;

		readonly Slider sizeSlider;

		bool enabled;

		ITile fixedCenter;
		ITile previousCenter;

		public StaticSquareTool(GameController input, GameUI ui, CameraMover camera, int edgeSize)
			: base(input)
		{
			this.ui = ui;
			this.input = input;
			this.camera = camera;
			this.EdgeSize = edgeSize;

			sizeSlider = ui.CustomWindow.CreateSlider("Size slider");
			sizeSlider.SetStyle("Slider");
			sizeSlider.Range = 20;
			sizeSlider.SliderChanged += SizeSlider_SliderChanged;
			sizeSlider.Height = 20;
			sizeSlider.Width = ui.CustomWindow.Width - 10;
			sizeSlider.VerticalAlignment = VerticalAlignment.Top;
			sizeSlider.HorizontalAlignment = HorizontalAlignment.Center;
			sizeSlider.Position = new IntVector2(0, 10);


			sizeSlider.Value = (edgeSize - 1);
		}

		

		public override void Dispose()
		{
			Disable();
			sizeSlider.SliderChanged -= SizeSlider_SliderChanged;
		}

		public override void Enable() {
			if (enabled) return;

			previousCenter = null;
			input.MouseMove += OnMouseMove;
			camera.CameraMoved += OnCameraMove;
			ui.HoverBegin += OnUIHoverBegin;

			sizeSlider.Visible = true;
			enabled = true;
			
		}

		public override void ClearPlayerSpecificState() {

		}

		public override void Disable() {
			if (!enabled) return;

			input.MouseMove -= OnMouseMove;
			camera.CameraMoved -= OnCameraMove;
			ui.HoverBegin -= OnUIHoverBegin;
			Map.DisableHighlight();
			sizeSlider.Visible = false;
			enabled = false;
		}

		public void FixHighlight(ITile centerTile) {
			fixedCenter = centerTile;
		}

		public void FreeHighlight() {
			fixedCenter = null;
		}

		void SizeSlider_SliderChanged(SliderChangedEventArgs obj)
		{
			int sliderValue = (int) Math.Round(obj.Value);
			EdgeSize = 1 + sliderValue;
			((Slider) obj.Element).Value = sliderValue;
		}

		void OnCameraMove(CameraMovedEventArgs args)
		{
			if (ui.UIHovering || fixedCenter != null) return;

			HighlightSquareAndSignal();
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e) {
			if (ui.UIHovering) return;

			if (fixedCenter != null) {
				Map.HighlightRectangle(fixedCenter, new IntVector2(EdgeSize, EdgeSize),  Color.Green);
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
				Map.HighlightRectangle(centerTile, new IntVector2(EdgeSize, EdgeSize), Color.Green);
				if (centerTile != previousCenter) {
					OnSquareChanged(centerTile, EdgeSize);
					previousCenter = centerTile;
				}
			}
		}
	}
}
