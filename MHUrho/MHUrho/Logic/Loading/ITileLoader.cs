using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	/// <summary>
	/// Loader that loads tiles.
	/// </summary>
    interface ITileLoader : ILoader
    {
		/// <summary>
		/// Loaded tile.
		/// </summary>
		ITile Tile { get; }
    }
}
