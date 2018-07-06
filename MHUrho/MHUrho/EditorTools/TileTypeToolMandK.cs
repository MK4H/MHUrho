using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	class TileTypeToolMandK : TileTypeTool, IMandKTool {
		public override IEnumerable<Button> Buttons => tileTypeButtons.Keys;

		Dictionary<Button, TileType> tileTypeButtons;

		GameMandKController input;

		StaticRectangleToolMandK highlight;

		Button selected;
		ITile centerTile;

		bool mouseButtonDown;
		bool enabled;

		public TileTypeToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera)
			: base(input)
		{

			this.input = input;
			this.tileTypeButtons = new Dictionary<Button, TileType>();
			this.highlight = new StaticRectangleToolMandK(input, ui, camera, new IntVector2(3,3));

			foreach (var tileType in PackageManager.Instance.ActiveGame.TileTypes) {
				var tileImage = tileType.GetImage().ConvertToRGBA();

				var buttonTexture = new Texture2D();
				buttonTexture.FilterMode = TextureFilterMode.Nearest;
				buttonTexture.SetNumLevels(1);
				buttonTexture.SetSize(tileImage.Width, tileImage.Height, Graphics.RGBAFormat, TextureUsage.Static);
				buttonTexture.SetData(tileImage);



				var button = new Button();
				button.SetStyle("TextureButton");
				button.Size = new IntVector2(100, 100);
				button.HorizontalAlignment = HorizontalAlignment.Center;
				button.VerticalAlignment = VerticalAlignment.Center;
				button.Pressed += Button_Pressed;
				button.Texture = buttonTexture;
				button.FocusMode = FocusMode.ResetFocus;
				button.MaxSize = new IntVector2(100, 100);
				button.MinSize = new IntVector2(100, 100);
				button.Visible = false;

				tileTypeButtons.Add(button, tileType);
			}
		}

		public override void Enable() {
			if (enabled) return;


			highlight.Enable();
			input.UIManager.SelectionBarShowButtons(tileTypeButtons.Keys);
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			if (selected != null) {
				input.UIManager.Deselect();
				selected = null;
			}

			highlight.Disable();
			input.UIManager.SelectionBarClearButtons();
			input.MouseDown -= OnMouseDown;
			input.MouseUp -= OnMouseUp;
			input.MouseMove -= OnMouseMove;
			enabled = false;
			
		}

		public override void ClearPlayerSpecificState() {

		}

		public override void Dispose() {
			//TODO: Maybe dont disable, or change implementation of disable to not delete currently visible buttons
			Disable();
			foreach (var pair in tileTypeButtons) {
				pair.Key.Pressed -= Button_Pressed;
				pair.Key.Dispose();
			}
			tileTypeButtons = null;
		}

		void Button_Pressed(PressedEventArgs e) {
			if (selected == e.Element) {
				input.UIManager.Deselect();
				selected = null;
				input.MouseDown -= OnMouseDown;
				input.MouseUp -= OnMouseUp;
				input.MouseMove -= OnMouseMove;
			}
			else {
				input.UIManager.SelectButton((Button)e.Element);
				selected = (Button)e.Element;
				input.MouseDown += OnMouseDown;
				input.MouseUp += OnMouseUp;
				input.MouseMove += OnMouseMove;
			}
		}

		void OnMouseDown(MouseButtonDownEventArgs e) {
			if (selected != null) {
				centerTile = input.GetTileUnderCursor();
				if (centerTile != null) {
					Map.ChangeTileType(centerTile, highlight.Size, tileTypeButtons[selected]);
				}
				mouseButtonDown = true;
			}
		}

		void OnMouseUp(MouseButtonUpEventArgs e) {
			if (selected != null) {
				mouseButtonDown = false;
			}
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e) {
			if (selected != null && mouseButtonDown) {
				var newCenterTile = input.GetTileUnderCursor();
				if (newCenterTile != null && newCenterTile != centerTile) {
					centerTile = newCenterTile;
					Map.ChangeTileType(centerTile, highlight.Size, tileTypeButtons[selected]);
				}
			}
		}

		
	}
}
