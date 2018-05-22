using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.UnitComponents
{
	public class MovingRangeTarget : RangeTargetComponent {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => MovingRangeTarget;

			public MovingRangeTarget MovingRangeTarget { get; private set; }

			public Loader() {

			}

			public static PluginData SaveState(MovingRangeTarget movingRangeTarget) {
				var sequentialData = new SequentialPluginDataWriter();
				sequentialData.StoreNext(movingRangeTarget.InstanceID);
				sequentialData.StoreNext(movingRangeTarget.Enabled);
				return sequentialData.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData) {
				var notificationReceiver = plugin as INotificationReceiver;
				if (notificationReceiver == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
				}

				var sequentialData = new SequentialPluginDataReader(storedData);
				sequentialData.MoveNext();
				int instanceID = sequentialData.GetCurrent<int>();
				sequentialData.MoveNext();
				bool enabled = sequentialData.GetCurrent<bool>();
				sequentialData.MoveNext();


				MovingRangeTarget = new MovingRangeTarget(instanceID, level, notificationReceiver) {Enabled = enabled};
				level.LoadRangeTarget(MovingRangeTarget);
			}

			public override void ConnectReferences(LevelManager level) {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone()
			{
				return new Loader();
			}
		}

		public interface INotificationReceiver {

			IEnumerator<Waypoint> GetWaypoints(MovingRangeTarget target);

			Vector3 GetCurrentPosition(MovingRangeTarget target);

			void OnHit(MovingRangeTarget target, IProjectile projectile);

		}

		public static string ComponentName = nameof(MovingRangeTarget);
		public static DefaultComponents ComponentID = DefaultComponents.MovingRangeTarget;
		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		public override bool Moving => true;

		public override Vector3 CurrentPosition => notificationReceiver.GetCurrentPosition(this);

		INotificationReceiver notificationReceiver;

		protected MovingRangeTarget(ILevelManager level, INotificationReceiver notificationReceiver)
			:base(level)
		{
			this.notificationReceiver = notificationReceiver;
		}

		protected MovingRangeTarget(int ID, ILevelManager level, INotificationReceiver notificationReceiver)
			: base(ID, level)
		{
			this.notificationReceiver = notificationReceiver;
		}

		public static MovingRangeTarget CreateNew<T>(T instancePlugin, ILevelManager level)
			where T : InstancePlugin, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}
			var newTarget = new MovingRangeTarget(level, instancePlugin);
			((LevelManager)level).RegisterRangeTarget(newTarget);
			return newTarget;
		}

		public override IEnumerator<Waypoint> GetWaypoints() {
			return notificationReceiver.GetWaypoints(this);
		}

		public override PluginData SaveState() {
			return Loader.SaveState(this);
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(MovingRangeTarget), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(MovingRangeTarget), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}

	}
}
