using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Control;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.DefaultComponents;
using MHUrho.WorldMap;

namespace ShowcasePackage.Buildings
{
	abstract class SpiralFormationController : IFormationController
	{
		protected readonly Spiral Spiral;

		Dictionary<ITile, IBuildingNode> nodes;
		
		Spiral.SpiralEnumerator spiralPositions;
		IMap map;
		int maxSize;

		protected SpiralFormationController(Dictionary<ITile, IBuildingNode> nodes, ITile center, int squareSize, IMap map)
		{
			this.nodes = nodes;
			this.map = map;
			this.maxSize = squareSize;
			this.Spiral = new Spiral(center.MapLocation);
			spiralPositions = Spiral.GetSpiralEnumerator();
		}

		public bool MoveToFormation(UnitSelector unit)
		{
			bool executed = false;
			while (!executed &&
					spiralPositions.MoveNext() &&
					spiralPositions.ContainingSquareSize < maxSize &&
					nodes.TryGetValue(map.GetTileByMapLocation(spiralPositions.Current), out IBuildingNode buildingNode))
			{
				executed = unit.Order(new MoveOrder(buildingNode));
			}

			return executed;
		}

		public bool MoveToFormation(UnitGroup units)
		{
			bool executed = false;

			while (units.IsValid())
			{
				if (MoveToFormation(units.Current))
				{
					executed = true;
					units.TryMoveNext();
				}
				else if (spiralPositions.ContainingSquareSize >= maxSize) {
					executed = OverflowUnits(units);
				}
			}
			return executed;
		}

		protected abstract bool OverflowUnits(UnitGroup units);
	}
}
