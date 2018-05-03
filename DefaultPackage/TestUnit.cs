using System;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Packaging;
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

		}

		public override void SaveState(PluginDataWrapper pluginDataStorage) {

		}

		public override void LoadState(ILevelManager level, IUnit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			walker = unit.GetDefaultComponent<WorldWalker>();
			shooter = unit.GetDefaultComponent<Shooter>();
		}

		public override bool CanGoFromTo(ITile fromTile, ITile toTile) {
			var diff = toTile.MapLocation - fromTile.MapLocation;

			if (diff.X == 0 || diff.Y == 0) {
				return toTile.Building == null;
			}
			else {
				//Diagonal
				var tile1 = fromTile.Map.GetTileByMapLocation(fromTile.MapLocation + new IntVector2(diff.X, 0));
				var tile2 = fromTile.Map.GetTileByMapLocation(fromTile.MapLocation + new IntVector2(0, diff.Y));

				return tile1.Building == null && tile2.Building == null;
			}
			
		}

		public override void OnUpdate(float timeStep) {

		}

		public float GetMovementSpeed(ITile tile) {
			return 1;
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

		public void OnUnitOrderedToTile(UnitSelector selector, ITile targetTile, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
			switch (button) {
				case MouseButton.Left:
					orderArgs.Executed = walker.GoTo(targetTile);
					break;
				case MouseButton.Right:
					var rangeTarget = Map.GetRangeTarget(targetTile.Center3);
					orderArgs.Executed = shooter.ShootAt(rangeTarget);
					break;
			}
		}

		public void OnUnitOrderedToUnit(UnitSelector selector, IUnit targetUnit, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = false;
		}

		public void OnUnitOrderedToBuilding(UnitSelector selector, IBuilding targetBuilding, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = false;
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
	}
}
