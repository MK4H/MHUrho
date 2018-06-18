using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;

namespace MHUrho.PathFinding
{
	public interface IPathFindAlg {

		Path FindPath(Vector3 source,
					INode target,
					GetTime getTimeBetweenNodes,
					GetMinimalAproxTime getMinimalAproxTime);

		List<ITile> GetTileList(Vector3 source,
								INode target,
								GetTime getTimeBetweenNodes,
								GetMinimalAproxTime getMinimalAproxTime);

		INode GetClosestNode(Vector3 position);

		ITileNode GetTileNode(ITile tile);

		IBuildingNode CreateBuildingNode(IBuilding building, Vector3 position, object tag);
	}
}
