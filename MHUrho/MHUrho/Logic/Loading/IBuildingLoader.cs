using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
    interface IBuildingLoader : ILoader
    {
		IBuilding Building { get; }
    }
}
