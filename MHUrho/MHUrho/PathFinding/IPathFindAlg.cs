using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;

namespace MHUrho.PathFinding
{
	public interface IPathFindAlg {

		Path FindPath(Vector2 source,
					ITile target,
					CanGoToNeighbour canPassFromTo,
					GetMovementSpeed getMovementSpeed,
					float maxMovementSpeed);

		List<ITile> GetTileList(Vector2 source,
								ITile target,
								CanGoToNeighbour canPassTo,
								GetMovementSpeed getMovementSpeed,
								float maxMovementSpeed);
	}
}
