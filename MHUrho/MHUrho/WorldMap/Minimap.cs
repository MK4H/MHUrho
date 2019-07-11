using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.WorldMap
{
    public class Minimap : IDisposable {

		public Texture2D Texture { get; private set; }

		float refreshRate;
		public float RefreshRate {
			get => refreshRate;
			set {
				if (value <= 0) {
					throw new InvalidOperationException("Refresh rate cannot be negative");
				}
				refreshRate = value;
			}
		}

		float timeToRefresh;

		Image image;

		readonly ILevelManager Level;

		IMap Map => Level.Map;

		/// <summary>
		/// In both width and height, because tiles are square even in the minimap
		/// </summary>
		int pixelsPerTile = 8;

		IntVector2 topLeftPosition = new IntVector2(1,1);

		int TilesPerColumn => image.Height / pixelsPerTile;
		int TilesPerRow => image.Width / pixelsPerTile;

		
		public Minimap(ILevelManager level, float refreshRate)
		{
			image = new Image();
			image.SetSize(256, 256, 4);
			image.Clear(Color.Black);
			this.Level = level;
			Texture = new Texture2D();
			Texture.SetData(image);

			RefreshRate = refreshRate;
			timeToRefresh = 1.0f / RefreshRate;
		}

		public void Refresh()
		{
			timeToRefresh = 1.0f / RefreshRate;
			MoveTo(Level.Camera.PositionXZ.ToIntVector2());

			unsafe {
				uint* imageData = (uint*)image.Data;

				/*
				 * FUTURE: try one access to tile and immediate fill of all the pixels for that tile
				 * FUTURE: try sequential fill of pixels, while every fill acesses the tile
				 * FUTURE: try sequential fill of pixels, while only the first fill acesses the tile
				*/

				//One axis has to be inverted, because what works on screen ([0,0] top left corner) does not work in the 3D world
				IntVector2 tileMapLocation = topLeftPosition + new IntVector2(0, TilesPerColumn - 1);
				int tilePixelXIndex = 0, tilePixelYIndex = 0;
				uint color = GetTileColor(tileMapLocation);
				for (int y = 0; y < image.Height; y++) {
					for (int x = 0; x < image.Width; x++) {
						*imageData++ = color;

						if (++tilePixelXIndex != pixelsPerTile) {
							continue;
						}
						else {
							tileMapLocation.X += 1;
							color = GetTileColor(tileMapLocation);
							tilePixelXIndex = 0;
						}

						
					}

					tileMapLocation.X = topLeftPosition.X;

					if (++tilePixelYIndex != pixelsPerTile) {
						color = GetTileColor(tileMapLocation);
						continue;
					}
					else {
						
						tileMapLocation.Y -= 1;
						color = GetTileColor(tileMapLocation);
						tilePixelYIndex = 0;
					}
					
				}
				
			}

			Texture.SetData(image);
		}

		public void OnUpdate(float timeStep)
		{
			timeToRefresh -= timeStep;
			if (timeToRefresh < 0) {
				Refresh();
			}
		}

		public void MoveTo(IntVector2 centerTileMapLocation)
		{
			topLeftPosition = centerTileMapLocation - new IntVector2(image.Width, image.Height) / (2 * pixelsPerTile);
		}

		public void MoveTo(ITile centerTile)
		{
			MoveTo(centerTile.MapLocation);
		}

		/// <summary>
		/// Changes the size of the displayed part of the map.
		/// </summary>
		/// <param name="times">How much zoom, + is zoom in, - is zoom out</param>
		/// <returns>true if it is possible to zoom further in the given direction, false if not</returns>
		public bool Zoom(int times)
		{
			pixelsPerTile = times >= 0 ? pixelsPerTile << times : pixelsPerTile >> -times;

			if (pixelsPerTile > image.Height) {
				pixelsPerTile = image.Height;
				return false;
			}

			if (pixelsPerTile < 1) {
				pixelsPerTile = 1;
				return false;
			}

			return true;
		}

		public Vector2? MinimapToWorld(IntVector2 minimapPosition)
		{
			IntVector2 tilesOffset = minimapPosition / pixelsPerTile;
			//Invert Y coord, because i made minimap wrong
			tilesOffset.Y = -tilesOffset.Y;

			IntVector2 minimapTopLeft = topLeftPosition + new IntVector2(0, TilesPerColumn - 1);

			IntVector2 targetTopLeftPosition = minimapTopLeft + tilesOffset;

			if (!Map.IsInside(targetTopLeftPosition)) {
				return null;
			}

			if (pixelsPerTile != 1) {
				IntVector2 offsetInTile = new IntVector2(minimapPosition.X % pixelsPerTile, minimapPosition.Y % pixelsPerTile);
				return targetTopLeftPosition.ToVector2() +
						new Vector2((1.0f / pixelsPerTile) * offsetInTile.X, (1.0f / pixelsPerTile) * offsetInTile.Y);
			}
			else {
				return Map.GetTileByTopLeftCorner(targetTopLeftPosition).Center;
			}
 
		}

		public IntVector2? WorldToMinimap(Vector2 worldPosition)
		{
			IntRect mapRectangle = new IntRect(topLeftPosition.X,
													topLeftPosition.Y,
													topLeftPosition.X + TilesPerRow - 1,
													topLeftPosition.Y + TilesPerColumn - 1);

			if (!mapRectangle.Contains(worldPosition)) {
				return null;
			}

			IntVector2 tileTopLeft = Map.GetContainingTile(worldPosition).TopLeft;

			Vector2 inTileOffset = new Vector2(worldPosition.X - tileTopLeft.X, -(worldPosition.Y - tileTopLeft.Y));

			IntVector2 minimapTopLeft = topLeftPosition + new IntVector2(0, TilesPerColumn - 1);

			IntVector2 minimapTileTopLeft = new IntVector2(tileTopLeft.X - minimapTopLeft.X, minimapTopLeft.Y - tileTopLeft.Y);

			return minimapTileTopLeft + (inTileOffset * pixelsPerTile).FloorToIntVector2();
		}

		protected uint GetTileColor(IntVector2 tileMapLocation)
		{
			return GetTileColor(Map.GetTileByMapLocation(tileMapLocation));
		}

		protected uint GetTileColor(ITile tile)
		{
			if (tile == null) {
				return Color.Black.ToUInt();
			}

			foreach (var unit in tile.Units) {
				if (unit != null) {
					return unit.Player.Insignia.Color.ToUInt();
				}
			}

			if (tile.Building != null) {
				return tile.Building.Player.Insignia.Color.ToUInt();
			}

			return tile.Type.MinimapColor.ToUInt();
		}

		public void Dispose()
		{
			image.Dispose();
		}
	}
}
