﻿using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Logic
{
    interface IPathFindAlg {
        List<IntVector2> FindPath(IUnit unit, IntVector2 target);
    }
}
