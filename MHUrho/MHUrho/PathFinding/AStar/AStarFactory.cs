using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;

namespace MHUrho.PathFinding.AStar
{
	public class AStarFactory : IPathFindAlgFactory {

		readonly Visualization visualization;

		public AStarFactory(Visualization visualization = Visualization.None)
		{
			this.visualization = visualization;
		}

		public IPathFindAlg GetPathFindAlg(IMap map)
		{
			return new AStarAlg(map, visualization);
		}
	}
}
