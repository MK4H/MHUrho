using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.DefaultComponents;
using MHUrho.EntityInfo;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UserInterface.MandK;
using MHUrho.Helpers.Extensions;
using ShowcasePackage.Misc;
using ShowcasePackage.Units;
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
		public BuildingType MyTypeInstance { get; private set; }


		const string WorkerElement = "workerType";
		const string ResourceElement = "resourceType";
		const string CostElement = "cost";
		const string CanBuildOnElement = "canBuildOn";


		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			MyTypeInstance = package.GetBuildingType(TypeID);

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
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return TreeCutter.CreateNew(level, building, this);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return TreeCutter.CreateForLoading(level, building, this);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IPlayer owner, ILevelManager level)
		{
			return level.Map
						.GetTilesInRectangle(MyTypeInstance.GetBuildingTilesRectangle(topLeftTileIndex))
						.All((tile) => tile.Building == null && tile.Units.Count == 0 && ViableTileTypes.IsViable(tile));
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new TreeCutterBuilder(input, ui, camera, this);
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
				if (unit != null) {
					((Dog) unit.UnitPlugin).Cutter = cutter;
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
							((Dog)Unit.UnitPlugin).Cutter = cutter;
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

		HealthBarControl healthBar;

		TreeCutter(ILevelManager level, IBuilding building, TreeCutterType type)
			: base(level, building)
		{
			this.type = type;
			this.workers = new Worker[numberOfWorkers];
		}


		public static TreeCutter CreateNew(ILevelManager level, IBuilding building, TreeCutterType type)
		{
			TreeCutter newCutter = null;
			try {
				newCutter = new TreeCutter(level, building, type);
				newCutter.healthBar =
					new HealthBarControl(level, building, 100, new Vector3(0, 3, 0), new Vector2(0.5f, 0.1f), false);
				StaticRangeTarget.CreateNew(newCutter, level, building.Center);

				using (var spawnPoints = building.Tiles[0].GetNeighbours().GetEnumerator()) {
					for (int i = 0; i < numberOfWorkers; i++) {
						//TODO: Testing
						IUnit workerUnit1 = !level.EditorMode ? null : newCutter.SpawnWorkerUnit(spawnPoints);
						newCutter.workers[i] = new Worker(newCutter, workerUnit1, workerRespawnTime)
												{
													//TODO: Testing
													DoRespawn = level.EditorMode
												};
					}
				}

				return newCutter;
			}
			catch (Exception e) {
				newCutter?.Dispose();
				throw;
			}
		}

		public static TreeCutter CreateForLoading(ILevelManager level, IBuilding building, TreeCutterType type)
		{
			return new TreeCutter(level, building, type);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			healthBar.Save(writer);
			foreach (var worker in workers) {
				worker.Store(writer);
			}
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var reader = pluginData.GetReaderForWrappedSequentialData();
			healthBar = HealthBarControl.Load(Level, Building, reader);

			for (int i = 0; i < numberOfWorkers; i++) {
				workers[i] = Worker.Load(reader, this, !Level.EditorMode);
			}
		}

		public override void Dispose()
		{
			foreach (var worker in workers) {
				worker.Despawn();
				worker.DoRespawn = false;
			}
			healthBar?.Dispose();
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
			if (Building.Player.IsFriend(byEntity.Player))
			{
				return;
			}

			int damage = (int)userData;

			if (!healthBar.ChangeHitPoints(-damage))
			{
				Building.RemoveFromLevel();
			}
		}

		public override void OnUpdate(float timeStep)
		{
			foreach (var worker in workers) {
				worker.OnUpdate(timeStep);
			}
		}

		public override bool CanChangeTileHeight(int x, int y)
		{
			return false;
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
								TreeCutterType type)
			: base(input, ui, camera, type.MyTypeInstance)
		{
			cwUI = new BaseCustomWindowUI(ui, type.Name, $"Cost: {type.Cost}");
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
