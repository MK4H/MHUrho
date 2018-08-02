using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
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

		Dictionary<CheckBox, TileType> tileTypes;

		readonly GameMandKController input;
		readonly MandKGameUI ui;

		readonly StaticSquareToolMandK highlight;

		readonly ExclusiveCheckBoxes checkBoxes;

		ITile centerTile;

		bool mouseButtonDown;
		bool enabled;

		public TileTypeToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera)
			: base(input)
		{

			this.input = input;
			this.ui = ui;
			this.tileTypes = new Dictionary<CheckBox, TileType>();
			this.highlight = new StaticSquareToolMandK(input, ui, camera, 3);
			this.checkBoxes = new ExclusiveCheckBoxes();

			foreach (var tileType in PackageManager.Instance.ActiveGame.TileTypes) {

				if (!tileType.IsManuallySpawnable) {
					continue;
				}

				var checkBox = ui.SelectionBar.CreateCheckBox();
				checkBox.SetStyle("SelectionBarCheckBox");
				checkBox.Toggled += OnTileTypeToggled;
				checkBox.Texture = PackageManager.Instance.ActiveGame.TileIconTexture;
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

			highlight.Enable();
			input.MouseDown += OnMouseDown;
			input.MouseUp += OnMouseUp;
			input.MouseMove += OnMouseMove;
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			checkBoxes.Hide();
			checkBoxes.Deselect();

			highlight.Disable();
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
			foreach (var pair in tileTypes) {
				pair.Key.Toggled -= OnTileTypeToggled;
				ui.SelectionBar.RemoveChild(pair.Key);
			}
			tileTypes = null;

			highlight.Dispose();
			checkBoxes.Dispose();
		}

		void OnTileTypeToggled(ToggledEventArgs e) {
		
		}

		void OnMouseDown(MouseButtonDownEventArgs e) {
			if (ui.UIHovering) return;

			if (checkBoxes.Selected != null) {
				centerTile = input.GetTileUnderCursor();
				if (centerTile != null) {
					Map.ChangeTileType(centerTile, highlight.Size, tileTypes[checkBoxes.Selected]);
				}
				mouseButtonDown = true;
			}
		}

		void OnMouseUp(MouseButtonUpEventArgs e) {
			if (ui.UIHovering) return;

			if (checkBoxes.Selected != null) {
				mouseButtonDown = false;
			}
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e) {
			if (ui.UIHovering) return;

			if (checkBoxes.Selected != null && mouseButtonDown) {
				var newCenterTile = input.GetTileUnderCursor();
				if (newCenterTile != null && newCenterTile != centerTile) {
					centerTile = newCenterTile;
					Map.ChangeTileType(centerTile, highlight.Size, tileTypes[checkBoxes.Selected]);
				}
			}
		}

		
	}
}
