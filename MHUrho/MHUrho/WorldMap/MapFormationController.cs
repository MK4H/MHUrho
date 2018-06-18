using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.UnitComponents;
using Urho;

namespace MHUrho.WorldMap
{
    class MapFormationController : IFormationController
    {
		readonly IMap map;
		readonly Spiral.SpiralEnumerator spiral;

		public MapFormationController(IMap map, ITile tile)
		{
			this.map = map;
			spiral = new Spiral(tile.MapLocation).GetSpiralEnumerator();
		}

		public bool MoveToFormation(UnitSelector unit)
		{
			while (spiral.ContainingSquareSize < map.Width || spiral.ContainingSquareSize < map.Length) {
				var targetTile = map.GetTileByTopLeftCorner(spiral.Current);
				if (targetTile != null && unit.Order(targetTile, MouseButton.Left, 0, 0)) {
					return true;
				}

				//Move the spiral coords
				spiral.MoveNext();
			}
			return false;
		}

		/// <summary>
		/// Creates a spiral around the provided tile
		/// </summary>
		/// <param name="units"></param>
		public void MoveToFormation(IEnumerator<UnitSelector> units)
		{
			if (units == null) {
				return;
			}

			while (units.MoveNext() && MoveToFormation(units.Current)) {

			}
		}
		
	}
}
