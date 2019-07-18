using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{
	/// <summary>
	/// Loader that loads maps.
	/// </summary>
    interface IMapLoader : ILoader
    {
		/// <summary>
		/// The loaded map.
		/// </summary>
		Map Map { get; }
    }
}
