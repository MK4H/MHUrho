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

		

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit) {
			return new TestUnitInstance(level, unit);
		}

		public override UnitInstancePlugin GetInstanceForLoading() {
			return new TestUnitInstance();
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return true;
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {

		}
	}

	public class TestUnitInstance : UnitInstancePlugin, 
									WorldWalker.IUser, 
									MovingMeeleAttacker.IUser
	{
		WorldWalker walker;
		MovingMeeleAttacker meele;

		HealthBar healthbar;
		float health;

		public TestUnitInstance() {

		}

		public TestUnitInstance(ILevelManager level, IUnit unit)
			:base(level, unit)
		{
			this.walker = WorldWalker.CreateNew(this, level);
			this.meele = MovingMeeleAttacker.CreateNew(this, level, true, new IntVector2(20,20),0.2f, 0.5f, 1);
			this.meele.Attacked += Attacked;
			this.meele.TargetInRange += TargetInRange;

			var selector = UnitSelector.CreateNew(level);
			selector.Ordered += OnUnitOrdered;

			unit.AddComponent(walker);
			unit.AddComponent(selector);
			unit.AddComponent(meele);

			health = 100;
			
			Init();
		}



		public override void SaveState(PluginDataWrapper pluginDataStorage) {

		}

		public override void LoadState(ILevelManager level, IUnit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			walker = unit.GetDefaultComponent<WorldWalker>();
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
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



		public bool GetTime(INode from, INode to, out float time)
		{
			time = (to.Position - from.Position).Length;
			return true;
		}

		public float GetMinimalAproximatedTime(Vector3 from, Vector3 to)
		{
			return (to - from).Length;
		}

		public void OnUnitOrdered(UnitSelector selector, Order order) {
			if (order.PlatformOrder) {
				switch (order) {
					case MoveOrder moveOrder:
						order.Executed = walker.GoTo(moveOrder.Target);
						break;
					case AttackOrder attackOrder:
						if (attackOrder.Target.Player != Unit.Player ) {
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


		public override void Dispose()
		{

		}

		void Init()
		{
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 1, 0), new Vector2(0.5f, 0.1f), health);
		}

		public bool IsInRange(MeeleAttacker attacker, IEntity target)
		{
			return Vector3.Distance(Unit.Position, target.Position) < 1;
		}

		public void Attacked(MeeleAttacker attacker, IEntity target)
		{
			target.HitBy(Unit);
		}

		public IUnit PickTarget(List<IUnit> possibleTargets)
		{
			return possibleTargets.Count == 0
						? null
						: possibleTargets.Aggregate((e1, e2) => Vector3.Distance(Unit.Position, e1.Position) <
																Vector3.Distance(Unit.Position, e2.Position)
																	? e1
																	: e2);
		}

		public void MoveTo(Vector3 position)
		{
			walker.GoTo(Map.PathFinding.GetClosestNode(position));
		}

		void TargetInRange(MeeleAttacker attacker, IEntity target)
		{
			walker.Stop();
		}

		public void GetMandatoryDelegates(out GetTime getTime, out GetMinimalAproxTime getMinimalAproximatedTime)
		{
			getTime = GetTime;
			getMinimalAproximatedTime = GetMinimalAproximatedTime;
		}

		public void GetMandatoryDelegates(out MoveTo moveTo, out IsInRange isInRange, out PickTarget pickTarget)
		{
			moveTo = MoveTo;
			isInRange = IsInRange;
			pickTarget = PickTarget;
		}
	}
}
