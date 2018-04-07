using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.UnitComponents
{
    

    public class WorkQueue : DefaultComponent {

        private interface IWorkTask {
            bool IsFinished();
            void OnUpdate(float timeStep);
            void InvokeTaskStarted(WorkQueue workQueue);
            void InvokeTaskFinished(WorkQueue workQueue);
            
        }

        public abstract class WorkTask : IWorkTask {
            bool IWorkTask.IsFinished() {
                return IsFinished();
            }

            void IWorkTask.OnUpdate(float timeStep) {
                throw new NotImplementedException();
            }

            void IWorkTask.InvokeTaskStarted(WorkQueue workQueue) {
                throw new NotImplementedException();
            }

            void IWorkTask.InvokeTaskFinished(WorkQueue workQueue) {
                throw new NotImplementedException();
            }

            protected abstract bool IsFinished();

            protected virtual void OnUpdate(float timeStep) {
                //NOTHING
            }

            protected abstract void InvokeTaskStarted(WorkQueue workQueue);

            protected abstract void InvokeTaskFinished(WorkQueue workQueue);
        }

        public class TimedWorkTask : WorkTask {
            
            public event Action<WorkQueue, TimedWorkTask> TaskStarted;
            public event Action<WorkQueue, TimedWorkTask> TaskFinished;

            private float duration;

            public TimedWorkTask(float duration) {
                this.duration = duration;
            }

            public TimedWorkTask OnTaskStarted(Action<WorkQueue, TimedWorkTask> handler) {
                TaskStarted += handler;
                return this;
            }

            public TimedWorkTask OnTaskFinished(Action<WorkQueue, TimedWorkTask> handler) {
                TaskFinished += handler;
                return this;
            }

            protected override bool IsFinished() {
                return duration <= 0;
            }

            protected override void OnUpdate(float timeStep) {
                duration -= timeStep;
            }

            protected override void InvokeTaskStarted(WorkQueue workQueue) {
                TaskStarted?.Invoke(workQueue, this);
            }

            protected override void InvokeTaskFinished(WorkQueue workQueue) {
                TaskFinished?.Invoke(workQueue, this);
            }
        }

        public class DelegatedWorkTask : WorkTask {
            public event Action<WorkQueue, DelegatedWorkTask> TaskStarted;
            public event Action<WorkQueue, DelegatedWorkTask> TaskFinished;

            private bool finished = false;

            public void Finish() {
                finished = true;
            }

            public DelegatedWorkTask OnTaskStarted(Action<WorkQueue, DelegatedWorkTask> handler) {
                TaskStarted += handler;
                return this;
            }

            public DelegatedWorkTask OnTaskFinished(Action<WorkQueue, DelegatedWorkTask> handler) {
                TaskFinished += handler;
                return this;
            }

            protected override bool IsFinished() {
                return finished;
            }

            protected override void InvokeTaskStarted(WorkQueue workQueue) {
                TaskStarted?.Invoke(workQueue, this);
            }

            protected override void InvokeTaskFinished(WorkQueue workQueue) {
                TaskFinished?.Invoke(workQueue, this);
            }
        }

        public static string ComponentName = nameof(WorkQueue);
        public static DefaultComponents ComponentID = DefaultComponents.WorkQueue;

        public override string Name => ComponentName;
        public override DefaultComponents ID => ComponentID;

        private readonly Queue<IWorkTask> workQueue;

        public WorkQueue(Building building) {
            workQueue = new Queue<IWorkTask>();
        }

        public static WorkQueue Load(LevelManager level, PluginData pluginData) {
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
