using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace DefaultPackage
{
	public class TestWorkerType : UnitTypePlugin {

		public override bool IsMyType(string typeName) {
			return typeName == "TestWorker";
		}

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit) {
			return new TestWorkerInstance(level, unit);
		}

		public override UnitInstancePlugin GetInstanceForLoading() {
			return new TestWorkerInstance();
		}


		public override bool CanSpawnAt(ITile centerTile) {
			return centerTile.Type != PackageManager.Instance.ActiveGame.DefaultTileType &&
				   centerTile.Building == null;
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			
		}
	}

	public class TestWorkerInstance : UnitInstancePlugin, WorldWalker.INotificationReceiver {
		public TestBuildingInstance WorkedBuilding { get; set; }

		public float MaxMovementSpeed => 100;

		WorldWalker walker;
		bool homeGoing = false;
		bool started = false;

		public TestWorkerInstance(ILevelManager level, IUnit unit) : base(level, unit) {
			walker = WorldWalker.GetInstanceFor(this, level);
			unit.AddComponent(walker);

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

		public override void LoadState(ILevelManager level, IUnit unit, PluginDataWrapper pluginData) {
			this.Level = level;
			this.Unit = unit;
			this.started = true;

			var indexedData = pluginData.GetReaderForWrappedIndexedData();
			WorkedBuilding = (TestBuildingInstance)level.GetBuilding(indexedData.Get<int>(1)).BuildingPlugin;
			homeGoing = indexedData.Get<bool>(2);
			walker = unit.GetDefaultComponent<WorldWalker>();
		}

		

		public override bool CanGoFromTo(Vector3 from, Vector3 to)
		{
			ITile fromTile = Map.GetContainingTile(from);
			ITile toTile = Map.GetContainingTile(to);

			var diff = toTile.MapLocation - fromTile.MapLocation;
			bool targetEmpty = toTile.Building == null;

			if (diff.X == 0 || diff.Y == 0) {
				return targetEmpty;
			}
			else {
				//Diagonal
				var tile1 = fromTile.Map.GetTileByMapLocation(fromTile.MapLocation + new IntVector2(diff.X, 0));
				var tile2 = fromTile.Map.GetTileByMapLocation(fromTile.MapLocation + new IntVector2(0, diff.Y));

				return targetEmpty && (tile1.Building == null || tile2.Building == null);
			}
		}

		public override void OnProjectileHit(IProjectile projectile)
		{
			throw new NotImplementedException();
		}

		public override void OnMeeleHit(IEntity byEntity)
		{
			throw new NotImplementedException();
		}

		public bool GetTime(AStarNode from, AStarNode to, out float time)
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
