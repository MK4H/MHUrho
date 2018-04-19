using System;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.UnitComponents;
using MHUrho.Storage;
using Urho;

namespace DefaultPackage
{
	public class TestUnitType : UnitTypePluginBase {

		public UnitTypeInitializationData TypeData => new UnitTypeInitializationData();

		private ProjectileType projectileType;

		public override bool IsMyType(string unitTypeName) {
			return unitTypeName == "TestUnit";
		}

		public TestUnitType() {

		}

		

		public override UnitInstancePluginBase CreateNewInstance(ILevelManager level, Unit unit) {
			return new TestUnitInstance(level, unit, projectileType);
		}

		public override UnitInstancePluginBase GetInstanceForLoading() {
			return new TestUnitInstance();
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return true;
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			projectileType = PackageManager.Instance
										   .ActiveGame
										   .GetProjectileType(XmlHelpers.GetString(extensionElement, 
																				   "projectileType"),
															  true);
		}
	}

	public class TestUnitInstance : UnitInstancePluginBase, WorldWalker.INotificationReceiver, UnitSelector.INotificationReceiver, Shooter.INotificationReceiver
	{
		private ILevelManager level;
		private Node unitNode;
		private Unit unit;
		private WorldWalker walker;

		public TestUnitInstance() {

		}

		public TestUnitInstance(ILevelManager level, Unit unit, ProjectileType projectileType) {
			this.level = level;
			this.unitNode = unit.Node;
			this.unit = unit;
			this.walker = WorldWalker.GetInstanceFor(this, level);


			unitNode.AddComponent(walker);
			unitNode.AddComponent(UnitSelector.CreateNew(this, level));
			unitNode.AddComponent(Shooter.CreateNew(this,
													level,
													unit.Player,
													projectileType,
													10,
													1,
													1));

		}

		public override void SaveState(PluginDataWrapper pluginDataStorage) {

		}

		public override void LoadState(ILevelManager level,Unit unit, PluginDataWrapper pluginData) {
			this.level = level;
			this.unit = unit;
			this.unitNode = unit.Node;
			walker = unitNode.GetComponent<WorldWalker>();
		}

		public override bool CanGoFromTo(ITile fromTile, ITile toTile) {
			return toTile.Building == null;
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

		public void OnUnitOrderedToTile(UnitSelector selector, ITile targetTile, int buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = walker.GoTo(targetTile);
		}

		public void OnUnitOrderedToUnit(UnitSelector selector, Unit targetUnit, int buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = false;
		}

		public void OnUnitOrderedToBuilding(UnitSelector selector, Building targetBuilding, int buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = false;
		}

		public void OnTargetAcquired(Shooter shooter, Unit targetUnit) {

		}

		public void OnShotFired(Shooter shooter, Projectile projectile) {

		}

		public void OnShotReloaded(Shooter shooter) {

		}
	}
}
