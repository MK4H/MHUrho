using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools.MandK.MapHighlighting;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools.MandK
{
	public class TileTypeTool : Base.TileTypeTool, IMandKTool { 

		Dictionary<CheckBox, TileType> tileTypes;

		readonly GameController input;
		readonly GameUI ui;

		readonly StaticSizeHighlighter highlight;

		readonly ExclusiveCheckBoxes checkBoxes;

		readonly UIElement uiElem;
		readonly Slider sizeSlider;

		bool mouseButtonDown;
		bool enabled;

		public TileTypeTool(GameController input, GameUI ui, CameraMover camera, IntRect iconRectangle)
			: base(input, iconRectangle)
		{

			this.input = input;
			this.ui = ui;
			this.tileTypes = new Dictionary<CheckBox, TileType>();
			this.highlight = new StaticSizeHighlighter(input, ui, camera, 3, Color.Green);
			this.checkBoxes = new ExclusiveCheckBoxes();
			InitUI(ui, out uiElem, out sizeSlider);

			foreach (var tileType in input.Level.Package.TileTypes) {

				var checkBox = ui.SelectionBar.CreateCheckBox();
				checkBox.SetStyle("SelectionBarCheckBox");
				checkBox.Toggled += OnTileTypeToggled;
				checkBox.Texture = input.Level.Package.TileIconTexture;
				checkBox.ImageRect = tileType.IconRectangle;
				checkBox.HoverOffset = new IntVector2(tileType.IconRectangle.Width(), 0);
				checkBox.CheckedOffset = new IntVector2(2 * tileType.IconRectangle.Width(), 0);

				tileTypes.Add(checkBox, tileType);
				checkBoxes.AddCheckBox(checkBox);
			}
		}

		public override void Enable() {
			if (enabled) return;

			checkBoxes.Show();
			uiElem.Visible = true;

			highlight.Enable();
			input.MouseDown += OnMouseDown;
			input.MouseUp += OnMouseUp;
			sizeSlider.SliderChanged += OnSliderChanged;
			highlight.SquareChanged += Highlight_SquareChanged;
			enabled = true;
		}

		

		public override void Disable() {
			if (!enabled) return;

			checkBoxes.Hide();
			checkBoxes.Deselect();
			uiElem.Visible = false;

			highlight.Disable();
			input.MouseDown -= OnMouseDown;
			input.MouseUp -= OnMouseUp;
			sizeSlider.SliderChanged -= OnSliderChanged;
			highlight.SquareChanged -= Highlight_SquareChanged;
			
			enabled = false;		
		}

		public override void ClearPlayerSpecificState() {

		}

		public override void Dispose() {
			//ALT: Maybe don't disable, or change implementation of disable to not delete currently visible buttons
			Disable();
			foreach (var pair in tileTypes) {
				pair.Key.Toggled -= OnTileTypeToggled;
				ui.SelectionBar.RemoveChild(pair.Key);
			}
			tileTypes = null;

			highlight.Dispose();
			checkBoxes.Dispose();
			sizeSlider.Dispose();
			uiElem.Dispose();
		}

		void OnTileTypeToggled(ToggledEventArgs e) {
		
		}

		void OnMouseDown(MouseButtonDownEventArgs e) {
			mouseButtonDown = true;

			if (ui.UIHovering) return;

			if (checkBoxes.Selected != null) {
				var centerTile = input.GetTileUnderCursor();
				if (centerTile != null) {
					Map.ChangeTileType(centerTile, highlight.Size, tileTypes[checkBoxes.Selected]);
				}
			}
		}

		void Highlight_SquareChanged(Base.MapHighlighting.StaticSquareChangedArgs args)
		{
			if (ui.UIHovering) return;

			if (checkBoxes.Selected != null && mouseButtonDown) {
				Map.ChangeTileType(args.CenterTile, args.Size, tileTypes[checkBoxes.Selected]);
			}
		}

	

		void OnMouseUp(MouseButtonUpEventArgs e) {
			mouseButtonDown = false;
		}

		static void InitUI(GameUI ui, out UIElement uiElem, out Slider sizeSlider)
		{
			if ((uiElem = ui.CustomWindow.GetChild("TileTypeToolUI")) == null) {
				ui.CustomWindow.LoadLayout("UI/TileTypeToolUI.xml");
				uiElem = ui.CustomWindow.GetChild("TileTypeToolUI");
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
