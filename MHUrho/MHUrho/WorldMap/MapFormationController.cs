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
				//Move the spiral coords
				spiral.MoveNext();
				var targetTile = map.GetTileByTopLeftCorner(spiral.Current);

				if (targetTile != null && unit.Order(new MoveOrder(map.PathFinding.GetTileNode(targetTile)))) {
					return true;
				}


			}
			return false;
		}

		/// <summary>
		/// Creates a spiral around the provided tile
		/// </summary>
		/// <param name="units"></param>
		public bool MoveToFormation(UnitGroup units)
		{
			if (units == null) {
				return false;
			}



			bool executed = false;
			while (units.IsValid()) {
				if (!MoveToFormation(units.Current)) continue;

				executed = true;
				if (!units.TryMoveNext()) {
					break;
				}
			}
			return executed;
		}
		
	}
}
