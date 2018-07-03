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

		ProjectileType projectileType;

		public override bool IsMyType(string unitTypeName) {
			return unitTypeName == "TestUnit";
		}

		public TestUnitType() {

		}

		

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit) {
			return new TestUnitInstance(level, unit, projectileType);
		}

		public override UnitInstancePlugin GetInstanceForLoading() {
			return new TestUnitInstance();
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return true;
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			projectileType = PackageManager.Instance
										   .ActiveGame
										   .GetProjectileType(XmlHelpers.GetString(XmlHelpers.GetChild(extensionElement, "projectileType")),
															  true);
		}
	}

	public class TestUnitInstance : UnitInstancePlugin, 
									WorldWalker.INotificationReceiver, 
									UnitSelector.INotificationReceiver,
									MovingMeeleAttacker.INotificationReceiver
	{
		WorldWalker walker;
		MovingMeeleAttacker meele;

		HealthBar healthbar;

		public TestUnitInstance() {

		}

		public TestUnitInstance(ILevelManager level, IUnit unit, ProjectileType projectileType)
			:base(level, unit)
		{
			this.walker = WorldWalker.GetInstanceFor(this, level);
			this.meele = MovingMeeleAttacker.CreateNew(this, level);

			unit.AddComponent(walker);
			unit.AddComponent(UnitSelector.CreateNew(this, level));


			Init(100);
		}

		public override void SaveState(PluginDataWrapper pluginDataStorage) {

		}

		public override void LoadState(ILevelManager level, IUnit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			walker = unit.GetDefaultComponent<WorldWalker>();
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
			throw new NotImplementedException();
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

		public void OnMovementStarted(WorldWalker walker) {

		}

		public void OnMovementFinished(WorldWalker walker) {

		}

		public void OnMovementFailed(WorldWalker walker) {

		}

		public void OnUnitSelected(UnitSelector selector) {

		}

		public void OnUnitDeselected(UnitSelector selector) {

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

		void Init(float health)
		{
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 20, 0), new Vector2(0.8f, 0.4f), health);
		}

		public bool IsInRange(MeeleAttacker attacker, IEntity target)
		{
			return Vector3.Distance(Unit.Position, target.Position) < 1;
		}

		public void Attacked(MeeleAttacker attacker, IEntity target)
		{
			target.HitBy(Unit);
		}

		public IEntity PickTarget(List<IEntity> possibleTargets)
		{
			return possibleTargets.Aggregate((e1, e2) => Vector3.Distance(Unit.Position, e1.Position) <
														Vector3.Distance(Unit.Position, e2.Position)
															? e1
															: e2);
		}

		public void MoveTo(Vector3 position)
		{
			walker.GoTo(Map.PathFinding.GetClosestNode(position));
		}
	}
}
