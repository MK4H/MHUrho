using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.UnitComponents
{
    public class MovingMeeleAttacker : MeeleAttacker
    {
		internal class Loader : DefaultComponentLoader{
			public override DefaultComponent Component => MovingMeele;

			public MovingMeeleAttacker MovingMeele { get; private set; }

			int targetID;

			public Loader()
			{

			}

			public static PluginData SaveState(MovingMeeleAttacker movingMeele)
			{
				var writer = new SequentialPluginDataWriter(movingMeele.Level);
				writer.StoreNext(movingMeele.AttackIfInRange);
				writer.StoreNext(movingMeele.AttacksPerSecond);
				writer.StoreNext(movingMeele.TargetSearchRectangleSize);
				writer.StoreNext(movingMeele.Enabled);
				writer.StoreNext(movingMeele.Target.ID);
				writer.StoreNext(movingMeele.timeBetweenPositionChecks);
				writer.StoreNext(movingMeele.timeToNextAttack);
				writer.StoreNext(movingMeele.timeToNextPositionCheck);
				return writer.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData)
			{
				var notificationReceiver = plugin as INotificationReceiver;
				if (notificationReceiver == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
				}

				var reader = new SequentialPluginDataReader(storedData, level);
				var attackIfInRange = reader.GetNext<bool>();
				var attacksPerSecond = reader.GetNext<float>();
				var targetSearchRectangleSize = reader.GetNext<IntVector2>();
				var enabled = reader.GetNext<bool>();
				targetID = reader.GetNext<int>();
				var timeBetweenPositionChecks = reader.GetNext<float>();
				var timeToNextAttack = reader.GetNext<float>();
				var timeToNextPositionCheck = reader.GetNext<float>();

				MovingMeele = new MovingMeeleAttacker(level,
													 notificationReceiver,
													 attackIfInRange,
													 attacksPerSecond,
													 targetSearchRectangleSize,
													 enabled,
													 timeBetweenPositionChecks,
													 timeToNextAttack,
													 timeToNextPositionCheck);

			}

			public override void ConnectReferences(LevelManager level)
			{
				MovingMeele.Target = level.GetEntity(targetID);
				//TODO: Runtime check in release to find corrupted save files
				Debug.Assert(MovingMeele.Target != null, "Saved entity ID was not valid");
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

			void MoveTo(Vector3 position);
		}

		public static string ComponentName = nameof(MovingMeeleAttacker);
		public static DefaultComponents ComponentID = DefaultComponents.MovingMeele;

		public override string ComponentTypeName => ComponentName;

		public override DefaultComponents ComponentTypeID => ComponentID;


		protected override IBaseNotificationReceiver BaseNotificationReceiver => notificationReceiver;


		Vector3 previousTargetPosition;


		float timeBetweenPositionChecks;
		float timeToNextPositionCheck;

		INotificationReceiver notificationReceiver;

		protected MovingMeeleAttacker(ILevelManager level, INotificationReceiver notificationReceiver)
			: base(level)
		{
			this.notificationReceiver = notificationReceiver;
		}

		protected MovingMeeleAttacker(ILevelManager level,
									INotificationReceiver notificationReceiver,
									bool attackIfInRange,
									float attacksPerSecond,
									IntVector2 targetSearchRectangleSize,
									bool enabled,
									float timeBetweenPositionChecks,
									float timeToNextAttack,
									float timeToNextPositionCheck)
			:base(level)
		{
			this.notificationReceiver = notificationReceiver;
			this.AttackIfInRange = attackIfInRange;
			this.AttacksPerSecond = attacksPerSecond;
			this.TargetSearchRectangleSize = targetSearchRectangleSize;
			this.Enabled = enabled;
			this.timeBetweenPositionChecks = timeBetweenPositionChecks;
			this.timeToNextAttack = timeToNextAttack;
			this.timeToNextPositionCheck = timeToNextPositionCheck;
		}

		public static MovingMeeleAttacker CreateNew<T>(T instancePlugin, ILevelManager level)
			where T : InstancePlugin, INotificationReceiver
		{
			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new MovingMeeleAttacker(level, instancePlugin);
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

			//If attacker has a target, and the target is out of range
			if (Target != null && !TryAttack(timeStep)) {

				timeToNextPositionCheck -= timeStep;
				if (timeToNextPositionCheck > 0) return;

				timeToNextPositionCheck = timeBetweenPositionChecks;
				CheckTargetPosition();
				
			}

			
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			base.AddedToEntity(entityDefaultComponents);

			AddedToEntity(typeof(MovingMeeleAttacker), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(MovingMeeleAttacker), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}

		void CheckTargetPosition()
		{
			//TODO: Intersect
			if (Target.Position != previousTargetPosition) {
				notificationReceiver.MoveTo(Target.Position);
				previousTargetPosition = Target.Position;
			}
		}
	}
}
