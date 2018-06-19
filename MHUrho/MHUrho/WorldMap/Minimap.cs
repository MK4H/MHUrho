using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MHUrho.Helpers;
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
			MoveTo(Level.Camera.CameraXZPosition.ToIntVector2());
			int tilesPerColumn = image.Height / pixelsPerTile;

			unsafe {
				uint* imageData = (uint*)image.Data;

				/*
				 * TODO: try one access to tile and immediate fill of all the pixels for that tile
				 * TODO: try sequential fill of pixels, while every fill acesses the tile
				 * TODO: try sequential fill of pixels, while only the first fill acesses the tile
				*/

				//One axis has to be inverted, because what works on screen ([0,0] top left corner) does not work in the 3D world
				IntVector2 tileMapLocation = topLeftPosition + new IntVector2(0, tilesPerColumn - 1);
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
				timeToRefresh = 1.0f / RefreshRate;
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

		public bool ZoomIn(int times = 1) {
			pixelsPerTile = pixelsPerTile << times;

			if (pixelsPerTile <= image.Height && pixelsPerTile > 0) return true;

			pixelsPerTile = image.Height;
			return false;
		}

		public bool ZoomOut(int times = 1)
		{
			pixelsPerTile = pixelsPerTile >> times;
			if (pixelsPerTile > 0) return true;
			pixelsPerTile = 1;
			return false;
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
					return unit.Player.Color.ToUInt();
				}
			}

			if (tile.Building != null) {
				return tile.Building.Player.Color.ToUInt();
			}

			return tile.Type.MinimapColor.ToUInt();
		}

		public void Dispose()
		{
			image.Dispose();
		}
	}
}
