using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;

namespace MHUrho.WorldMap
{
	public interface IPathFindAlg {

		Path FindPath(Vector2 source,
					ITile target,
					CanGoToNeighbour canPassTo,
					GetMovementSpeed getMovementSpeed);

		List<ITile> GetTileList(Vector2 source,
									ITile target,
									CanGoToNeighbour canPassTo,
									GetMovementSpeed getMovementSpeed);
	}
}
