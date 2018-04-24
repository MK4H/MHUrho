using System;
using System.Collections.Generic;
using System.Diagnostics;
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

	public class ChickenInstance : UnitInstancePluginBase, WorldWalker.INotificationReceiver, UnitSelector.INotificationReceiver {

		private AnimationController animationController;
		private WorldWalker walker;

		public ChickenInstance() {

		}

		public ChickenInstance(ILevelManager level, Unit unit) 
			:base(level,unit) {
			animationController = unit.Node.CreateComponent<AnimationController>();
			walker = WorldWalker.GetInstanceFor(this,level);
			unit.AddComponent(walker);
			unit.AddComponent(UnitSelector.CreateNew(this, level));
			unit.AlwaysVertical = true;
		}

		public override void SaveState(PluginDataWrapper pluginDataStorage) {

		}

		public override void LoadState(ILevelManager level, Unit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			this.Unit = unit;
			unit.AlwaysVertical = true;
			animationController = unit.Node.CreateComponent<AnimationController>();
			walker = unit.GetDefaultComponent<WorldWalker>();

		}

		public override bool CanGoFromTo(ITile fromTile, ITile toTile) {
			return toTile.Building == null;
		}

		public float GetMovementSpeed(ITile tile) {
			return 1;
		}

		public void OnMovementStarted(WorldWalker walker) {
			animationController.Play("Chicken/Models/Walk.ani", 0, true);
			animationController.SetSpeed("Chicken/Models/Walk.ani", 2);
		}

		public void OnMovementFinished(WorldWalker walker) {
			animationController.Stop("Chicken/Models/Walk.ani");
		}

		public void OnMovementFailed(WorldWalker walker) {
			animationController.Stop("Chicken/Models/Walk.ani");
		}

		public void OnUnitSelected(UnitSelector selector) {
			if (!walker.MovementStarted) {
				bool played = animationController.Play("Chicken/Models/Stand.ani", 0, true);
				Debug.Assert(played);
			}	
		}

		public void OnUnitDeselected(UnitSelector selector) {

		}

		public void OnUnitOrderedToTile(UnitSelector selector, ITile targetTile, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = walker.GoTo(targetTile);
		}

		public void OnUnitOrderedToUnit(UnitSelector selector, Unit targetUnit, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = false;
		}

		public void OnUnitOrderedToBuilding(UnitSelector selector, Building targetBuilding, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs) {
			orderArgs.Executed = false;
		}

	}
}
