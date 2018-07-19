using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.EditorTools.TerrainManipulation
{
    class TileHeightManipulator : TerrainManipulator
    {
		const float Sensitivity = 0.01f;

		readonly GameMandKController input;
		readonly MandKGameUI ui;
		readonly StaticRectangleToolMandK highlight;
		readonly IMap map;

		bool mouseButtonDown;

		ITile centerTile;

		public TileHeightManipulator(GameMandKController input, MandKGameUI ui, CameraMover camera, IMap map)
		{
			this.input = input;
			this.ui = ui;
			this.map = map;
			highlight = new StaticRectangleToolMandK(input, ui, camera, new IntVector2(3, 3));
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{ 

			centerTile = input.GetTileUnderCursor();
			if (centerTile != null) {
				input.HideCursor();
				mouseButtonDown = true;
				highlight.FixHighlight(centerTile);
			}
		}

		public override void OnMouseUp(MouseButtonUpEventArgs e)
		{

			if (centerTile != null) {
				input.ShowCursor(new Vector3(centerTile.Center.X, map.GetTerrainHeightAt(centerTile.Center), centerTile.Center.Y));
				mouseButtonDown = false;
				centerTile = null;
				highlight.FreeHighlight();
			}
		}

		public override void OnMouseMoved(MHUrhoMouseMovedEventArgs e)
		{

			if (mouseButtonDown) {
				map.ChangeTileHeight(centerTile, highlight.Size, -e.DY * Sensitivity);
			}
		}

		public override void OnEnabled()
		{
			highlight.Enable();
		}

		public override void OnDisabled()
		{
			highlight.Disable();
		}
	}
}
