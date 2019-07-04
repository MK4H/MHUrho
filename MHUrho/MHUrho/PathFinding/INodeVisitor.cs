using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.PathFinding
{
    public interface INodeVisitor {
		void Visit(ITileNode source, ITileNode target, MovementType movementType);
		void Visit(ITileNode source, IBuildingNode target, MovementType movementType);
		void Visit(ITileNode source, ITempNode target, MovementType movementType);


		void Visit(IBuildingNode source, ITileNode target, MovementType movementType);
		void Visit(IBuildingNode source, IBuildingNode target, MovementType movementType);
		void Visit(IBuildingNode source, ITempNode target, MovementType movementType);


		void Visit(ITempNode source, ITileNode target, MovementType movementType);
		void Visit(ITempNode source, IBuildingNode target, MovementType movementType);
		void Visit(ITempNode source, ITempNode target, MovementType movementType);

	}
}
