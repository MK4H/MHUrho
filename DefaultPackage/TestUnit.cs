using System;
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

	public class TestUnitInstance : UnitInstancePlugin, WorldWalker.INotificationReceiver, UnitSelector.INotificationReceiver, Shooter.INotificationReceiver
	{
		WorldWalker walker;
		Shooter shooter;

		HealthBar healthbar;

		public TestUnitInstance() {

		}

		public TestUnitInstance(ILevelManager level, IUnit unit, ProjectileType projectileType)
			:base(level, unit)
		{
			this.walker = WorldWalker.GetInstanceFor(this, level);
			this.shooter = Shooter.CreateNew(this,
											level,
											projectileType,
											10);
			this.shooter.SearchForTarget = false;

			unit.AddComponent(walker);
			unit.AddComponent(UnitSelector.CreateNew(this, level));
			unit.AddComponent(shooter);

			Init(100);
		}

		public override void SaveState(PluginDataWrapper pluginDataStorage) {

		}

		public override void LoadState(ILevelManager level, IUnit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			walker = unit.GetDefaultComponent<WorldWalker>();
			shooter = unit.GetDefaultComponent<Shooter>();
		}

		public float MaxMovementSpeed => 100;

		public override void OnProjectileHit(IProjectile projectile)
		{
			throw new NotImplementedException();
		}

		public override void OnMeeleHit(IEntity byEntity)
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
						IRangeTarget rangeTarget;
						if (attackOrder.Target.Player != Unit.Player &&
							(rangeTarget = attackOrder.Target.GetDefaultComponent<RangeTargetComponent>()) != null) {
							order.Executed = shooter.ShootAt(rangeTarget);
						}
						
						break;
					case ShootOrder shootOrder:
						order.Executed = shooter.ShootAt(shootOrder.Target);
						break;
				}
			}
			
		}

		public Vector3 GetSourceOffset(Shooter shooter) {
			return new Vector3(0, 1, 0);
		}

		public void OnTargetAcquired(Shooter shooter) {

		}

		public void BeforeShotFired(Shooter shooter) {

		}

		public void AfterShotFired(Shooter shooter, IProjectile projectile) {

		}

		public void OnShotReloaded(Shooter shooter) {

		}

		public override void Dispose()
		{

		}

		void Init(float health)
		{
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 20, 0), new Vector2(0.8f, 0.4f));
			healthbar.SetHealth((int)health);
		}

	}
}
