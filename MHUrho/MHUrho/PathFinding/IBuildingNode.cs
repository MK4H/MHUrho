using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;

namespace MHUrho.PathFinding
{
    public interface IBuildingNode : INode
    {
		IBuilding Building { get; }
    }
}
