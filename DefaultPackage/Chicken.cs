using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace DefaultPackage
{
	public class ChickenType : UnitTypePluginBase {

		public UnitTypeInitializationData TypeData => new UnitTypeInitializationData();


		public override bool IsMyType(string unitTypeName) {
			return unitTypeName == "Chicken";
		}

		public ChickenType() {

		}



		public override UnitInstancePluginBase CreateNewInstance(ILevelManager level, Unit unit) {
			return new ChickenInstance(level, unit);
		}

		public override UnitInstancePluginBase GetInstanceForLoading() {
			return new ChickenInstance();
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return true;
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {

		}
	}

	public class ChickenInstance : UnitInstancePluginBase, WorldWalker.INotificationReciever, UnitSelector.INotificationReciever {


		public ChickenInstance() {

		}

		public ChickenInstance(ILevelManager level, Unit unit) 
			:base(level,unit)
		{

		}

		public override void SaveState(PluginDataWrapper pluginDataStorage) {

		}

		public override void LoadState(ILevelManager level, Unit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			this.Unit = unit;
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

		public void OnUnitOrderedToTile(UnitSelector selector, ITile targetTile, OrderArgs orderArgs) {
			orderArgs.Executed = false;
		}

		public void OnUnitOrderedToUnit(UnitSelector selector, Unit targetUnit, OrderArgs orderArgs) {
			orderArgs.Executed = false;
		}

		public void OnUnitOrderedToBuilding(UnitSelector selector, Building targetBuilding, OrderArgs orderArgs) {
			orderArgs.Executed = false;
		}

	}
}
