using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;

namespace MHUrho.Plugins
{
    public abstract class PlayerAITypePlugin : TypePlugin {
		public abstract PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player);

		public abstract PlayerAIInstancePlugin GetInstanceForLoading();

	}
}
