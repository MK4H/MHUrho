using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Urho;
using MHUrho.Helpers;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{
    public class Building
    {
        Tile[] Tiles;
        LevelManager level;
        Unit[] Workers;

        public IntRect Rectangle { get; private set; }

        public IntVector2 Location => Rectangle.TopLeft();

        public static Building BuildAt(IntVector2 topLeftCorner, Map)
        {
            
        }
    }
}