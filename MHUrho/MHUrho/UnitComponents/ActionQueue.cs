using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.UnitComponents
{
    internal delegate void TaskStartedDelegate(ActionQueue queue, ActionQueue.WorkTask workTask);
    internal delegate void TaskFinishedDelegate(ActionQueue queue, ActionQueue.WorkTask workTask);

    public class ActionQueue : DefaultComponent {

        public interface INotificationReciver {
            void OnWorkTaskStarted(ActionQueue queue, WorkTask workTask);

            void OnWorkTaskFinished(ActionQueue queue, WorkTask workTask);
        }

        private interface IWorkTask {
            bool IsFinished();
            void OnUpdate(float timeStep);            
        }

        public abstract class WorkTask : IWorkTask {
            public int Tag { get; private set; }

            protected WorkTask(int tag) {
                this.Tag = tag;
            }

            bool IWorkTask.IsFinished() {
                return IsFinished();
            }

            void IWorkTask.OnUpdate(float timeStep) {
                OnUpdate(timeStep);
            }

            protected abstract bool IsFinished();

            protected virtual void OnUpdate(float timeStep) {
                //NOTHING
            }

        }

        public class TimedWorkTask : WorkTask {
            
            private float duration;

            public TimedWorkTask(float duration, int tag) : base(tag) {
                this.duration = duration;
            }

            protected override bool IsFinished() {
                return duration <= 0;
            }

            protected override void OnUpdate(float timeStep) {
                duration -= timeStep;
            }
        }

        public class DelegatedWorkTask : WorkTask {

            private bool finished = false;

            public DelegatedWorkTask(int tag) : base(tag) {

            }

            public void Finish() {
                finished = true;
            }

            protected override bool IsFinished() {
                return finished;
            }
        }

        public static string ComponentName = nameof(ActionQueue);
        public static DefaultComponents ComponentID = DefaultComponents.WorkQueue;

        public override string ComponentTypeName => ComponentName;
        public override DefaultComponents ComponentTypeID => ComponentID;

        internal event TaskStartedDelegate OnTaskStarted;
        internal event TaskFinishedDelegate OnTaskFinished;

        private readonly Queue<IWorkTask> workQueue;

        private INotificationReciver notificationReciver;

        public ActionQueue CreateNew<T>(T instancePlugin)
            where T : InstancePluginBase, INotificationReciver {

            if (instancePlugin == null) {
                throw new ArgumentNullException(nameof(instancePlugin));
            }

            return new ActionQueue(instancePlugin);
        }

        protected ActionQueue(INotificationReciver notificationReciver) {
            this.notificationReciver = notificationReciver;
            workQueue = new Queue<IWorkTask>();
        }

        internal static ActionQueue Load(ILevelManager level, InstancePluginBase plugin, PluginData pluginData) {
            throw new NotImplementedException();
        }

        internal override void ConnectReferences(ILevelManager level) {
            //NOTHING
        }

        public override PluginData SaveState() {
            throw new NotImplementedException();
        }

        public void EnqueueTask(WorkTask task) {
            workQueue.Enqueue(task);
        }

        public override void OnAttachedToNode(Node node) {
            base.OnAttachedToNode(node);

            OnTaskStarted += notificationReciver.OnWorkTaskStarted;
            OnTaskFinished += notificationReciver.OnWorkTaskFinished;
        }

        protected override void OnUpdate(float timeStep) {
            base.OnUpdate(timeStep);

            if (!EnabledEffective) return;

            if (workQueue.Count == 0) {
                return;
            }

            var currentTask = workQueue.Peek();
            currentTask.OnUpdate(timeStep);

            if (currentTask.IsFinished()) {
                OnTaskFinished?.Invoke(this, (WorkTask)currentTask);
            }

            workQueue.Dequeue();

            if (workQueue.Count == 0) return;

            OnTaskStarted?.Invoke(this, (WorkTask) currentTask);
        }

        
    }
}
