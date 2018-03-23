using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
    [Flags]
    enum CollisionLayer {
        Unit = 1,
        Arrow = 2,
        Building = 4,
        Boulder = 8
    }
    
}
