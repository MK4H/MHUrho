using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;

namespace MHUrho.PathFinding
{
    public interface ITileNode : INode
    {
		ITile Tile { get; }

		ITileEdgeNode GetEdgeNode(ITileNode neighbour);
	}
}
