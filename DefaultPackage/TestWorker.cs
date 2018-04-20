using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace DefaultPackage
{
	public class TestWorkerType : UnitTypePluginBase {
		public UnitTypeInitializationData TypeData => new UnitTypeInitializationData();

		public override bool IsMyType(string typeName) {
			return typeName == "TestWorker";
		}

		public override UnitInstancePluginBase CreateNewInstance(ILevelManager level, Unit unit) {
			return new TestWorkerInstance(level, unit);
		}

		public override UnitInstancePluginBase GetInstanceForLoading() {
			return new TestWorkerInstance();
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return centerTile.Type != PackageManager.Instance.ActiveGame.DefaultTileType &&
				   centerTile.Building == null;
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			
		}
	}

	public class TestWorkerInstance : UnitInstancePluginBase, WorldWalker.INotificationReceiver {
		public TestBuildingInstance WorkedBuilding { get; set; }

		private WorldWalker walker;
		private bool homeGoing = false;
		private bool started = false;

		public TestWorkerInstance(ILevelManager level, Unit unit) : base(level, unit) {
			walker = WorldWalker.GetInstanceFor(this, level);
			unit.Node.AddComponent(walker);

		}

		public TestWorkerInstance() {

		}

		public override void OnUpdate(float timeStep) {
			if (WorkedBuilding == null) {
				throw new InvalidOperationException("TestWorker has no building");
			}

			if (!started) {
				started = true;
				OnMovementFinished(walker);
			}
			
		}

		public override void SaveState(PluginDataWrapper pluginData) {
			var indexedData = pluginData.GetWriterForWrappedIndexedData();
			indexedData.Store(1, WorkedBuilding.Building.ID);
			indexedData.Store(2, homeGoing);
		}

		public override void LoadState(ILevelManager level, Unit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			this.Unit = unit;
			this.started = true;

			var indexedData = pluginData.GetReaderForWrappedIndexedData();
			WorkedBuilding = (TestBuildingInstance)level.GetBuilding(indexedData.Get<int>(1)).Plugin;
			homeGoing = indexedData.Get<bool>(2);
			walker = unit.GetComponent<WorldWalker>();
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

		public float GetMovementSpeed(ITile tile) {
			return 1;
		}

		public void OnMovementStarted(WorldWalker walker) {

		}

		public void OnMovementFinished(WorldWalker walker) {
			if (homeGoing) {
				homeGoing = !homeGoing;
				walker.GoTo(new IntVector2(20, 20));
				
			}
			else if (!homeGoing) {
				homeGoing = !homeGoing;
				walker.GoTo(WorkedBuilding.GetInterfaceTile(this));
				
			}
		}

		public void OnMovementFailed(WorldWalker walker) {

		}
	}
}
