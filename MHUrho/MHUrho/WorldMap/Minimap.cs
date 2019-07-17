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

	/// <summary>
	/// Provides a 2D representation of the map
	/// </summary>
    public class Minimap : IDisposable {

		/// <summary>
		/// The 2D representation of the map
		/// </summary>
		public Texture2D Texture { get; private set; }

		float refreshRate;
		/// <summary>
		/// The frequency of updates of the state showed by minimap.
		/// </summary>
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

		readonly Image image;

		/// <summary>
		/// The level this minimap is displaying the state of.
		/// </summary>
		readonly ILevelManager level;

		/// <summary>
		/// Map of the level
		/// </summary>
		IMap Map => level.Map;

		/// <summary>
		/// In both width and height, because tiles are square even in the minimap
		/// </summary>
		int pixelsPerTile = 8;

		/// <summary>
		/// Position of the top left corner of the rectangle displayed by minimap
		///
		/// Top left as in the map meaning, left as lowest x coord, top as lowest z coord.
		/// </summary>
		IntVector2 topLeftPosition = new IntVector2(1,1);

		int TilesPerColumn => image.Height / pixelsPerTile;
		int TilesPerRow => image.Width / pixelsPerTile;

		/// <summary>
		/// Creates new instance of minimap to display the state of the <paramref name="level"/> updated
		/// <paramref name="refreshRate"/> times per second.
		/// </summary>
		/// <param name="level">The level to display by this minimap.</param>
		/// <param name="refreshRate">The number of updates per second.</param>
		public Minimap(ILevelManager level, float refreshRate)
		{
			image = new Image();
			image.SetSize(256, 256, 4);
			image.Clear(Color.Black);
			this.level = level;
			Texture = new Texture2D();
			Texture.SetData(image);

			RefreshRate = refreshRate;
			timeToRefresh = 1.0f / RefreshRate;
		}

		/// <summary>
		/// Trigger the update of the minimap.
		/// </summary>
		public void Refresh()
		{
			timeToRefresh = 1.0f / RefreshRate;
			MoveTo(level.Camera.PositionXZ.ToIntVector2());

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

		/// <summary>
		/// Step the timer to automatic refresh.
		/// </summary>
		/// <param name="timeStep">The time to step the timer.</param>
		public void OnUpdate(float timeStep)
		{
			timeToRefresh -= timeStep;
			if (timeToRefresh < 0) {
				Refresh();
			}
		}

		/// <summary>
		/// Move the displayed area to <paramref name="centerTileMapLocation"/> where <paramref name="centerTileMapLocation"/> will
		///  be displayed at the center of the minimap.
		/// </summary>
		/// <param name="centerTileMapLocation">The coordinates to display at the center of the minimap.</param>
		public void MoveTo(IntVector2 centerTileMapLocation)
		{
			topLeftPosition = centerTileMapLocation - new IntVector2(image.Width, image.Height) / (2 * pixelsPerTile);
		}

		/// <summary>
		/// Move the displayed area to have <paramref name="centerTile"/> displayed at the center of the minimap.
		/// </summary>
		/// <param name="centerTile">The tile to display at the center of the minimap.</param>
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

		/// <summary>
		/// Converts position on the minimap to a position in the world map.
		/// </summary>
		/// <param name="minimapPosition">The position on the minimap.</param>
		/// <returns>The position in the world map corresponding to the <paramref name="minimapPosition"/>.</returns>
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

		/// <summary>
		/// Converts coordinates from world position to a position on the minimap.
		/// </summary>
		/// <param name="worldPosition">The position in the world map to convert.</param>
		/// <returns>The position on the minimap corresponding to the <paramref name="worldPosition"/>.</returns>
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

		/// <summary>
		/// Releases all resources held by this instance.
		/// </summary>
		public void Dispose()
		{
			image.Dispose();
		}

		/// <summary>
		/// Gets the color to display on the minimap for the tile at <paramref name="tileMapLocation"/>
		/// </summary>
		/// <param name="tileMapLocation">The location of tile to get the color of.</param>
		/// <returns>RGBA color to display for the tile at <paramref name="tileMapLocation"/>.</returns>
		protected uint GetTileColor(IntVector2 tileMapLocation)
		{
			return GetTileColor(Map.GetTileByMapLocation(tileMapLocation));
		}

		/// <summary>
		/// Gets the color to display on the minimap for the <paramref name="tile"/>.
		/// </summary>
		/// <param name="tile">The tile to get the color of.</param>
		/// <returns>RGBA color to display for the <paramref name="tile"/>.</returns>
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


	}
}
