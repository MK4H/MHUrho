﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace ShowcasePackage.Players
{
	public class TestHumanPlayer1Type : PlayerAITypePlugin
	{
		public override int ID => 5;

		public override string Name => "testHuman1";


		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return new TestHumanPlayer1Instance(level, player);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return new TestHumanPlayer1Instance(level, player);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}
	}

	class TestHumanPlayer1Instance : PlayerWithKeep {

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

		public override void Init(ILevelManager level)
		{
			
		}
	}
}