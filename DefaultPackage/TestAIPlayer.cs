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

		int state = 0;
		float logicTimeout;

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
			logicTimeout -= timeStep;
			if (logicTimeout > 0) return;
			logicTimeout = 2;
			switch (state) {
				case 0:
					state = 1;
					var spiralPoint = new Spiral(spawnPoint).GetEnumerator();
					spiralPoint.MoveNext();
					for (int i = 0; i < 1; i++, spiralPoint.MoveNext()) {
						IUnit newChicken = Level.SpawnUnit(type.Chicken, Map.GetTileByMapLocation(spiralPoint.Current), Player);
						chickens.Add(new ChickenWrapper((ChickenInstance)newChicken.UnitPlugin));
					}

					var target = FindTargets(spawnPoint).FirstOrDefault();
					if (target == null) {
						return;
					}

					foreach (var chicken in chickens) {
						if (!chicken.Chicken.Shooter.ShootAt(target)) {
							chicken.Chicken.Walker.GoTo(Map.PathFinding.GetClosestNode(target.CurrentPosition));
							chicken.NextTarget = target;
						}
					}
					break;
				case 1:
					var newTarget = FindTargets(spawnPoint).FirstOrDefault();
					if (newTarget == null) return;
					foreach (var chicken in chickens) {
						if (chicken.Chicken.Shooter.Target != null) {
							continue;
						}

						chicken.NextTarget = newTarget;
						if (!chicken.Chicken.Shooter.ShootAt(chicken.NextTarget)) {
							chicken.Chicken.Walker.GoTo(Map.PathFinding.GetClosestNode(newTarget.CurrentPosition));
						}
						else {
							chicken.Chicken.Walker.Stop();
						}
						
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

		public override void LoadState(ILevelManager level, IPlayer player, PluginDataWrapper pluginData)
		{
			var indexedData = pluginData.GetReaderForWrappedIndexedData();

			this.Level = level;
			this.Player = player;

			state = indexedData.Get<int>(1);
			spawnPoint = indexedData.Get<IntVector2>(2);
			chickens = new List<ChickenWrapper>(from unit in indexedData.Get<IEnumerable<int>>(3)
												select new ChickenWrapper((ChickenInstance)level.GetUnit(unit).Plugin));

		}

		public override void OnUnitKilled(IUnit unit) {
			if (unit.UnitType != type.Chicken) {
				throw new InvalidOperationException("Wrong type of unit signaled");
			}

			chickens.RemoveAll((chicken) => chicken.Chicken == unit.UnitPlugin);

			foreach (var point in new Spiral(spawnPoint)) {
				if (Map.GetTileByMapLocation(point).Units.Count == 0) {
					IUnit newChicken = Level.SpawnUnit(type.Chicken, Map.GetTileByMapLocation(point), Player);
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
