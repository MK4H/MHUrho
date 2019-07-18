using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	/// <summary>
	/// Loader that loads units.
	/// </summary>
    interface IUnitLoader : ILoader
    {
		/// <summary>
		/// The loaded unit.
		/// </summary>
		IUnit Unit { get; }
    }
}
