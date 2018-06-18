using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Plugins
{
    public abstract class LevelLogicPlugin {
		public abstract void OnUpdate(float timeStep);
	}
}
