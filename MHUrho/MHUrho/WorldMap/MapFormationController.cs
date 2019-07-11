using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.DefaultComponents;
using Urho;

namespace MHUrho.WorldMap
{
    class MapFormationController : IFormationController
    {
		readonly IMap map;
		readonly Spiral.SpiralEnumerator spiral;
		readonly HashSet<UnitType> failedTypes;

		public MapFormationController(IMap map, ITile tile)
		{
			this.map = map;
			spiral = new Spiral(tile.MapLocation).GetSpiralEnumerator();
			spiral.MoveNext();

			failedTypes = new HashSet<UnitType>();
		}

		public bool MoveToFormation(UnitSelector unit)
		{
			if (failedTypes.Contains(unit.Unit.UnitType)) {
				return false;
			}

			var targetTile = map.GetTileByTopLeftCorner(spiral.Current);
			while (targetTile == null &&
					(spiral.ContainingSquareSize < map.Width ||
					spiral.ContainingSquareSize < map.Length))
			{
				//Move the spiral coords
				spiral.MoveNext();
			} 

			if (targetTile == null) {
				return false;
			}

			if (unit.Order(new MoveOrder(map.PathFinding.GetTileNode(targetTile)))) {
				//Move the spiral coords only on successfully filling the position
				spiral.MoveNext();
				return true;
			}

			failedTypes.Add(unit.Unit.UnitType);			
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
				if (MoveToFormation(units.Current)) {
					executed = true;
				}

				if (!units.TryMoveNext()) {
					break;
				}
			}
			return executed;
		}
		
	}
}
