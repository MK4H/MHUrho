using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;

namespace MHUrho.WorldMap
{
    public interface IPathFindAlg {
        List<IntVector2> FindPath(IUnit unit, IntVector2 target);
    }
}
