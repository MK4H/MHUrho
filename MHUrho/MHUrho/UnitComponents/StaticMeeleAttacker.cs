using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.UnitComponents
{
    public class StaticMeeleAttacker : MeeleAttacker
    {
		internal class Loader : DefaultComponentLoader {
			public override DefaultComponent Component => StaticMeele;

			public StaticMeeleAttacker StaticMeele { get; private set; }

			int targetID;

			public Loader()
			{

			}

			public static PluginData SaveState(StaticMeeleAttacker staticMeele)
			{
				var writer = new SequentialPluginDataWriter(staticMeele.Level);
				writer.StoreNext(staticMeele.AttackIfInRange);
				writer.StoreNext(staticMeele.AttacksPerSecond);
				writer.StoreNext(staticMeele.TargetSearchRectangleSize);
				writer.StoreNext(staticMeele.Enabled);
				writer.StoreNext(staticMeele.Target.ID);
				writer.StoreNext(staticMeele.timeToNextAttack);
				return writer.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData)
			{
				var notificationReceiver = plugin as INotificationReceiver;
				if (notificationReceiver == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(MovingMeeleAttacker.INotificationReceiver)} interface", nameof(plugin));
				}

				var reader = new SequentialPluginDataReader(storedData, level);
				var attackIfInRange = reader.GetNext<bool>();
				var attacksPerSecond = reader.GetNext<float>();
				var targetSearchRectangleSize = reader.GetNext<IntVector2>();
				var enabled = reader.GetNext<bool>();
				targetID = reader.GetNext<int>();
				var timeToNextAttack = reader.GetNext<float>();


				StaticMeele = new StaticMeeleAttacker(level,
													notificationReceiver,
													attackIfInRange,
													attacksPerSecond,
													targetSearchRectangleSize,
													enabled,
													timeToNextAttack);
			}

			public override void ConnectReferences(LevelManager level)
			{
				StaticMeele.Target = level.GetEntity(targetID);
			}

			public override void FinishLoading()
			{
				
			}

			public override DefaultComponentLoader Clone()
			{
				return new Loader();
			}
		}

		public interface INotificationReceiver : IBaseNotificationReceiver {

		}

		public static string ComponentName = nameof(StaticMeeleAttacker);
		public static DefaultComponents ComponentID = DefaultComponents.StaticMeele;

		public override string ComponentTypeName => ComponentName;

		public override DefaultComponents ComponentTypeID => ComponentID;

		protected override IBaseNotificationReceiver BaseNotificationReceiver => notificationReceiver;

		INotificationReceiver notificationReceiver;

		protected StaticMeeleAttacker(ILevelManager level, INotificationReceiver notificationReceiver)
			:base(level)
		{
			this.notificationReceiver = notificationReceiver;
		}

		protected StaticMeeleAttacker(ILevelManager level,
									INotificationReceiver notificationReceiver,
									bool attackIfInRange,
									float attacksPerSecond,
									IntVector2 targetSearchRectangleSize,
									bool enabled,
									float timeToNextAttack)
			:base(level)
		{
			this.notificationReceiver = notificationReceiver;
			this.AttackIfInRange = attackIfInRange;
			this.AttacksPerSecond = attacksPerSecond;
			this.TargetSearchRectangleSize = targetSearchRectangleSize;
			this.Enabled = enabled;
			this.timeToNextAttack = timeToNextAttack;

		}

		public static StaticMeeleAttacker CreateNew<T>(T instancePlugin, ILevelManager level)
			where T : InstancePlugin, INotificationReceiver
		{
			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new StaticMeeleAttacker(level, instancePlugin);
		}

		public override PluginData SaveState()
		{
			return Loader.SaveState(this);
		}

		protected override void OnUpdateChecked(float timeStep)
		{
			if (Target == null) {
				SearchForTargetInRange();
			}

			if (Target != null) {
				TryAttack(timeStep);
			}
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			base.AddedToEntity(entityDefaultComponents);

			AddedToEntity(typeof(StaticMeeleAttacker), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(StaticMeeleAttacker), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}
	}
}
