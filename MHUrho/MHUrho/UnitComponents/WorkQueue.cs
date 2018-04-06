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
            void InvokeTaskStarted(Unit unit);
            void InvokeTaskFinished(Unit unit);
            
        }

        public abstract class WorkTask : IWorkTask {
            bool IWorkTask.IsFinished() {
                return IsFinished();
            }

            void IWorkTask.OnUpdate(float timeStep) {
                throw new NotImplementedException();
            }

            void IWorkTask.InvokeTaskStarted(Unit unit) {
                throw new NotImplementedException();
            }

            void IWorkTask.InvokeTaskFinished(Unit unit) {
                throw new NotImplementedException();
            }

            protected abstract bool IsFinished();

            protected virtual void OnUpdate(float timeStep) {
                //NOTHING
            }

            protected abstract void InvokeTaskStarted(Unit unit);

            protected abstract void InvokeTaskFinished(Unit unit);
        }

        public class TimedWorkTask : WorkTask {
            
            public event Action<Unit, TimedWorkTask> TaskStarted;
            public event Action<Unit, TimedWorkTask> TaskFinished;

            private float duration;

            public TimedWorkTask(float duration) {
                this.duration = duration;
            }

            public TimedWorkTask OnTaskStarted(Action<Unit,TimedWorkTask> handler) {
                TaskStarted += handler;
                return this;
            }

            public TimedWorkTask OnTaskFinished(Action<Unit,TimedWorkTask> handler) {
                TaskFinished += handler;
                return this;
            }

            protected override bool IsFinished() {
                return duration <= 0;
            }

            protected override void OnUpdate(float timeStep) {
                duration -= timeStep;
            }

            protected override void InvokeTaskStarted(Unit unit) {
                TaskStarted?.Invoke(unit, this);
            }

            protected override void InvokeTaskFinished(Unit unit) {
                TaskFinished?.Invoke(unit, this);
            }
        }

        public class DelegatedWorkTask : WorkTask {
            public event Action<Unit, DelegatedWorkTask> TaskStarted;
            public event Action<Unit, DelegatedWorkTask> TaskFinished;

            private bool finished = false;

            public void Finish() {
                finished = true;
            }

            public DelegatedWorkTask OnTaskStarted(Action<Unit, DelegatedWorkTask> handler) {
                TaskStarted += handler;
                return this;
            }

            public DelegatedWorkTask OnTaskFinished(Action<Unit, DelegatedWorkTask> handler) {
                TaskFinished += handler;
                return this;
            }

            protected override bool IsFinished() {
                return finished;
            }

            protected override void InvokeTaskStarted(Unit unit) {
                TaskStarted?.Invoke(unit, this);
            }

            protected override void InvokeTaskFinished(Unit unit) {
                TaskFinished?.Invoke(unit, this);
            }
        }

        public static string ComponentName = nameof(WorkQueue);

        public override string Name => ComponentName;

        private readonly Queue<IWorkTask> workQueue;

        public WorkQueue(Building building) {
            this.Building = building;
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
                currentTask.InvokeTaskFinished(unit);
            }

            workQueue.Dequeue();

            if (workQueue.Count == 0) return;

            workQueue.Peek().InvokeTaskStarted(unit);
        }

        
    }
}
