using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;

namespace MHUrho.Plugins
{
    public abstract class PlayerAITypePlugin : TypePlugin {

		/// <summary>
		/// Creates new instance plugin for freshly created player.
		/// </summary>
		/// <param name="level">The new level.</param>
		/// <param name="player">The platform representation of the player the new plugin will be controlling.</param>
		/// <returns>The new instance plugin.</returns>
		public abstract PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player);

		/// <summary>
		/// Creates new instance plugin for player with saved state for this type of plugin.
		/// </summary>
		/// <param name="level">The loaded level.</param>
		/// <param name="player">The platform representation of the player the new plugin will be controlling.</param>
		/// <returns>The new instance plugin.</returns>
		public abstract PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player);

	}
}
