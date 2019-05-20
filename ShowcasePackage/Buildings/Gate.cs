using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace ShowcasePackage.Buildings
{
	public class GateType : BuildingTypePlugin {
		public override string Name => "Gate";
		public override int ID => 3;

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return new GateInstance(level, building);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return new GateInstance(level, building);
		}

		public override bool CanBuildIn(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, ILevelManager level)
		{
			return level.Map
						.GetTilesInRectangle(topLeftTileIndex, bottomRightTileIndex)
						.All((tile) => tile.Building == null && tile.Units.Count == 0);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}
	}

	public class GateInstance : BuildingInstancePlugin {

		class Door {

			/// <summary>
			/// Time to open or close the doors in seconds
			/// </summary>
			public double OpeningTime { get; set; }
			public bool IsOpen { get; private set; }


			Node node;
			double openAngle;
			double closedAngle;
			double rotationChange;
			double start;
			double target;
			bool isTargetOpen;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="doorNode"></param>
			/// <param name="openAngle"></param>
			/// <param name="closedAngle"></param>
			/// <param name="openingTime">Time to open or close the doors in seconds</param>
			/// <param name="isOpen"></param>
			public Door(Node doorNode, double openAngle, double closedAngle, double openingTime, bool isOpen)
			{
				this.node = doorNode;
				this.openAngle = openAngle;
				this.closedAngle = closedAngle;
				this.OpeningTime = openingTime;
				this.IsOpen = isOpen;
				this.isTargetOpen = isOpen;
				this.rotationChange = isOpen ? openAngle - closedAngle : closedAngle - openAngle;
				this.start = isOpen ? closedAngle : openAngle;
				this.target = isOpen ? openAngle : closedAngle;

				SetAngle((float)target);
			}

			public void Open()
			{
				this.rotationChange = openAngle - closedAngle;
				this.start = closedAngle;
				this.target = openAngle;
				this.isTargetOpen = true;
			}

			public void Close()
			{
				this.rotationChange = closedAngle - openAngle;
				this.start = openAngle;
				this.target = closedAngle;
				this.isTargetOpen = false;
			}

			public void Show()
			{
				node.Enabled = true;
			}

			public void Hide()
			{
				node.Enabled = false;
			}

			public void SetOpen()
			{
				Open();
				SetAngle((float) target);
				IsOpen = true;
			}

			public void SetClosed()
			{
				Close();
				SetAngle((float)target);
				IsOpen = false;
			}

			public void OnUpdate(float timeStep)
			{
				double angle = node.Rotation.YawAngle;
				if ((rotationChange < 0 && angle > target) || 
					(rotationChange > 0 && angle < target)) {

					//Moving to target angle
					double tickRotation = (rotationChange / OpeningTime) * timeStep;
					node.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, (float)tickRotation));
				}
				else {
					//On target angle
					SetAngle((float)target);
					IsOpen = isTargetOpen;
				}
			}

			void SetAngle(float angle)
			{
				node.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, angle);
			}
		}

		public bool IsOpen => leftDoor.IsOpen && rightDoor.IsOpen;

		readonly Door leftDoor;
		readonly Door rightDoor;

		public GateInstance(ILevelManager level, IBuilding building)
			: base(level, building)
		{
			StaticRangeTarget.CreateNew(this, level, building.Center);
			this.leftDoor = new Door(building.Node.GetChild("Door_l"), 0, 90, 5, true);
			this.rightDoor = new Door(building.Node.GetChild("Door_r"), 0, -90, 5, true);
		}

		public void Open()
		{
			leftDoor.Open();
			rightDoor.Open();
		}

		public void Close()
		{
			leftDoor.Close();
			rightDoor.Close();
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			writer.StoreNext(IsOpen);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var reader = pluginData.GetReaderForWrappedSequentialData();
			bool isOpen = reader.GetNext<bool>();

			if (isOpen) {
				leftDoor.SetOpen();
				rightDoor.SetOpen();
			}
			else {
				leftDoor.SetClosed();
				rightDoor.SetClosed();
			}
		}

		public override void Dispose()
		{
			
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
			
		}

		public override float? GetHeightAt(float x, float y)
		{
			return null;
		}

		public override IFormationController GetFormationController(Vector3 centerPosition)
		{
			return null;
		}
	}
}
