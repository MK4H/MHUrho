using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
    interface IPlayerLoader : ILoader
    {
		Player Player { get; }
    }
}
