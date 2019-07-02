using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.DefaultComponents;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UserInterface.MandK;
using MHUrho.Helpers.Extensions;
using ShowcasePackage.Misc;
using Urho;

namespace ShowcasePackage.Buildings
{
	public class TreeCutterType : BaseBuildingTypePlugin
	{
		public static string TypeName = "TreeCutter";
		public static int TypeID = 6;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public UnitType WorkerType { get; private set; }
		public ResourceType ProducedResource { get; private set; }

		const string WorkerElement = "workerType";
		const string ResourceElement = "resourceType";

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			XElement workerElement = extensionElement.Element(package.PackageManager.GetQualifiedXName(WorkerElement));
			WorkerType = package.GetUnitType(workerElement.Value);

			XElement resourceElement =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(ResourceElement));
			ProducedResource = package.GetResourceType(resourceElement.Value);
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return new TreeCutter(level, building, this);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return new TreeCutter(level, building, this);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, IPlayer owner, ILevelManager level)
		{
			return topLeftTileIndex == bottomRightTileIndex &&
					level.Map
						.GetTilesInRectangle(topLeftTileIndex, bottomRightTileIndex)
						.All((tile) => tile.Building == null && tile.Units.Count == 0);
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new LineBuilder(input, ui, camera, input.Level.Package.GetBuildingType(ID));
		}
	}

	public class TreeCutter : BuildingInstancePlugin {

		class Worker {

			public IUnit Unit { get; private set; }
			readonly Timeout timeout;
			readonly TreeCutter cutter;

			public Worker(TreeCutter cutter, IUnit unit, double respawnTime)
				:this(cutter, unit, respawnTime, respawnTime)
			{

			}

			Worker(TreeCutter cutter, IUnit unit, double respawnTime, double remainingTime)
			{
				this.Unit = unit;
				if (unit != null)
				{
					unit.OnRemoval += OnWorkerDeath;
				}

				this.cutter = cutter;

				timeout = new Timeout(respawnTime, remainingTime);
			}

			public static Worker Load(SequentialPluginDataReader reader, TreeCutter cutter)
			{
				int ID = reader.GetNext<int>();
				double duration = reader.GetNext<double>();
				double remaining = reader.GetNext<double>();

				IUnit unit = ID != 0 ? cutter.Level.GetUnit(ID) : null;


				return new Worker(cutter, unit, duration, remaining);
			}

			public void OnUpdate(float timeStep)
			{
				if (Unit != null) {
					return;
				}

				if (timeout.Update(timeStep)) {
					foreach (var tile in cutter.Building.Tiles[0].GetNeighbours()) {
						Unit = cutter.Level.SpawnUnit(cutter.type.WorkerType, tile, Quaternion.Identity, cutter.Building.Player);
						if (Unit != null) {
							Unit.OnRemoval += OnWorkerDeath;
							timeout.Reset();
							return;
						}
					}
				}
			}

			public void Store(SequentialPluginDataWriter writer)
			{
				writer.StoreNext(Unit?.ID ?? 0);
				writer.StoreNext(timeout.Duration);
				writer.StoreNext(timeout.Remaining);
			}

			void OnWorkerDeath(IEntity worker)
			{
				if (worker != Unit) {
					throw new InvalidOperationException("Event was sent to the wrong entity.");
				}

				timeout.Reset();
				Unit = null;
			}
		}

		public ResourceType ProducedResource => type.ProducedResource;

		/// <summary>
		/// Respawn time in seconds.
		/// </summary>
		const double workerRespawnTime = 10;

		readonly TreeCutterType type;

		readonly Worker[] workers;

		public TreeCutter(ILevelManager level, IBuilding building, TreeCutterType type)
			: base(level, building)
		{
			this.type = type;
			this.workers = new Worker[2];
			StaticRangeTarget.CreateNew(this, level, building.Center);

			using (var spawnPoints = building.Tiles[0].GetNeighbours().GetEnumerator()) {
				workers[0] = new Worker(this, SpawnWorkerUnit(spawnPoints), workerRespawnTime);
				workers[1] = new Worker(this, SpawnWorkerUnit(spawnPoints), workerRespawnTime);
			}

		}


		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			foreach (var worker in workers) {
				worker.Store(writer);
			}
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var reader = pluginData.GetReaderForWrappedSequentialData();
			workers[0] = Worker.Load(reader, this);
			workers[1] = Worker.Load(reader, this);
		}

		public override void Dispose()
		{

		}

		public override void OnUpdate(float timeStep)
		{
			foreach (var worker in workers) {
				worker.OnUpdate(timeStep);
			}
		}

		public override void OnHit(IEntity byEntity, object userData)
		{

		}

		IUnit SpawnWorkerUnit(IEnumerator<ITile> spawnPoints)
		{
			while (spawnPoints.MoveNext())
			{
				IUnit newWorker = Level.SpawnUnit(type.WorkerType, spawnPoints.Current, Quaternion.Identity, Building.Player);
				if (newWorker != null) {
					return newWorker;
				}
			}

			return null;
		}


	}
}
