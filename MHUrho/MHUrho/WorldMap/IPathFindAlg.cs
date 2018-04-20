using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;

namespace MHUrho.WorldMap
{
	public interface IPathFindAlg {


		List<IntVector2> FindPath(ITile source,
								ITile target,
								CanGoToNeighbour canPassTo,
								GetMovementSpeed getMovementSpeed);

		List<IntVector2> FindPath(ITile source,
								IntVector2 targetCoords,
								CanGoToNeighbour canPassTo,
								GetMovementSpeed getMovementSpeed);

		List<IntVector2> FindPath(IntVector2 sourceCoords,
								ITile target,
								CanGoToNeighbour canPassTo,
								GetMovementSpeed getMovementSpeed);

		List<IntVector2> FindPath(IntVector2 sourceCoords,
								IntVector2 targetCoords, 
								CanGoToNeighbour canPassTo,
								GetMovementSpeed getMovementSpeed);
	}
}
