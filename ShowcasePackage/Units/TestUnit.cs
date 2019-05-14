using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.EntityInfo;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.PathFinding.AStar;
using MHUrho.Plugins;
using MHUrho.UnitComponents;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace ShowcasePackage.Units
{
	public class TestUnitType : UnitTypePlugin {


		public override int ID => 1;

		public override string Name => "TestUnit";

		public TestUnitType() {

		}

		

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit)
		{
			return TestUnitInstance.CreateNew(level, unit);
		}

		public override UnitInstancePlugin GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return TestUnitInstance.GetInstanceForLoading(level, unit);
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return true;
		}

		public override void Initialize(XElement extensionElement, GamePack package) {

		}
	}

	public class TestUnitInstance : UnitInstancePlugin, 
									WorldWalker.IUser, 
									MovingMeeleAttacker.IUser,
									MovingRangeTarget.IUser,
									UnitSelector.IUser
	{
		class DistanceCalc : NodeDistCalculator {

			public override float GetMinimalAproxTime(Vector3 source, Vector3 target)
			{
				return (target - source).Length;
			}

			protected override bool GetTime(ITileNode source, ITileNode target, out float time)
			{
				Vector3 edgePosition = source.GetEdgePosition(target);
				time = (edgePosition - source.Position).Length + (target.Position - edgePosition).Length;
				return true;
			}

			protected override bool GetTime(ITileNode source, ITempNode target, out float time)
			{
				time = (target.Position - source.Position).Length;
				return true;
			}

			protected override bool GetTime(ITempNode source, ITileNode target, out float time)
			{
				time = (target.Position - source.Position).Length;
				return true;
			}
		}

		WorldWalker walker;
		MovingMeeleAttacker meele;

		HealthBar healthbar;
		float health;

		public static TestUnitInstance CreateNew(ILevelManager level, IUnit unit)
		{
			var plugin = new TestUnitInstance(level, unit);
			plugin.walker = WorldWalker.CreateNew(plugin, level);
			plugin.meele = MovingMeeleAttacker.CreateNew(plugin, level, true, new IntVector2(20, 20), 0.2f, 0.5f, 1);
			var selector = UnitSelector.CreateNew(plugin, level);
			MovingRangeTarget.CreateNew(plugin, level, new Vector3(0,0.25f,0));



			plugin.health = 100;

			plugin.Init(selector, plugin.meele);

			return plugin;
		}

		public static TestUnitInstance GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return new TestUnitInstance(level, unit);
		}

		protected TestUnitInstance(ILevelManager level, IUnit unit)
			:base(level, unit)
		{


		}



		public override void SaveState(PluginDataWrapper pluginDataStorage)
		{
			var writer = pluginDataStorage.GetWriterForWrappedSequentialData();

			writer.StoreNext(health);
		}

		public override void LoadState(PluginDataWrapper pluginData) {
			walker = Unit.GetDefaultComponent<WorldWalker>();
			meele = Unit.GetDefaultComponent<MovingMeeleAttacker>();

			var reader = pluginData.GetReaderForWrappedSequentialData();
			health = reader.GetNext<float>();

			Init(Unit.GetDefaultComponent<UnitSelector>(),
				meele);
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
			if (byEntity.Player == Unit.Player) {
				return;
			}

			health -= 10;

			if (health < 0) {
				Unit.RemoveFromLevel();
			}
			else {
				healthbar.SetHealth(health);
			}
			
		}

		public override void OnUpdate(float timeStep) {

		}

		public override void Dispose()
		{

		}

		bool UnitSelector.IUser.ExecuteOrder(Order order)
		{
			if (order.PlatformOrder)
			{
				switch (order)
				{
					case MoveOrder moveOrder:
						order.Executed = walker.GoTo(moveOrder.Target);
						break;
					case AttackOrder attackOrder:
						if (attackOrder.Target.Player != Unit.Player)
						{
							meele.Attack(attackOrder.Target);
							order.Executed = true;
						}

						break;
					case ShootOrder shootOrder:
						order.Executed = false;
						break;
				}
			}

			return order.Executed;
		}

		void OnAttacked(MeeleAttacker attacker, IEntity target)
		{
			target.HitBy(Unit);
		}

		

		void Init(UnitSelector selector, MovingMeeleAttacker meeleAttacker)
		{
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 1, 0), new Vector2(0.5f, 0.1f), health);
			meele.Attacked += OnAttacked;
			meele.TargetInRange += OnTargetInRange;

		}

		void OnTargetInRange(MeeleAttacker attacker, IEntity target)
		{
			walker.Stop();
		}

		void MovingMeeleAttacker.IUser.MoveTo(Vector3 position)
		{
			walker.GoTo(Level.Map.PathFinding.GetClosestNode(position));
		}

		bool MeeleAttacker.IBaseUser.IsInRange(MeeleAttacker attacker, IEntity target)
		{
			return Vector3.Distance(Unit.Position, target.Position) < 1;
		}

		IUnit MeeleAttacker.IBaseUser.PickTarget(ICollection<IUnit> possibleTargets)
		{
			return possibleTargets.Count == 0
						? null
						: possibleTargets.Aggregate((e1, e2) => Vector3.Distance(Unit.Position, e1.Position) <
																Vector3.Distance(Unit.Position, e2.Position)
																	? e1
																	: e2);
		}

		public IEnumerable<Waypoint> GetFutureWaypoints(MovingRangeTarget target)
		{
			return walker.GetRestOfThePath(new Vector3(0,0.25f,0));
		}

		INodeDistCalculator WorldWalker.IUser.GetNodeDistCalculator()
		{
			return new DistanceCalc();
		}
	}
}
