using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using ShowcasePackage.Buildings;

namespace ShowcasePackage.Levels
{
	public abstract class LevelInstancePluginBase : LevelLogicInstancePlugin
	{
		Dictionary<IEntityType, double> buildingDamage;
		Dictionary<IEntityType, double> unitDamage;

		protected LevelInstancePluginBase(ILevelManager level)
			: base(level)
		{ }

		public bool IsRoofNode(IBuildingNode node)
		{
			return node.Tag == GateInstance.GateRoofTag ||
					node.Tag == Wall.WallTag ||
					node.Tag == Tower.TowerTag;
		}
	}
}
