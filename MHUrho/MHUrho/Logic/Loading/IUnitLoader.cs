using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
    interface IUnitLoader : ILoader
    {
		IUnit Unit { get; }
    }
}
