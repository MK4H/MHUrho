using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WaveEngine.Common.Math;

namespace MHUrho
{
    class Building
    {
        Tile[] Tiles;
        List<Tile> Damaged;
        LogicManager Level;
        Unit[] Workers;
        public Rectangle Rectangle { get; private set; }

        public Point Location{ get { return Rectangle.Location; } }

        public IEnumerable<Tile> Active { get; private set; }

        

        public bool BuildAt(Point TopLeftCorner)
        {
            throw new NotImplementedException();
        }
    }
}