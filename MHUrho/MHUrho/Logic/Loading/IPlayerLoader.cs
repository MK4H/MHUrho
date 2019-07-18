using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	/// <summary>
	/// Loader that loads players.
	/// </summary>
    interface IPlayerLoader : ILoader
    {
		/// <summary>
		/// The loaded player.
		/// </summary>
		Player Player { get; }
    }
}
