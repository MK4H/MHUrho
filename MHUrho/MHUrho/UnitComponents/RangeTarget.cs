using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    public abstract class RangeTarget : DefaultComponent
    {
        public abstract bool Moving { get; }


        public abstract Vector3 GetPositionAfter(float time);


    }

    public class StaticRangeTarget : RangeTarget {
        public interface INotificationReceiver {

        }


        public static string ComponentName = nameof(StaticRangeTarget);
        public static DefaultComponents ComponentID = DefaultComponents.StaticRangeTarget;
        public override string Name => ComponentName;
        public override DefaultComponents ID => ComponentID;

        public override bool Moving => false;

        public Vector3 Position { get; private set; }

        protected StaticRangeTarget(ILevelManager level, Vector3 position) {
            this.Position = position;
        }

        public static RangeTarget CreateNewStaticTarget<T>(T instancePlugin, ILevelManager level, Vector3 position)
            where T : InstancePluginBase, INotificationReceiver {

            if (instancePlugin == null) {
                throw new ArgumentNullException(nameof(instancePlugin));
            }

            return new StaticRangeTarget(level, position);
        }

        //For map targets
        internal static StaticRangeTarget CreateNew(ILevelManager level,Vector3 position) {
            return new StaticRangeTarget(level, position);
        }

        public override Vector3 GetPositionAfter(float time) {
            return Position;
        }

        public override PluginData SaveState() {
            throw new NotImplementedException();
        }
    }

    public class MovingRangeTarget : RangeTarget {
        public interface INotificationReceiver {

            Vector3 GetPositionAfter(float time);

        }

        public static string ComponentName = nameof(MovingRangeTarget);
        public static DefaultComponents ComponentID = DefaultComponents.MovingRangeTarget;
        public override string Name => ComponentName;
        public override DefaultComponents ID => ComponentID;

        public override bool Moving => true;

        private INotificationReceiver notificationReceiver;

        public static RangeTarget CreateNew<T>(T instancePlugin, ILevelManager level)
            where T : InstancePluginBase, INotificationReceiver {

            if (instancePlugin == null) {
                throw new ArgumentNullException(nameof(instancePlugin));
            }

            return new MovingRangeTarget();
        }

        public override Vector3 GetPositionAfter(float time) {
            return notificationReceiver.GetPositionAfter(time);
        }

        public override PluginData SaveState() {
            throw new NotImplementedException();
        }
    }
}
