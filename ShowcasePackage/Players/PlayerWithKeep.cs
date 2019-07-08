using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Logic;
using MHUrho.Plugins;
using ShowcasePackage.Buildings;

namespace ShowcasePackage.Players
{
	public abstract class PlayerWithKeep : PlayerAIInstancePlugin
	{
		public  Keep Keep { get; protected set; }

		protected PlayerWithKeep(ILevelManager level, IPlayer player)
			: base(level, player)
		{ }
	}
}
