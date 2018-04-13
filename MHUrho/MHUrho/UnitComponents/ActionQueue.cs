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
            void InvokeTaskStarted(ActionQueue actionQueue);
            void InvokeTaskFinished(ActionQueue actionQueue);
            
        }

        public abstract class WorkTask : IWorkTask {
            public int Tag { get; private set; }

            bool IWorkTask.IsFinished() {
                return IsFinished();
            }

            void IWorkTask.OnUpdate(float timeStep) {
                throw new NotImplementedException();
            }

            void IWorkTask.InvokeTaskStarted(ActionQueue actionQueue) {
                throw new NotImplementedException();
            }

            void IWorkTask.InvokeTaskFinished(ActionQueue actionQueue) {
                throw new NotImplementedException();
            }

            protected abstract bool IsFinished();

            protected virtual void OnUpdate(float timeStep) {
                //NOTHING
            }

            protected abstract void InvokeTaskStarted(ActionQueue actionQueue);

            protected abstract void InvokeTaskFinished(ActionQueue actionQueue);
        }

        public class TimedWorkTask : WorkTask {
            
            public event Action<ActionQueue, TimedWorkTask> TaskStarted;
            public event Action<ActionQueue, TimedWorkTask> TaskFinished;

            private float duration;

            public TimedWorkTask(float duration) {
                this.duration = duration;
            }

            public TimedWorkTask OnTaskStarted(Action<ActionQueue, TimedWorkTask> handler) {
                TaskStarted += handler;
                return this;
            }

            public TimedWorkTask OnTaskFinished(Action<ActionQueue, TimedWorkTask> handler) {
                TaskFinished += handler;
                return this;
            }

            protected override bool IsFinished() {
                return duration <= 0;
            }

            protected override void OnUpdate(float timeStep) {
                duration -= timeStep;
            }

            protected override void InvokeTaskStarted(ActionQueue actionQueue) {
                TaskStarted?.Invoke(actionQueue, this);
            }

            protected override void InvokeTaskFinished(ActionQueue actionQueue) {
                TaskFinished?.Invoke(actionQueue, this);
            }
        }

        public class DelegatedWorkTask : WorkTask {
            public event Action<ActionQueue, DelegatedWorkTask> TaskStarted;
            public event Action<ActionQueue, DelegatedWorkTask> TaskFinished;

            private bool finished = false;

            public void Finish() {
                finished = true;
            }

            public DelegatedWorkTask OnTaskStarted(Action<ActionQueue, DelegatedWorkTask> handler) {
                TaskStarted += handler;
                return this;
            }

            public DelegatedWorkTask OnTaskFinished(Action<ActionQueue, DelegatedWorkTask> handler) {
                TaskFinished += handler;
                return this;
            }

            protected override bool IsFinished() {
                return finished;
            }

            protected override void InvokeTaskStarted(ActionQueue actionQueue) {
                TaskStarted?.Invoke(actionQueue, this);
            }

            protected override void InvokeTaskFinished(ActionQueue actionQueue) {
                TaskFinished?.Invoke(actionQueue, this);
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
