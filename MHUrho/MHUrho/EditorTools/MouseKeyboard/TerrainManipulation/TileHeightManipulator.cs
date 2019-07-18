using System;
using System.Collections.Generic;
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

namespace MHUrho.EditorTools.MouseKeyboard.TerrainManipulation
{
    class TileHeightManipulator : TerrainManipulator
    {
		const float Sensitivity = 0.01f;
		const int MaxHighlightSize = 32;

		readonly GameController input;
		readonly GameUI ui;
		readonly StaticSizeHighlighter highlight;
		readonly IMap map;
		readonly UIElement uiElem;
		readonly Slider sizeSlider;

		bool mouseButtonDown;

		ITile centerTile;

		public TileHeightManipulator(GameController input, GameUI ui, CameraMover camera, IMap map)
		{
			this.input = input;
			this.ui = ui;
			this.map = map;
			InitUI(ui, out uiElem, out sizeSlider);
			highlight = new StaticSizeHighlighter(input, ui, camera, 3, Color.Green);
		}

		public override void OnMouseDown(MouseButtonDownEventArgs args)
		{ 

			centerTile = input.GetTileUnderCursor();
			if (centerTile != null) {
				input.HideCursor();
				mouseButtonDown = true;
				highlight.FixHighlight(centerTile);
			}
		}

		public override void OnMouseUp(MouseButtonUpEventArgs args)
		{

			if (centerTile != null) {
				input.ShowCursor(new Vector3(centerTile.Center.X, map.GetTerrainHeightAt(centerTile.Center), centerTile.Center.Y));
				mouseButtonDown = false;
				centerTile = null;
				highlight.FreeHighlight();
			}
		}

		public override void OnMouseMoved(MHUrhoMouseMovedEventArgs args)
		{

			if (mouseButtonDown) {
				map.ChangeTileHeight(centerTile, highlight.Size, -args.DeltaY * Sensitivity);
			}
		}

		public override void Dispose()
		{
			ui.UnregisterForHover(sizeSlider);
			highlight.Dispose();
			sizeSlider.Dispose();
			uiElem.Dispose();
		}

		public override void OnEnabled()
		{
			highlight.Enable();
			sizeSlider.SliderChanged += OnSliderChanged;
			ui.RegisterForHover(sizeSlider);
			uiElem.Visible = true;
			
		}

		public override void OnDisabled()
		{
			highlight.Disable();
			sizeSlider.SliderChanged -= OnSliderChanged;
			ui.UnregisterForHover(sizeSlider);
			uiElem.Visible = false;
		}

		static void InitUI(GameUI ui, out UIElement uiElem, out Slider sizeSlider)
		{
			if ((uiElem = ui.CustomWindow.GetChild("TileHeightManipulatorUI")) == null)
			{
				ui.CustomWindow.LoadLayout("UI/TileHeightManipulatorUI.xml");
				uiElem = ui.CustomWindow.GetChild("TileHeightManipulatorUI");
			}

			sizeSlider = (Slider)uiElem.GetChild("SizeSlider");
			//-1 due to lower bound being 0, so when we are reading the value, we are adding 1
			sizeSlider.Range = MaxHighlightSize - 1;

			uiElem.Visible = false;
		}

		void OnSliderChanged(SliderChangedEventArgs obj)
		{
			int sliderValue = (int)Math.Round(obj.Value);
			highlight.EdgeSize = 1 + sliderValue;
			((Slider)obj.Element).Value = sliderValue;
		}
	}
}
