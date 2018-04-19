using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Control
{
	public class FormationController
	{
		private readonly Map map;
		
		/// <summary>
		/// Creates a spiral around the provided tile
		/// </summary>
		/// <param name="units"></param>
		/// <param name="tile"></param>
		public virtual void MoveToFormation(IEnumerable<UnitSelector> units, ITile tile) {
			IntVector2 center = tile.MapLocation;
			IntVector2 spiralCoords = new IntVector2(0, 0);
			IntVector2 d = new IntVector2(0, -1);
			var unit = units.GetEnumerator();
			bool unitsLeft = unit.MoveNext();
			//TODO: Create cutoff if there is too many units, more than can fit the map
			while (unitsLeft) { 
				var targetTile = map.GetTileByTopLeftCorner(center + spiralCoords);
				if (targetTile != null && unit.Current.Order(targetTile,0,0)) {
					//If the unit was ordered, try ordering the next unit
					unitsLeft = unit.MoveNext();
				}
				
				//Move the spiral coords
				if (spiralCoords.X == spiralCoords.Y || 
					(spiralCoords.X < 0 && spiralCoords.X == -spiralCoords.Y) || 
					(spiralCoords.X > 0 && spiralCoords.X == 1 - spiralCoords.Y)) {
					int tmp = d.X;
					d.X = -d.Y;
					d.Y = tmp;
				}

				spiralCoords += d;
			}

			unit.Dispose();
		}

		public FormationController(Map map) {
			this.map = map;
		}

	}
}
