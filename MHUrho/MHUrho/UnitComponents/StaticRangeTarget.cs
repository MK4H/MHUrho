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
	public class StaticRangeTarget : RangeTargetComponent {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => StaticRangeTarget;

			public StaticRangeTarget StaticRangeTarget { get; private set; }

			public Loader() {

			}

			public static PluginData SaveState(StaticRangeTarget staticRangeTarget) {
				var sequentialData = new SequentialPluginDataWriter();
				sequentialData.StoreNext(staticRangeTarget.InstanceID);
				sequentialData.StoreNext(staticRangeTarget.CurrentPosition);
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
				Vector3 position = sequentialData.GetCurrent<Vector3>();
				sequentialData.MoveNext();
				bool enabled = sequentialData.GetCurrent<bool>();
				sequentialData.MoveNext();

				StaticRangeTarget = new StaticRangeTarget(instanceID, level, position, notificationReceiver)
									{
										Enabled = enabled
									};
				level.LoadRangeTarget(StaticRangeTarget);
			}

			public override void ConnectReferences(LevelManager level) {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
			}
		}

		public interface INotificationReceiver {

			void OnHit(StaticRangeTarget target, IProjectile projectile);

		}


		public static string ComponentName = nameof(StaticRangeTarget);
		public static DefaultComponents ComponentID = DefaultComponents.StaticRangeTarget;
		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		public override bool Moving => false;

		public override Vector3 CurrentPosition { get; }

		INotificationReceiver notificationReceiver;

		protected StaticRangeTarget(int instanceID, ILevelManager level, Vector3 position, INotificationReceiver notificationReceiver)
			: base(instanceID, level)
		{
			this.CurrentPosition = position;
			this.notificationReceiver = notificationReceiver;
		}

		protected StaticRangeTarget(ILevelManager level, Vector3 position, INotificationReceiver notificationReceiver) 
			:base(level)
		{
			this.CurrentPosition = position;
			this.notificationReceiver = notificationReceiver;
		}

		public static StaticRangeTarget CreateNew<T>(T instancePlugin, ILevelManager level, Vector3 position)
			where T : InstancePlugin, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			var newTarget = new StaticRangeTarget(level, position, instancePlugin);

			((LevelManager)level).RegisterRangeTarget(newTarget);

			return newTarget;
		}

		public override PluginData SaveState() {
			return Loader.SaveState(this);
		}

		public override IEnumerator<Waypoint> GetWaypoints() {
			yield return new Waypoint(CurrentPosition, 0, MovementType.Linear);
		}


		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(StaticRangeTarget), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(StaticRangeTarget), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}
	}
}
