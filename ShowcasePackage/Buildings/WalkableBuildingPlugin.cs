using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.Plugins;

namespace ShowcasePackage.Buildings
{
	public abstract class WalkableBuildingPlugin : BuildingInstancePlugin
	{
		protected WalkableBuildingPlugin(ILevelManager level, IBuilding building)
			: base(level, building)
		{ }

		/// <summary>
		/// Gets walkable building node at the <paramref name="tile"/>, or null if there is none on the <paramref name="tile"/>.
		/// </summary>
		/// <param name="tile">The tile the node should be on.</param>
		/// <returns>Pathfinding node on the <paramref name="tile"/>, or null if there is none.</returns>
		public abstract IBuildingNode TryGetNodeAt(ITile tile);
	}
}
