using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;

namespace MHUrho.PathFinding
{
	public interface IPathFindAlgFactory {
		IPathFindAlg GetPathFindAlg(IMap map);
	}
}
