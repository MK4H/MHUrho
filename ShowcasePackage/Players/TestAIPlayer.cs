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
using Urho;

namespace ShowcasePackage.Players
{
	public class TestAIPlayerType : PlayerAITypePlugin {

		public override int ID => 1;

		public override string Name => "TestAI";

		public UnitType Chicken { get; private set; }
		public UnitType TestUnit { get; private set; }
		public UnitType TestWorker { get; private set; }

		public BuildingType TestBuilding { get; private set; }

		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return TestAIPlayer.CreateNew(level, player,this);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return TestAIPlayer.GetInstanceForLoading(level, player, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			Chicken = package.GetUnitType("Chicken");
			TestUnit = package.GetUnitType("TestUnit");
			TestWorker = package.GetUnitType("TestWorker");
			TestBuilding = package.GetBuildingType("TestBuilding");
		}
	}

    public class TestAIPlayer : PlayerAIInstancePlugin {

		class ChickenWrapper {
			public const float DefaultWaitingTime = 4;

			public ChickenInstance Chicken { get; private set; }
			public IRangeTarget NextTarget { get; set; }

			public float TimeToNextTargetCheck { get; set; }

			public void TrySetTarget(IRangeTarget newTarget, IMap map)
			{
				if (Chicken.Shooter.Target != null) {
					return;
				}

				NextTarget = newTarget;
				if (!Chicken.Shooter.ShootAt(NextTarget)) {
					Chicken.Walker.GoTo(map.PathFinding.GetClosestNode(newTarget.CurrentPosition));
				}
				else {
					Chicken.Walker.Stop();
				}
			}

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

		int state = 0;
		float logicTimeout;

		public static TestAIPlayer CreateNew(ILevelManager level, IPlayer player, TestAIPlayerType type)
		{
			var instance = new TestAIPlayer(level, player, type) {spawnPoint = (level.Map.TopLeft + level.Map.BottomRight) / 2};

			return instance;
		}

		public static TestAIPlayer GetInstanceForLoading(ILevelManager level, IPlayer player, TestAIPlayerType type)
		{
			return new TestAIPlayer(level, player, type);
		}

		protected TestAIPlayer(ILevelManager level, IPlayer player, TestAIPlayerType type)
			:base(level, player)
		{
			this.type = type;
			chickens = new List<ChickenWrapper>();

		}

		public override void OnUpdate(float timeStep)
		{
			logicTimeout -= timeStep;
			if (logicTimeout > 0) return;
			logicTimeout = 2;
			switch (state) {
				case 0:
					state = 1;
					var spiralPoint = new Spiral(spawnPoint).GetEnumerator();
					spiralPoint.MoveNext();
					for (int i = 0; i < 1; i++, spiralPoint.MoveNext()) {
						IUnit newChicken = Level.SpawnUnit(type.Chicken, Level.Map.GetTileByMapLocation(spiralPoint.Current), Quaternion.Identity, Player);
						chickens.Add(new ChickenWrapper((ChickenInstance)newChicken.UnitPlugin));
					}

					var target = FindTargets(spawnPoint).FirstOrDefault();
					if (target == null) {
						return;
					}

					foreach (var chicken in chickens) {
						if (!chicken.Chicken.Shooter.ShootAt(target)) {
							chicken.Chicken.Walker.GoTo(Level.Map.PathFinding.GetClosestNode(target.CurrentPosition));
							chicken.NextTarget = target;
						}
					}
					break;
				case 1:
					var newTarget = FindTargets(spawnPoint).FirstOrDefault();
					if (newTarget == null) return;

					foreach (var chicken in chickens) {
						
						
					}
					break;
				default:
					throw new InvalidOperationException("Invalid state of logic");
			}
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var indexedData = pluginData.GetWriterForWrappedIndexedData();
			indexedData.Store(1, state);
			indexedData.Store(2, spawnPoint);
			indexedData.Store(3, from chicken in chickens select chicken.Chicken.Unit.ID);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var indexedData = pluginData.GetReaderForWrappedIndexedData();

			state = indexedData.Get<int>(1);
			spawnPoint = indexedData.Get<IntVector2>(2);
			chickens = new List<ChickenWrapper>(from unit in indexedData.Get<IEnumerable<int>>(3)
												select new ChickenWrapper((ChickenInstance)Level.GetUnit(unit).Plugin));

		}

		public override void Dispose()
		{

		}

		public override void OnUnitKilled(IUnit unit) {
			if (Level.IsEnding || unit.UnitType != type.Chicken) {
				//Do nothing
				return;
			}

			chickens.RemoveAll((chicken) => chicken.Chicken == unit.UnitPlugin);

			foreach (var point in new Spiral(spawnPoint)) {
				if (Level.Map.GetTileByMapLocation(point).Units.Count == 0) {
					IUnit newChicken = Level.SpawnUnit(type.Chicken, Level.Map.GetTileByMapLocation(point), Quaternion.Identity, Player);
					chickens.Add(new ChickenWrapper((ChickenInstance)newChicken.UnitPlugin));
					break;
				}
			}
		}

		IEnumerable<IRangeTarget> FindTargets(IntVector2 sourcePoint)
		{

			return from enemyPlayer in Player.GetEnemyPlayers()
					from unit in enemyPlayer.GetAllUnits()
					where unit.HasDefaultComponent<RangeTargetComponent>()
					orderby (unit.Position.XZ2() - sourcePoint.ToVector2()).Length
					select unit.GetDefaultComponent<RangeTargetComponent>();

		}		

	}
}
