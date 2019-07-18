using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Storage;

namespace MHUrho.Plugins
{
	/// <summary>
	/// Player plugin type used as a substitute for real player plugins during editing
	///
	/// Used so that we can assume that plugin will never be null in player implementation.
	/// </summary>
	class PlaceholderPlayerPluginType : PlayerAITypePlugin {
		/// <summary>
		/// Name of the placeholder player type plugin should never be used.
		/// </summary>
		public override string Name => "";
		/// <summary>
		/// Id of the placeholder player type plugin should never be used.
		/// </summary>
		public override int ID => 0;

		/// <summary>
		/// Creates new instance of placeholder instance plugin that does nothing.
		/// </summary>
		/// <param name="level">Level.</param>
		/// <param name="player">The controlled player.</param>
		/// <returns>Placeholder instance plugin.</returns>
		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return new PlaceholderPlayerPluginInstance(level, player);
		}

		/// <summary>
		/// Creates new instance of placeholder instance plugin that does nothing.
		/// </summary>
		/// <param name="level">Level.</param>
		/// <param name="player">The controlled player.</param>
		/// <returns>Placeholder instance plugin.</returns>
		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return new PlaceholderPlayerPluginInstance(level, player);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			//Nothing
		}
	}

	/// <summary>
	/// Instance of the placeholder player that holds the place of real player plugins during level editing
	///
	/// Used so that we can assume that plugin will never be null in player implementation.
	/// </summary>
	class PlaceholderPlayerPluginInstance : PlayerAIInstancePlugin {
		public PlaceholderPlayerPluginInstance(ILevelManager level, IPlayer player)
			: base(level, player)
		{ }

		public override void SaveState(PluginDataWrapper pluginData)
		{

		}

		public override void LoadState(PluginDataWrapper pluginData)
		{

		}

		public override void Dispose()
		{

		}

		public override void Init(ILevelManager level)
		{
			
		}
	}
}
