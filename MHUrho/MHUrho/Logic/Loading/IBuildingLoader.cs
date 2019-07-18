using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	/// <summary>
	/// Loader that loads buildings.
	/// </summary>
    interface IBuildingLoader : ILoader
    {
		/// <summary>
		/// The loaded building.
		/// </summary>
		IBuilding Building { get; }
    }
}
