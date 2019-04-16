using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
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

		readonly StaticSquareTool highlight;

		readonly ExclusiveCheckBoxes checkBoxes;

		bool mouseButtonDown;
		bool enabled;

		public TileTypeTool(GameController input, GameUI ui, CameraMover camera)
			: base(input)
		{

			this.input = input;
			this.ui = ui;
			this.tileTypes = new Dictionary<CheckBox, TileType>();
			this.highlight = new StaticSquareTool(input, ui, camera, 3);
			this.checkBoxes = new ExclusiveCheckBoxes();

			foreach (var tileType in PackageManager.Instance.ActivePackage.TileTypes) {

				if (!tileType.IsManuallySpawnable) {
					continue;
				}

				var checkBox = ui.SelectionBar.CreateCheckBox();
				checkBox.SetStyle("SelectionBarCheckBox");
				checkBox.Toggled += OnTileTypeToggled;
				checkBox.Texture = PackageManager.Instance.ActivePackage.TileIconTexture;
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
			highlight.SquareChanged += Highlight_SquareChanged;
			enabled = true;
		}

		

		public override void Disable() {
			if (!enabled) return;

			checkBoxes.Hide();
			checkBoxes.Deselect();

			highlight.Disable();
			input.MouseDown -= OnMouseDown;
			input.MouseUp -= OnMouseUp;
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

		void Highlight_SquareChanged(Base.StaticSquareChangedArgs args)
		{
			if (ui.UIHovering) return;

			if (checkBoxes.Selected != null && mouseButtonDown) {
				Map.ChangeTileType(args.CenterTile, args.Size, tileTypes[checkBoxes.Selected]);
			}
		}

	

		void OnMouseUp(MouseButtonUpEventArgs e) {
			mouseButtonDown = false;
		}
	}
}
