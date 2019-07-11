using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace ShowcasePackage.Players
{
	public class NeutralPlayerType : PlayerAITypePlugin
	{
		public override string Name => "NeutralAI";
		public override int ID => 6;

		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return new NeutralPlayer(level, player);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return new NeutralPlayer(level, player);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}
	}

	class NeutralPlayer : PlayerAIInstancePlugin {
		public NeutralPlayer(ILevelManager level, IPlayer player)
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
