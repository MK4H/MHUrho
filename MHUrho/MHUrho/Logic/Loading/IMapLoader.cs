using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{
    interface IMapLoader : ILoader
    {
		Map Map { get; }
    }
}
