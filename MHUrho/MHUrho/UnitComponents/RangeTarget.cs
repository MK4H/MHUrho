using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    public abstract class RangeTarget : DefaultComponent {
        public interface IShooter {
            void OnTargetDestroy(RangeTarget target);
        }

        public int InstanceID;

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

        protected RangeTarget(int instanceID) {
            this.InstanceID = instanceID;
        }

        protected override void OnDeleted() {
            base.OnDeleted();

            foreach (var shooter in shooters) {
                shooter.OnTargetDestroy(this);
            }
        }

    }

    public class StaticRangeTarget : RangeTarget {
        public interface INotificationReceiver {

        }


        public static string ComponentName = nameof(StaticRangeTarget);
        public static DefaultComponents ComponentID = DefaultComponents.StaticRangeTarget;
        public override string ComponentTypeName => ComponentName;
        public override DefaultComponents ComponentTypeID => ComponentID;

        public override bool Moving => false;

        public Vector3 Position { get; private set; }

        protected StaticRangeTarget(int instanceID, ILevelManager level, Vector3 position)
            : base(instanceID)
        {
            this.Position = position;
        }

        public static RangeTarget CreateNewStaticTarget<T>(T instancePlugin, int targetID, ILevelManager level, Vector3 position)
            where T : InstancePluginBase, INotificationReceiver {

            if (instancePlugin == null) {
                throw new ArgumentNullException(nameof(instancePlugin));
            }

            return new StaticRangeTarget(targetID, level, position);
        }


        public override Vector3 GetPositionAfter(float time) {
            return Position;
        }

        public override PluginData SaveState() {
            throw new NotImplementedException();
        }
    }

    internal class MapRangeTarget : RangeTarget {
        public static string ComponentName = nameof(StaticRangeTarget);
        public static DefaultComponents ComponentID = DefaultComponents.StaticRangeTarget;

        public override string ComponentTypeName => ComponentName;
        public override DefaultComponents ComponentTypeID => ComponentID;

        public override bool Moving => false;

        protected MapRangeTarget(int instanceID, ILevelManager level, Vector3 position)
            : base(instanceID) {
            this.Position = position;
        }
    }

    public class MovingRangeTarget : RangeTarget {
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
            : base(targetID) 
        {
            this.notificationReceiver = notificationReceiver;
        }

        public static RangeTarget CreateNew<T>(T instancePlugin, int targetID, ILevelManager level)
            where T : InstancePluginBase, INotificationReceiver {

            if (instancePlugin == null) {
                throw new ArgumentNullException(nameof(instancePlugin));
            }

            return new MovingRangeTarget(targetID, level, instancePlugin);
        }

        public override Vector3 GetPositionAfter(float time) {
            return notificationReceiver.GetPositionAfter(time);
        }

        public override PluginData SaveState() {
            throw new NotImplementedException();
        }

 
    }

}
