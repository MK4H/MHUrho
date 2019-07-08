using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using ShowcasePackage.Buildings;
using ShowcasePackage.Misc;

namespace ShowcasePackage.Levels
{
	public abstract class LevelInstancePluginBase : LevelLogicInstancePlugin
	{
		public PackageUI PackageUI { get; protected set; }

		protected LevelInstancePluginBase(ILevelManager level)
			: base(level)
		{ }

		public bool IsRoofNode(IBuildingNode node)
		{
			return node.Tag == Gate.GateRoofTag ||
					node.Tag == Wall.WallTag ||
					node.Tag == Tower.TowerTag;
		}
	}
}
