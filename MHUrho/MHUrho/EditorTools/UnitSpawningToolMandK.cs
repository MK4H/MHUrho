﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	class UnitSpawningToolMandK : UnitSpawningTool, IMandKTool {
		public override IEnumerable<Button> Buttons => unitTypeButtons.Keys;

		private Dictionary<Button, UnitType> unitTypeButtons;

		private GameMandKController input;
		private Map Map => input.Level.Map;

		private Button selected;

		private bool enabled;

		public UnitSpawningToolMandK(GameMandKController input) {

			this.input = input;
			this.unitTypeButtons = new Dictionary<Button, UnitType>();

			foreach (var unitType in PackageManager.Instance.ActiveGame.UnitTypes) {
				var unitIcon = unitType.Icon;

				var buttonTexture = new Texture2D();
				buttonTexture.FilterMode = TextureFilterMode.Nearest;
				buttonTexture.SetNumLevels(1);
				buttonTexture.SetSize(unitIcon.Width, unitIcon.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
				buttonTexture.SetData(unitIcon);



				var button = new Button();
				button.SetStyle("UnitButton");
				button.Size = new IntVector2(100, 100);
				button.HorizontalAlignment = HorizontalAlignment.Center;
				button.VerticalAlignment = VerticalAlignment.Center;
				button.Pressed += Button_Pressed;
				button.Texture = buttonTexture;
				button.FocusMode = FocusMode.ResetFocus;
				button.MaxSize = new IntVector2(100, 100);
				button.MinSize = new IntVector2(100, 100);
				button.Visible = false;

				unitTypeButtons.Add(button, unitType);
			}
		}

		public override void Enable() {
			if (enabled) return;

			input.UIManager.SelectionBarShowButtons(unitTypeButtons.Keys);
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			if (selected != null) {
				input.UIManager.Deselect();
				selected = null;
			}

			input.UIManager.SelectionBarClearButtons();
			input.MouseDown -= OnMouseDown;
			enabled = false;

		}

		public override void ClearPlayerSpecificState() {

		}

		public override void Dispose() {
			//TODO: Maybe dont disable, or change implementation of disable to not delete currently visible buttons
			Disable();
			foreach (var pair in unitTypeButtons) {
				pair.Key.Pressed -= Button_Pressed;
				pair.Key.Dispose();
			}
			unitTypeButtons = null;
		}

		private void Button_Pressed(PressedEventArgs e) {
			if (selected == e.Element) {
				input.UIManager.Deselect();
				selected = null;
				input.MouseDown -= OnMouseDown;
			}
			else {
				input.UIManager.SelectButton((Button)e.Element);
				selected = (Button)e.Element;
				input.MouseDown += OnMouseDown;
			}
		}

		private void OnMouseDown(MouseButtonDownEventArgs e) {
			if (selected != null) {
				var tile = input.GetTileUnderCursor();
				//TODO: Rectangle
				if (tile != null) {
					LevelManager.CurrentLevel.SpawnUnit(unitTypeButtons[selected], tile, input.Player);
				}
			}
		}
	}
}
