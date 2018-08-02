using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.Plugins
{
    public abstract class PlayerAIInstancePlugin : InstancePlugin
    {

		protected IPlayer Player;

		protected PlayerAIInstancePlugin(ILevelManager level, IPlayer player)
			:base(level)
		{

			this.Player = player;
		}


		public virtual void OnBuildingDestroyed(IBuilding building)
		{

		}

		public virtual void OnUnitKilled(IUnit unit)
		{

		}
	}
}
