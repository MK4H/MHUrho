using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.Control;
using MHUrho.UnitComponents;
using MHUrho.Helpers;
using MHUrho.Packaging;
using Urho;

namespace DefaultPackage
{
	public class TestAIPlayerType : PlayerAITypePlugin {

		public UnitType Chicken { get; private set; }
		public UnitType TestUnit { get; private set; }
		public UnitType TestWorker { get; private set; }

		public BuildingType TestBuilding { get; private set; }

		public override bool IsMyType(string typeName)
		{
			return typeName == "TestAI";
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager)
		{
			Chicken = packageManager.ActiveGame.GetUnitType("Chicken");
			TestUnit = packageManager.ActiveGame.GetUnitType("TestUnit");
			TestWorker = packageManager.ActiveGame.GetUnitType("TestWorker");
			TestBuilding = packageManager.ActiveGame.GetBuildingType("TestBuilding");
		}

		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return new TestAIPlayer(level, player,this);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading()
		{
			return new TestAIPlayer(this);
		}
	}

    public class TestAIPlayer : PlayerAIInstancePlugin {

		class ChickenWrapper {
			public const float DefaultWaitingTime = 4;

			public ChickenInstance Chicken { get; private set; }
			public IRangeTarget NextTarget { get; set; }

			public float TimeToNextTargetCheck { get; set; }

			public ChickenWrapper(ChickenInstance chicken)
			{
				this.Chicken = chicken;
				NextTarget = null;
				TimeToNextTargetCheck = DefaultWaitingTime;
			}
		}

		TestAIPlayerType type;

		List<ChickenWrapper> chickens;
		IntVector2 spawnPoint;

		public TestAIPlayer(TestAIPlayerType type)
		{
			this.type = type;
			chickens = new List<ChickenWrapper>();
		}

		public TestAIPlayer(ILevelManager level, IPlayer player, TestAIPlayerType type)
			:this(type)
		{
			this.Level = level;
			this.Player = player;
			spawnPoint = (Map.TopLeft + Map.BottomRight) / 2;
		}

		public override void OnUpdate(float timeStep)
		{
			if (chickens.Count < 10 && Map.GetTileByMapLocation(spawnPoint).Units.Count == 0) {
				IUnit newChicken = Level.SpawnUnit(type.Chicken, Map.GetTileByMapLocation(spawnPoint), Player);
				chickens.Add(new ChickenWrapper((ChickenInstance)newChicken.Plugin));
			}
			
			foreach (var chickenInstance in chickens) {
				if (chickenInstance.Chicken.Shooter.Target != null || chickenInstance.NextTarget != null) continue;

				if (chickenInstance.TimeToNextTargetCheck > 0) {
					chickenInstance.TimeToNextTargetCheck -= timeStep;
				}
				chickenInstance.TimeToNextTargetCheck = ChickenWrapper.DefaultWaitingTime;


			   ITile foundTile = Map.FindClosestTile(chickenInstance.Chicken.Unit.Tile,
													(tile) => {
														return tile.Units.Any(unit => unit.Player != chickenInstance.Chicken.Unit.Player &&
																					unit.HasDefaultComponent<RangeTargetComponent>());
													});

				IRangeTarget target = foundTile?.Units
												.Where(unit => unit.Player != chickenInstance.Chicken.Unit.Player &&
																unit.HasDefaultComponent<RangeTargetComponent>())
												.Select(unit => unit.GetDefaultComponent<RangeTargetComponent>()).FirstOrDefault();

				if (target != null && !chickenInstance.Chicken.Shooter.ShootAt(target)) {
					chickenInstance.NextTarget = target;
					chickenInstance.Chicken.Walker.GoTo(chickenInstance.NextTarget.CurrentPosition.XZ2().RoundToIntVector2());
				}
			}
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			throw new NotImplementedException();
		}

		public override void LoadState(ILevelManager level, IPlayer player, PluginDataWrapper pluginData)
		{
			this.Level = level;
			this.Player = player;
			throw new NotImplementedException();
		}

		public override void OnUnitKilled(IUnit unit) {
			if (unit.UnitType != type.Chicken) {
				throw new InvalidOperationException("Wrong type of unit signaled");
			}

			chickens.RemoveAll((chicken) => chicken.Chicken == unit.Plugin);
		}

	
		

	}
}
