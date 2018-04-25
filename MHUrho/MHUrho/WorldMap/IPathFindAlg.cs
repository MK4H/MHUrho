using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;

namespace MHUrho.WorldMap
{
	public interface IPathFindAlg {

		Path FindPath(ITile source,
					ITile target,
					CanGoToNeighbour canPassTo,
					GetMovementSpeed getMovementSpeed);

		List<ITile> GetTileList(ITile source,
									ITile target,
									CanGoToNeighbour canPassTo,
									GetMovementSpeed getMovementSpeed);
	}
}
