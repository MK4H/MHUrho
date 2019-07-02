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
	public class TestNeutralPlayer1Type : PlayerAITypePlugin
	{
		public override string Name => "testNeutral1";
		public override int ID => 3;

		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return new TestNeutralPlayer1Instance(level, player);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return new TestNeutralPlayer1Instance(level, player);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}
	}

	class TestNeutralPlayer1Instance : PlayerAIInstancePlugin {
		public TestNeutralPlayer1Instance(ILevelManager level, IPlayer player)
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

	public class TestNeutralPlayer2Type : PlayerAITypePlugin {
		public override string Name => "testNeutral2";
		public override int ID => 4;


		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return new TestNeutralPlayer2Instance(level, player);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return new TestNeutralPlayer2Instance(level, player);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}
	}

	class TestNeutralPlayer2Instance : PlayerAIInstancePlugin {
		public TestNeutralPlayer2Instance(ILevelManager level, IPlayer player)
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
