using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;

namespace MHUrho.PathFinding
{
	public class AStarFactory : IPathFindAlgFactory
	{
		public IPathFindAlg GetPathFindAlg(IMap map)
		{
			return new AStar(map);
		}
	}
}
