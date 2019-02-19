using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.EditorTools.MandK.TerrainManipulation
{
    class TileHeightManipulator : TerrainManipulator
    {
		const float Sensitivity = 0.01f;

		readonly GameController input;
		readonly GameUI ui;
		readonly StaticSquareTool highlight;
		readonly IMap map;

		bool mouseButtonDown;

		ITile centerTile;

		public TileHeightManipulator(GameController input, GameUI ui, CameraMover camera, IMap map)
		{
			this.input = input;
			this.ui = ui;
			this.map = map;
			highlight = new StaticSquareTool(input, ui, camera, 3);
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
				map.ChangeTileHeight(centerTile, highlight.Size, -args.DY * Sensitivity);
			}
		}

		public override void Dispose()
		{
			highlight.Dispose();
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
