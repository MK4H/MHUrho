﻿using System;
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

		class PathVisitor : NodeVisitor {
			TestWorkerInstance worker;

			public PathVisitor(TestWorkerInstance worker)
			{
				this.worker = worker;
			}

			public override bool Visit(ITempNode source, ITileEdgeNode target, out float time)
			{
				time = (source.Position - target.Position).Length;
				return true;
			}

			public override bool Visit(ITempNode source, ITileNode target, out float time)
			{
				time = (source.Position - target.Position).Length;
				return true;
			}

			public override bool Visit(ITileEdgeNode source, ITileNode target, out float time)
			{
				if (target.Tile.Building == null) {
					time = (source.Position - target.Position).Length;
					return true;
				}

				time = -1;
				return false;
			}

			public override bool Visit(ITileNode source, ITileEdgeNode target, out float time)
			{
				time = (source.Position - target.Position).Length;
				ITileNode targetTile = target.GetOtherSide(source);

				//If the edge is diagonal and there are buildings on both sides of the edge, dont go there
				if (source.Tile.MapLocation.X != targetTile.Tile.MapLocation.X &&
					source.Tile.MapLocation.Y != targetTile.Tile.MapLocation.Y &&
					worker.Map
						.GetTileByMapLocation(new IntVector2(source.Tile.MapLocation.X,
															targetTile.Tile.MapLocation.Y))
						.Building != null &&
					worker.Map
						.GetTileByMapLocation(new IntVector2(targetTile.Tile.MapLocation.X,
															source.Tile.MapLocation.Y)) != null
				) {
					time = -1;
					return false;
				}

				return true;
			}

		}

		public TestBuildingInstance WorkedBuilding { get; set; }


		WorldWalker walker;
		bool homeGoing = false;
		bool started = false;

		readonly PathVisitor pathVisitor;

		public TestWorkerInstance(ILevelManager level, IUnit unit) : base(level, unit) {
			walker = WorldWalker.GetInstanceFor(this, level);
			unit.AddComponent(walker);

			this.pathVisitor = new PathVisitor(this);
			
		}

		public TestWorkerInstance() {
			this.pathVisitor = new PathVisitor(this);
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

		public override void OnProjectileHit(IProjectile projectile)
		{
			throw new NotImplementedException();
		}

		public override void OnMeeleHit(IEntity byEntity)
		{
			throw new NotImplementedException();
		}

		public bool GetTime(INode from, INode to, out float time)
		{
			return from.Accept(pathVisitor, to, out time);
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
				walker.GoTo(Map.PathFinding.GetTileNode(Map.GetTileByMapLocation(new IntVector2(20, 20))));
				
			}
			else if (!homeGoing) {
				homeGoing = !homeGoing;
				walker.GoTo(Map.PathFinding.GetTileNode(WorkedBuilding.GetInterfaceTile(this)));
				
			}
		}

		public void OnMovementFailed(WorldWalker walker) {

		}
	}
}
