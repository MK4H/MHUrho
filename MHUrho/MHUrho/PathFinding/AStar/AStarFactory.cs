using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;

namespace MHUrho.PathFinding.AStar
{
	public class AStarFactory : IPathFindAlgFactory
	{
		public IPathFindAlg GetPathFindAlg(IMap map)
		{
			return new AStarAlg(map);
		}
	}
}
