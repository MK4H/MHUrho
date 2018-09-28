using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace DefaultPackage
{
	class TestHumanPlayer1Type : PlayerAITypePlugin
	{
		public override bool IsMyType(string typeName)
		{
			return string.Equals(typeName, "testHuman1", StringComparison.InvariantCultureIgnoreCase);
		}

		public override void Initialize(XElement extensionElement, GamePack package)
		{

		}

		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return new TestHumanPlayer1Instance(level, player);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return new TestHumanPlayer1Instance(level, player);
		}
	}

	class TestHumanPlayer1Instance : PlayerAIInstancePlugin {
		public TestHumanPlayer1Instance(ILevelManager level, IPlayer player)
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
	}
}
