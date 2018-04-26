using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.UnitComponents {


	public interface IRangeTarget {
		int InstanceID { get; set; }

		bool Moving { get; }

		Vector3 CurrentPosition { get; }

		IEnumerator<Waypoint> GetWaypoints();

		void AddShooter(RangeTargetComponent.IShooter shooter);

		void RemoveShooter(RangeTargetComponent.IShooter shooter);
	}

	public abstract class RangeTargetComponent : DefaultComponent, IRangeTarget {
		public interface IShooter {
			void OnTargetDestroy(IRangeTarget target);
		}

		public int InstanceID { get; set; }

		public abstract bool Moving { get; }

		public abstract Vector3 CurrentPosition { get; }

		protected List<IShooter> shooters;

		protected RangeTargetComponent() {
			shooters = new List<IShooter>();
		}

		protected RangeTargetComponent(int ID) {
			this.InstanceID = ID;
			shooters = new List<IShooter>();
		}

		public abstract IEnumerator<Waypoint> GetWaypoints();

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

		public override Vector3 CurrentPosition { get; }

		protected StaticRangeTarget(int instanceID, ILevelManager level, Vector3 position)
			: base(instanceID) {
			this.CurrentPosition = position;
		}

		protected StaticRangeTarget(ILevelManager level, Vector3 position) {
			this.CurrentPosition = position;
		}

		public static StaticRangeTarget CreateNewStaticTarget<T>(T instancePlugin, ILevelManager level, Vector3 position)
			where T : InstancePluginBase, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			var newTarget = new StaticRangeTarget(level, position);

			((LevelManager)level).RegisterRangeTarget(newTarget);

			return newTarget;
		}

		internal static StaticRangeTarget Load(ILevelManager level, InstancePluginBase plugin, PluginData data) {
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

		public override IEnumerator<Waypoint> GetWaypoints() {
			yield return new Waypoint(CurrentPosition, 0);
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

			IEnumerator<Waypoint> GetWaypoints();

			Vector3 GetCurrentPosition();

		}

		public static string ComponentName = nameof(MovingRangeTarget);
		public static DefaultComponents ComponentID = DefaultComponents.MovingRangeTarget;
		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		public override bool Moving => true;

		public override Vector3 CurrentPosition => notificationReceiver.GetCurrentPosition();

		private INotificationReceiver notificationReceiver;

		protected MovingRangeTarget(ILevelManager level, INotificationReceiver notificationReceiver) {
			this.notificationReceiver = notificationReceiver;
		}

		protected MovingRangeTarget(int ID, ILevelManager level, INotificationReceiver notificationReceiver) 
			:base(ID) 
		{
			this.notificationReceiver = notificationReceiver;
		}

		public static MovingRangeTarget CreateNew<T>(T instancePlugin, ILevelManager level)
			where T : InstancePluginBase, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}
			var newTarget = new MovingRangeTarget(level, instancePlugin);
			((LevelManager) level).RegisterRangeTarget(newTarget);
			return newTarget;
		}

		internal static RangeTargetComponent Load(LevelManager level, InstancePluginBase plugin, PluginData data) {
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



		public override IEnumerator<Waypoint> GetWaypoints() {
			return notificationReceiver.GetWaypoints();
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

		public Vector3 CurrentPosition { get; }

		protected List<RangeTargetComponent.IShooter> shooters;

		private LevelManager level;

		protected MapRangeTarget(LevelManager level, Vector3 position) {
			this.level = level;
			this.CurrentPosition = position;
			shooters = new List<RangeTargetComponent.IShooter>();
		}

		internal static MapRangeTarget CreateNew(LevelManager level, Vector3 position) {
			
			var mapTarget = new MapRangeTarget(level, position);
			level.RegisterRangeTarget(mapTarget);
			return mapTarget;
		}

		public IEnumerator<Waypoint> GetWaypoints() {
			yield return new Waypoint(CurrentPosition, 0);
		}

		public void AddShooter(RangeTargetComponent.IShooter shooter) {
			shooters.Add(shooter);
		}

		public void RemoveShooter(RangeTargetComponent.IShooter shooter) {
			shooters.Remove(shooter);

			if (shooters.Count == 0) {
				level.UnRegisterRangeTarget(InstanceID);
				level.Map.RemoveRangeTarget(this);
			}
		}


	}



}
