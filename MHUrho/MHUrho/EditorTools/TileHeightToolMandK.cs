﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools
{
	class TileHeightToolMandK : TileHeightTool, IMandKTool {

		public override IEnumerable<Button> Buttons => new Button[0];

		const float Sensitivity = 0.01f;

		//private List<Button> buttons;
		GameMandKController input;
		MandKGameUI ui;

		StaticRectangleToolMandK highlight;

		bool enabled;
		bool mouseButtonDown;

		ITile centerTile;

		public TileHeightToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera)
			: base(input)
		{
			this.input = input;
			this.ui = ui;
			highlight = new StaticRectangleToolMandK(input, ui, camera, new IntVector2(3, 3));
		}

		public override void Enable() {
			if (enabled) return;

			
			input.MouseDown += MouseDown;
			input.MouseUp += MouseUp;
			input.MouseMove += MouseMove;
			highlight.Enable();
			//input.UIManager.SelectionBarShowButtons(buttons);
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			input.MouseDown -= MouseDown;
			input.MouseUp -= MouseUp;
			input.MouseMove -= MouseMove;
			highlight.Disable();
			enabled = false;
		}

		public override void ClearPlayerSpecificState() {

		}

		public override void Dispose() {
			Disable();
			highlight?.Dispose();
		}

		

		void MouseDown(MouseButtonDownEventArgs e) {
			if (ui.UIHovering) return;

			centerTile = input.GetTileUnderCursor();
			if (centerTile != null) {
				input.HideCursor();
				mouseButtonDown = true;
				highlight.FixHighlight(centerTile);
			}
		}

		void MouseUp(MouseButtonUpEventArgs e) {
			if (ui.UIHovering) return;

			if (centerTile != null) {
				input.ShowCursor(new Vector3(centerTile.Center.X, Map.GetTerrainHeightAt(centerTile.Center), centerTile.Center.Y));
				mouseButtonDown = false;
				centerTile = null;
				highlight.FreeHighlight();
			}
		}

		void MouseMove(MHUrhoMouseMovedEventArgs e) {
			if (mouseButtonDown) {
				Map.ChangeTileHeight(centerTile, highlight.Size, -e.DY * Sensitivity);
			}
		}

		
	}
}
