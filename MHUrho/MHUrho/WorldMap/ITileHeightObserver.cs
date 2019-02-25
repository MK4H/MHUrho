using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using MHUrho.Logic;

namespace MHUrho.WorldMap
{
	public interface IColTileHeightObserver {
		void TileHeightsChanged(IReadOnlyCollection<ITile> tiles);
	}

	public interface IHashTileHeightObserver {
		void TileHeightsChanged(ImmutableHashSet<ITile> tiles);
	}
}
