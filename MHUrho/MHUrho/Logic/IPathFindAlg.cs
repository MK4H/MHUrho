using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Logic
{
    public interface IPathFindAlg {
        List<IntVector2> FindPath(IUnit unit, IntVector2 target);
    }
}
