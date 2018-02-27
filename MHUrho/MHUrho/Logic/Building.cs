using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Urho;
using MHUrho.Helpers;

namespace MHUrho.Logic
{
    class Building
    {
        Tile[] Tiles;
        List<Tile> Damaged;
        LevelManager level;
        Unit[] Workers;
        public IntRect Rectangle { get; private set; }

        public IntVector2 Location{ get { return Rectangle.TopLeft(); } }

        public IEnumerable<Tile> Active { get; private set; }

        

        public bool BuildAt(IntVector2 TopLeftCorner)
        {
            throw new NotImplementedException();
        }
    }
}