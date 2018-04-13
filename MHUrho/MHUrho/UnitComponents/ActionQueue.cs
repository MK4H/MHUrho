using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.UnitComponents
{
    

    public class ActionQueue : DefaultComponent {

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
                throw new NotImplementedException();
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

        public override string Name => ComponentName;
        public override DefaultComponents ID => ComponentID;

        private readonly Queue<IWorkTask> workQueue;

        public ActionQueue() {
            workQueue = new Queue<IWorkTask>();
        }

        public static ActionQueue Load(ILevelManager level, PluginData pluginData) {
            throw new NotImplementedException();
        }

        public override PluginData SaveState() {
            throw new NotImplementedException();
        }

        public void EnqueueTask(WorkTask task) {
            workQueue.Enqueue(task);
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
                currentTask.InvokeTaskFinished(this);
            }

            workQueue.Dequeue();

            if (workQueue.Count == 0) return;

            workQueue.Peek().InvokeTaskStarted(this);
        }

        
    }
}
