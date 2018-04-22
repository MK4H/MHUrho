using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.UnitComponents {
	public interface IRangeTarget {
		int InstanceID { get; set; }

		bool Moving { get; }

		Vector3 GetPositionAfter(float time);

		void AddShooter(RangeTargetComponent.IShooter shooter);

		void RemoveShooter(RangeTargetComponent.IShooter shooter);
	}

	public abstract class RangeTargetComponent : DefaultComponent, IRangeTarget {
		public interface IShooter {
			void OnTargetDestroy(RangeTargetComponent target);
		}

		public int InstanceID { get; set; }

		public abstract bool Moving { get; }

		protected List<IShooter> shooters;

		public abstract Vector3 GetPositionAfter(float time);

		/// <summary>
		/// Adds a shooter to be notified when this target dies
		/// 
		/// IT IS RESET WITH LOAD, you need to add again when loading
		/// you can get this target by its <see cref="InstanceID"/> from <see cref="ILevelManager.GetTarget(int targetID)"/>
		/// </summary>
		/// <param name="shooter">the shooter to notify</param>
		public void AddShooter(IShooter shooter) {
			shooters.Add(shooter);
		}

		public void RemoveShooter(IShooter shooter) {
			shooters.Remove(shooter);
		}

		protected RangeTargetComponent(int instanceID) {
			this.InstanceID = instanceID;
		}

		protected override void OnDeleted() {
			base.OnDeleted();

			foreach (var shooter in shooters) {
				shooter.OnTargetDestroy(this);
			}
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			AddedToEntity(typeof(RangeTargetComponent), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			return RemovedFromEntity(typeof(RangeTargetComponent), entityDefaultComponents);
		}

	}

	public class StaticRangeTarget : RangeTargetComponent {
		public interface INotificationReceiver {

		}


		public static string ComponentName = nameof(StaticRangeTarget);
		public static DefaultComponents ComponentID = DefaultComponents.StaticRangeTarget;
		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		public override bool Moving => false;

		public Vector3 Position { get; private set; }

		protected StaticRangeTarget(int instanceID, ILevelManager level, Vector3 position)
			: base(instanceID) {
			this.Position = position;
		}

		public static RangeTargetComponent CreateNewStaticTarget<T>(T instancePlugin, int targetID, ILevelManager level, Vector3 position)
			where T : InstancePluginBase, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new StaticRangeTarget(targetID, level, position);
		}

		internal static RangeTargetComponent Load(ILevelManager level, InstancePluginBase plugin, PluginData data) {
			var notificationReceiver = plugin as INotificationReceiver;
			if (notificationReceiver == null) {
				throw new
					ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
			}

			var sequentialData = new SequentialPluginDataReader(data);
			sequentialData.MoveNext();
			int instanceID = sequentialData.GetCurrent<int>();
			sequentialData.MoveNext();
			Vector3 position = sequentialData.GetCurrent<Vector3>();
			sequentialData.MoveNext();

			return new StaticRangeTarget(instanceID, level, position);
		}

		internal override void ConnectReferences(ILevelManager level) {
			//NOTHING
		}

		public override Vector3 GetPositionAfter(float time) {
			return Position;
		}

		public override PluginData SaveState() {
			throw new NotImplementedException();
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(StaticRangeTarget), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.RemovedFromEntity(entityDefaultComponents);
			return RemovedFromEntity(typeof(StaticRangeTarget), entityDefaultComponents);
		}
	}



	public class MovingRangeTarget : RangeTargetComponent {
		public interface INotificationReceiver {

			Vector3 GetPositionAfter(float time);

		}

		public static string ComponentName = nameof(MovingRangeTarget);
		public static DefaultComponents ComponentID = DefaultComponents.MovingRangeTarget;
		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		public override bool Moving => true;

		private INotificationReceiver notificationReceiver;

		public MovingRangeTarget(int targetID, ILevelManager level, INotificationReceiver notificationReceiver)
			: base(targetID) {
			this.notificationReceiver = notificationReceiver;
		}

		public static RangeTargetComponent CreateNew<T>(T instancePlugin, int targetID, ILevelManager level)
			where T : InstancePluginBase, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new MovingRangeTarget(targetID, level, instancePlugin);
		}

		internal static RangeTargetComponent Load(ILevelManager level, InstancePluginBase plugin, PluginData data) {
			var notificationReceiver = plugin as INotificationReceiver;
			if (notificationReceiver == null) {
				throw new
					ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
			}

			var sequentialData = new SequentialPluginDataReader(data);
			sequentialData.MoveNext();
			int instanceID = sequentialData.GetCurrent<int>();
			sequentialData.MoveNext();

			return new MovingRangeTarget(instanceID, level, notificationReceiver);
		}

		internal override void ConnectReferences(ILevelManager level) {
			//NOTHING
		}

		public override Vector3 GetPositionAfter(float time) {
			return notificationReceiver.GetPositionAfter(time);
		}

		public override PluginData SaveState() {
			throw new NotImplementedException();
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(MovingRangeTarget), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.RemovedFromEntity(entityDefaultComponents);
			return RemovedFromEntity(typeof(MovingRangeTarget), entityDefaultComponents);
		}
	}

	internal class MapRangeTarget : IRangeTarget {
		public int InstanceID { get; set; }

		public bool Moving => false;

		public Vector3 Position { get; private set; }

		protected List<RangeTargetComponent.IShooter> shooters;

		protected MapRangeTarget(ILevelManager level, Vector3 position) {

			this.Position = position;
		}

		public static MapRangeTarget CreateNew(LevelManager level, Vector3 position) {
			
			var mapTarget = new MapRangeTarget(level, position);
			level.RegisterRangeTarget(mapTarget);
			return mapTarget;
		}

		public Vector3 GetPositionAfter(float time) {
			return Position;
		}

		public void AddShooter(RangeTargetComponent.IShooter shooter) {
			shooters.Add(shooter);
		}

		public void RemoveShooter(RangeTargetComponent.IShooter shooter) {
			shooters.Remove(shooter);
		}


	}



}
