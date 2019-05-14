using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;

namespace ShowcasePackage.Levels
{
	public abstract class LevelPluginBase : LevelLogicInstancePlugin
	{
		Dictionary<IEntityType, double> buildingDamage;
		Dictionary<IEntityType, double> unitDamage;

		protected LevelPluginBase(ILevelManager level)
			: base(level)
		{ }
	}
}
