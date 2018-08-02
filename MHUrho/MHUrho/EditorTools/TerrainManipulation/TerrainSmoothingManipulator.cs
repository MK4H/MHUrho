﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.EditorTools.TerrainManipulation
{
	class TerrainSmoothingManipulator : TerrainManipulator {

		//https://en.wikipedia.org/wiki/Gaussian_blur
		readonly Matrix3 matrix = new Matrix3(1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f,
											2.0f / 16.0f, 4.0f / 16.0f, 2.0f / 16.0f,
											1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f);

		readonly GameMandKController input;
		readonly MandKGameUI ui;
		readonly StaticSquareToolMandK highlight;
		readonly IMap map;

		bool mouseButtonDown;
		ITile smoothedCenter;

		public TerrainSmoothingManipulator(GameMandKController input, MandKGameUI ui, CameraMover camera, IMap map)
		{
			this.input = input;
			this.ui = ui;
			this.map = map;
			highlight = new StaticSquareToolMandK(input, ui, camera, 3);
		}

		public override void OnEnabled()
		{
			highlight.Enable();
		}

		public override void OnDisabled()
		{
			highlight.Disable();
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			mouseButtonDown = true;
			smoothedCenter = input.GetTileUnderCursor();
			map.ChangeTileHeight(smoothedCenter, highlight.Size, CalculateTileHeight);
		}

		public override void OnMouseMoved(MHUrhoMouseMovedEventArgs e)
		{
			if (mouseButtonDown) {
				ITile centerTile = input.GetTileUnderCursor();
				if (smoothedCenter != centerTile) {
					map.ChangeTileHeight(centerTile, highlight.Size, CalculateTileHeight);
					smoothedCenter = centerTile;
				}
				
			}
		}

		public override void OnMouseUp(MouseButtonUpEventArgs e)
		{
			mouseButtonDown = false;
		}

		public override void Dispose()
		{
			highlight.Dispose();
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
	}
}