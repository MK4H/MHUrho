using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.PathFinding
{
    public interface INodeVisitor {
		void Visit(ITileNode source, ITileNode target);
		void Visit(ITileNode source, IBuildingNode target);
		void Visit(ITileNode source, ITempNode target);


		void Visit(IBuildingNode source, ITileNode target);
		void Visit(IBuildingNode source, IBuildingNode target);
		void Visit(IBuildingNode source, ITempNode target);


		void Visit(ITempNode source, ITileNode target);
		void Visit(ITempNode source, IBuildingNode target);
		void Visit(ITempNode source, ITempNode target);

	}
}
