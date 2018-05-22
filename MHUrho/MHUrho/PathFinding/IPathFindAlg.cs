using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;

namespace MHUrho.PathFinding
{
	public interface IPathFindAlg {

		Path FindPath(Vector3 source,
					Vector3 target,
					GetTime getTimeBetweenNodes,
					GetMinimalAproxTime getMinimalAproxTime);

		List<ITile> GetTileList(Vector3 source,
								Vector3 target,
								GetTime getTimeBetweenNodes,
								GetMinimalAproxTime getMinimalAproxTime);

		INode GetNode(Vector3 position);
	}
}
