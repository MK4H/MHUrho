using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Control
{
    /// <summary>
    /// Selection mask aplied when selecting things in the world
    /// </summary>
    public enum Select {    Nothing = 0,
                            MyUnits = 1,
                            FriendlyUnits = 2,          
                            EnemyUnits = 4,
                            NeutralUnits = 8,
                            MyBuildings = 16,
                            FriendlyBuildings = 32,
                            EnemyBuildings = 64,
                            NeutralBuildings = 128,
                            Tiles = 256
    }
}
