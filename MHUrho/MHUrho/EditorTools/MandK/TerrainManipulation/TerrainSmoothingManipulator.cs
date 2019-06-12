using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools.MandK.MapHighlighting;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools.MandK.TerrainManipulation
{
	class TerrainSmoothingManipulator : TerrainManipulator {

		//https://en.wikipedia.org/wiki/Gaussian_blur
		readonly Matrix3 matrix = new Matrix3(1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f,
											2.0f / 16.0f, 4.0f / 16.0f, 2.0f / 16.0f,
											1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f);

		readonly GameController input;
		readonly GameUI ui;

		readonly UIElement uiElem;
		readonly Slider sizeSlider;
		readonly StaticSizeHighlighter highlight;
		readonly IMap map;

		bool mouseButtonDown;
		ITile smoothedCenter;

		public TerrainSmoothingManipulator(GameController input, GameUI ui, CameraMover camera, IMap map)
		{
			this.input = input;
			this.ui = ui;
			this.map = map;
			InitUI(ui, out uiElem, out sizeSlider);
			highlight = new StaticSizeHighlighter(input, ui, camera, 3, Color.Green);
		}

		public override void OnEnabled()
		{
			highlight.Enable();
			sizeSlider.SliderChanged += OnSliderChanged;
			uiElem.Visible = true;
		}

		

		public override void OnDisabled()
		{
			highlight.Disable();
			sizeSlider.SliderChanged -= OnSliderChanged;
			uiElem.Visible = false;
		}

		public override void OnMouseDown(MouseButtonDownEventArgs args)
		{
			mouseButtonDown = true;
			smoothedCenter = input.GetTileUnderCursor();
			if (smoothedCenter != null) {
				map.ChangeTileHeight(smoothedCenter, highlight.Size, CalculateTileHeight);
			}
		}

		public override void OnMouseMoved(MHUrhoMouseMovedEventArgs args)
		{
			MouseOrCameraMove();
		}

		public override void OnCameraMove(CameraMovedEventArgs args)
		{
			MouseOrCameraMove();
		}

		public override void OnMouseUp(MouseButtonUpEventArgs args)
		{
			mouseButtonDown = false;
		}

		public override void Dispose()
		{
			highlight.Dispose();
			sizeSlider.Dispose();
			uiElem.Dispose();
		}

		float CalculateTileHeight(float previousHeight, int x, int y)
		{
			//https://en.wikipedia.org/wiki/Gaussian_blur
			float result = 0;
			for (int dy = -1; dy < 2; dy++) {
				for (int dx = -1; dx < 2; dx++) {
					IntVector2 position = new IntVector2(x + dx, y + dy);
					float height = 0;
					if (map.IsInside(position)) {
						height = map.GetTerrainHeightAt(position);
					}

					result += height * matrix[dy + 1, dx + 1];
				}
			}

			return result;
		}

		void MouseOrCameraMove()
		{
			if (mouseButtonDown) {
				ITile centerTile = input.GetTileUnderCursor();
				if (centerTile == null) {
					return;
				}

				if (smoothedCenter != centerTile) {
					map.ChangeTileHeight(centerTile, highlight.Size, CalculateTileHeight);
					smoothedCenter = centerTile;
				}

			}
		}

		static void InitUI(GameUI ui, out UIElement uiElem, out Slider sizeSlider)
		{
			if ((uiElem = ui.CustomWindow.GetChild("SmoothingManipulatorUI")) == null)
			{
				ui.CustomWindow.LoadLayout("UI/SmoothingManipulatorUI.xml");
				uiElem = ui.CustomWindow.GetChild("SmoothingManipulatorUI");
			}

			sizeSlider = (Slider)uiElem.GetChild("SizeSlider");
		}

		void OnSliderChanged(SliderChangedEventArgs obj)
		{
			int sliderValue = (int)Math.Round(obj.Value);
			highlight.EdgeSize = 1 + sliderValue;
			((Slider)obj.Element).Value = sliderValue;
		}
	}
}
