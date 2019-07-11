using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using ShowcasePackage.Buildings;

namespace ShowcasePackage.Players
{
	public class HumanPlayerType : PlayerAITypePlugin
	{
		public override int ID => 5;

		public override string Name => "HumanAI";

		public KeepType Keep { get; private set; }


		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return new HumanPlayer(level, player, this);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return new HumanPlayer(level, player, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			Keep = (KeepType)package.GetBuildingType(KeepType.TypeID).Plugin;
		}
	}

	class HumanPlayer : PlayerWithKeep {

		readonly HumanPlayerType type;

		public HumanPlayer(ILevelManager level, IPlayer player, HumanPlayerType myType)
			: base(level, player, myType.Keep)
		{
			this.type = myType;
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			Keep = GetKeep();
		}

		public override void Dispose()
		{

		}

		public override void Init(ILevelManager level)
		{
			Keep = GetKeep();
		}

		public override void BuildingDestroyed(IBuilding building)
		{
			if (Level.IsEnding)
			{
				return;
			}

			if (building == Keep.Building)
			{
				Player.RemoveFromLevel();
			}
		}
	}
}
