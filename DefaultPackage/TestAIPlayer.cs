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
			return;

			logicTimeout -= timeStep;
			if (logicTimeout > 0) return;
			logicTimeout = 2;
			switch (state) {
				case 0:
					state = 1;
					var spiralPoint = new Spiral(spawnPoint).GetEnumerator();
					spiralPoint.MoveNext();
					for (int i = 0; i < 10; i++, spiralPoint.MoveNext()) {
						IUnit newChicken = Level.SpawnUnit(type.Chicken, Map.GetTileByMapLocation(spiralPoint.Current), Player);
						chickens.Add(new ChickenWrapper((ChickenInstance)newChicken.Plugin));
					}

					var target = FindTargets(spawnPoint).FirstOrDefault();
					if (target == null) {
						return;
					}

					foreach (var chicken in chickens) {
						if (!chicken.Chicken.Shooter.ShootAt(target)) {
							chicken.Chicken.Walker.GoTo(target.CurrentPosition.XZ2().RoundToIntVector2());
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
							chicken.Chicken.Walker.GoTo(chicken.NextTarget.CurrentPosition.XZ2().RoundToIntVector2());
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

			foreach (var point in new Spiral(spawnPoint)) {
				if (Map.GetTileByMapLocation(point).Units.Count == 0) {
					IUnit newChicken = Level.SpawnUnit(type.Chicken, Map.GetTileByMapLocation(point), Player);
					chickens.Add(new ChickenWrapper((ChickenInstance)newChicken.Plugin));
					break;
				}
			}
		}

		IEnumerable<IRangeTarget> FindTargets(IntVector2 sourcePoint)
		{

			return from tile in Map.GetTilesInSpiral(Map.GetTileByMapLocation(sourcePoint))
					from unit in tile.Units
					where unit.Player != Player && unit.HasDefaultComponent<RangeTargetComponent>()
					select unit.GetDefaultComponent<RangeTargetComponent>();
		}		

	}
}
