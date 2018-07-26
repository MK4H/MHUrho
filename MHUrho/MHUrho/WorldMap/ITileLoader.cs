using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;

namespace MHUrho.WorldMap
{
    interface ITileLoader : ILoader
    {
		ITile Tile { get; }
    }
}
