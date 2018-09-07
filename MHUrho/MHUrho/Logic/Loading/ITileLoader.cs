using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
    interface ITileLoader : ILoader
    {
		ITile Tile { get; }
    }
}
