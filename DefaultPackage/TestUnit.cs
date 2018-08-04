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
using MHUrho.Plugins;
using MHUrho.UnitComponents;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace DefaultPackage
{
	public class TestUnitType : UnitTypePlugin {

		public UnitTypeInitializationData TypeData => new UnitTypeInitializationData();


		public override bool IsMyType(string unitTypeName) {
			return unitTypeName == "TestUnit";
		}

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

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {

		}
	}

	public class TestUnitInstance : UnitInstancePlugin, 
									WorldWalker.IUser, 
									MovingMeeleAttacker.IUser,
									MovingRangeTarget.IUser
	{
		WorldWalker walker;
		MovingMeeleAttacker meele;

		HealthBar healthbar;
		float health;

		public static TestUnitInstance CreateNew(ILevelManager level, IUnit unit)
		{
			var plugin = new TestUnitInstance(level, unit);
			plugin.walker = WorldWalker.CreateNew(plugin, level);
			plugin.meele = MovingMeeleAttacker.CreateNew(plugin, level, true, new IntVector2(20, 20), 0.2f, 0.5f, 1);
			var selector = UnitSelector.CreateNew(level);
			var rangeTarget = MovingRangeTarget.CreateNew(plugin, level, new Vector3(0,0.25f,0));

			unit.AddComponent(plugin.walker);
			unit.AddComponent(selector);
			unit.AddComponent(plugin.meele);

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

		
		

		

	

		bool WorldWalker.IUser.GetTime(INode from, INode to, out float time)
		{
			time = (to.Position - from.Position).Length;
			return true;
		}

		float WorldWalker.IUser.GetMinimalAproxTime(Vector3 from, Vector3 to)
		{
			return (to - from).Length;
		}

		void OnUnitOrdered(UnitSelector selector, Order order)
		{
			if (order.PlatformOrder) {
				switch (order) {
					case MoveOrder moveOrder:
						order.Executed = walker.GoTo(moveOrder.Target);
						break;
					case AttackOrder attackOrder:
						if (attackOrder.Target.Player != Unit.Player) {
							meele.Attack(attackOrder.Target);
							order.Executed = true;
						}

						break;
					case ShootOrder shootOrder:
						order.Executed = false;
						break;
				}
			}

		}

		void Attacked(MeeleAttacker attacker, IEntity target)
		{
			target.HitBy(Unit);
		}

		

		void Init(UnitSelector selector, MovingMeeleAttacker meeleAttacker)
		{
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 1, 0), new Vector2(0.5f, 0.1f), health);
			selector.Ordered += OnUnitOrdered;
			meele.Attacked += Attacked;
			meele.TargetInRange += TargetInRange;

		}

		void TargetInRange(MeeleAttacker attacker, IEntity target)
		{
			walker.Stop();
		}

		void MovingMeeleAttacker.IUser.MoveTo(Vector3 position)
		{
			walker.GoTo(Map.PathFinding.GetClosestNode(position));
		}

		bool MeeleAttacker.IBaseUser.IsInRange(MeeleAttacker attacker, IEntity target)
		{
			return Vector3.Distance(Unit.Position, target.Position) < 1;
		}

		IUnit MeeleAttacker.IBaseUser.PickTarget(List<IUnit> possibleTargets)
		{
			return possibleTargets.Count == 0
						? null
						: possibleTargets.Aggregate((e1, e2) => Vector3.Distance(Unit.Position, e1.Position) <
																Vector3.Distance(Unit.Position, e2.Position)
																	? e1
																	: e2);
		}

		public IEnumerator<Waypoint> GetWaypoints(MovingRangeTarget target)
		{
			return walker.GetRestOfThePath(new Vector3(0,0.25f,0));
		}
	}
}
