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
		public override string Name => "";
		public override int ID => 0;

		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return new PlaceholderPlayerPluginInstance(level, player);
		}

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
