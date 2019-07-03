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
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	public class TreeCutterType : BaseBuildingTypePlugin
	{
		public static string TypeName = "TreeCutter";
		public static int TypeID = 6;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public Cost Cost { get; private set; }
		public ViableTileTypes ViableTileTypes { get; private set; }

		public UnitType WorkerType { get; private set; }
		public ResourceType ProducedResource { get; private set; }

		const string WorkerElement = "workerType";
		const string ResourceElement = "resourceType";
		const string CostElement = "cost";
		const string CanBuildOnElement = "canBuildOn";

		BuildingType myType;

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			XElement workerElement = extensionElement.Element(package.PackageManager.GetQualifiedXName(WorkerElement));
			WorkerType = package.GetUnitType(workerElement.Value);

			XElement resourceElement =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(ResourceElement));
			ProducedResource = package.GetResourceType(resourceElement.Value);

			XElement costElem = extensionElement.Element(package.PackageManager.GetQualifiedXName(CostElement));
			Cost = Cost.FromXml(costElem, package);

			XElement canBuildOnElem =
				extensionElement.Element(package.PackageManager.GetQualifiedXName(CanBuildOnElement));
			ViableTileTypes = ViableTileTypes.FromXml(canBuildOnElem, package);

			myType = package.GetBuildingType(TypeID);
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return TreeCutter.CreateNew(level, building, this);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return TreeCutter.CreateForLoading(level, building, this);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, IPlayer owner, ILevelManager level)
		{
			return topLeftTileIndex == bottomRightTileIndex &&
					level.Map
						.GetTilesInRectangle(topLeftTileIndex, bottomRightTileIndex)
						.All((tile) => tile.Building == null && tile.Units.Count == 0 && ViableTileTypes.CanBuildOn(tile));
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new TreeCutterBuilder(input, ui, camera, myType, this);
		}
	}

	public class TreeCutter : BuildingInstancePlugin {

		class Worker {

			public IUnit Unit { get; private set; }

			public bool DoRespawn { get; set; }

			readonly Timeout timeout;
			readonly TreeCutter cutter;

			public Worker(TreeCutter cutter, IUnit unit, double respawnTime)
				:this(cutter, unit, respawnTime, respawnTime)
			{
				
			}

			Worker(TreeCutter cutter, IUnit unit, double respawnTime, double remainingTime)
			{
				this.DoRespawn = true;
				this.Unit = unit;
				if (unit != null)
				{
					unit.OnRemoval += OnWorkerDeath;
				}

				this.cutter = cutter;

				timeout = new Timeout(respawnTime, remainingTime);
			}

			public static Worker Load(SequentialPluginDataReader reader, TreeCutter cutter, bool doRespawn)
			{
				reader.GetNext(out int ID);
				reader.GetNext(out double duration);
				reader.GetNext(out double remaining);

				IUnit unit = ID != 0 ? cutter.Level.GetUnit(ID) : null;


				return new Worker(cutter, unit, duration, remaining)
						{
							DoRespawn = doRespawn
						};
			}

			public void OnUpdate(float timeStep)
			{
				if (Unit != null || !DoRespawn) {
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

			public void Despawn()
			{
				Unit?.RemoveFromLevel();
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

		const int numberOfWorkers = 2;

		/// <summary>
		/// Respawn time in seconds.
		/// </summary>
		const double workerRespawnTime = 10;

		readonly TreeCutterType type;

		readonly Worker[] workers;

		TreeCutter(ILevelManager level, IBuilding building, TreeCutterType type)
			: base(level, building)
		{
			this.type = type;
			this.workers = new Worker[numberOfWorkers];
		}


		public static TreeCutter CreateNew(ILevelManager level, IBuilding building, TreeCutterType type)
		{
			TreeCutter newCutter = new TreeCutter(level, building, type);
			StaticRangeTarget.CreateNew(newCutter, level, building.Center);
			using (var spawnPoints = building.Tiles[0].GetNeighbours().GetEnumerator())
			{
				for (int i = 0; i < numberOfWorkers; i++) {
					IUnit workerUnit1 = level.EditorMode ? null : newCutter.SpawnWorkerUnit(spawnPoints);
					newCutter.workers[i] = new Worker(newCutter, workerUnit1, workerRespawnTime)
											{
												DoRespawn = !level.EditorMode
											};
				}
			}

			return newCutter;
		}

		public static TreeCutter CreateForLoading(ILevelManager level, IBuilding building, TreeCutterType type)
		{
			return new TreeCutter(level, building, type);
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
			for (int i = 0; i < numberOfWorkers; i++) {
				workers[i] = Worker.Load(reader, this, Level.EditorMode);
			}
		}

		public override void Dispose()
		{
			foreach (var worker in workers) {
				worker.Despawn();
				worker.DoRespawn = false;
			}		
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

	class TreeCutterBuilder : DirectionlessBuilder {

		readonly BaseCustomWindowUI cwUI;

		public TreeCutterBuilder(GameController input,
								GameUI ui,
								CameraMover camera,
								BuildingType type,
								TreeCutterType myType)
			: base(input, ui, camera, type)
		{
			cwUI = new BaseCustomWindowUI(ui, myType.Name, $"Cost: {myType.Cost}");
		}

		public override void Enable()
		{
			base.Enable();

			cwUI.Show();
		}

		public override void Disable()
		{
			cwUI.Hide();

			base.Disable();
		}

		public override void Dispose()
		{
			cwUI.Dispose();

			base.Dispose();
		}
	}
}
