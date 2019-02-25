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
					INodeDistCalculator nodeDistCalculator);

		List<ITile> GetTileList(Vector3 source,
								INode target,
								INodeDistCalculator nodeDistCalculator);

		INode GetClosestNode(Vector3 position);

		ITileNode GetTileNode(ITile tile);

		IBuildingNode CreateBuildingNode(IBuilding building, Vector3 position, object tag);

		ITempNode CreateTempNode(Vector3 position);
	}
}
