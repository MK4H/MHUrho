using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ShowcasePackage.Units;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.Control;
using MHUrho.DefaultComponents;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using ShowcasePackage.Buildings;
using Urho;

namespace ShowcasePackage.Players
{
	public class LazyPlayerType : PlayerAITypePlugin {

		public override int ID => 1;

		public override string Name => "LazyPlayer";

		public KeepType Keep { get; private set; }

		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return LazyPlayer.CreateNew(level, player,this);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return LazyPlayer.GetInstanceForLoading(level, player, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			Keep = (KeepType)package.GetBuildingType(KeepType.TypeID).Plugin;
		}
	}

    public class LazyPlayer : PlayerWithKeep {


		LazyPlayerType type;


		public static LazyPlayer CreateNew(ILevelManager level, IPlayer player, LazyPlayerType type)
		{
			var instance = new LazyPlayer(level, player, type);

			return instance;
		}

		public static LazyPlayer GetInstanceForLoading(ILevelManager level, IPlayer player, LazyPlayerType type)
		{
			return new LazyPlayer(level, player, type);
		}

		protected LazyPlayer(ILevelManager level, IPlayer player, LazyPlayerType type)
			:base(level, player, type.Keep)
		{
			this.type = type;
		}

		public override void OnUpdate(float timeStep)
		{
			
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{

		}

		public override void LoadState(PluginDataWrapper pluginData)
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

		public override void Dispose()
		{

		}

		public override void Init(ILevelManager level)
		{
			Keep = GetKeep();
		}
	}
}
