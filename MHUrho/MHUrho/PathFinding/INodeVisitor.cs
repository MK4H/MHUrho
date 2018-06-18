using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.PathFinding
{
    public interface INodeVisitor {
		bool Visit(ITileNode source, ITileNode target, out float time);
		bool Visit(ITileNode source, IBuildingNode target, out float time);
		bool Visit(ITileNode source, ITileEdgeNode target, out float time);

		bool Visit(IBuildingNode source, ITileNode target, out float time);
		bool Visit(IBuildingNode source, IBuildingNode target, out float time);
		bool Visit(IBuildingNode source, ITileEdgeNode target, out float time);

		bool Visit(ITileEdgeNode source, ITileNode target, out float time);
		bool Visit(ITileEdgeNode source, IBuildingNode target, out float time);
		bool Visit(ITileEdgeNode source, ITileEdgeNode target, out float time);

		bool Visit(ITempNode source, ITileNode target, out float time);
		bool Visit(ITempNode source, IBuildingNode target, out float time);
		bool Visit(ITempNode source, ITileEdgeNode target, out float time);

	}
}
