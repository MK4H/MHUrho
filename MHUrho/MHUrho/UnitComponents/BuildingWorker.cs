using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    

    public class BuildingWorker : DefaultComponent {

        private interface IWorkTask {
            bool IsFinished();
            void OnUpdate(float timeStep);
            void InvokeTaskStarted(Unit unit);
            void InvokeTaskFinished(Unit unit);
            
        }

        public class WorkTask : IWorkTask {
            public delegate void TaskHandler(Unit unit);

            public event TaskHandler TaskStarted;
            public event TaskHandler TaskFinished;

            private float duration;

            bool IWorkTask.IsFinished() {
                return duration <= 0;
            }

            void IWorkTask.OnUpdate(float timeStep) {
                duration -= timeStep;
            }

            

            void IWorkTask.InvokeTaskStarted(Unit unit) {
                TaskStarted?.Invoke(unit);
            }

            void IWorkTask.InvokeTaskFinished(Unit unit) {
                TaskFinished?.Invoke(unit);
            }
        }

        public static string ComponentName = nameof(BuildingWorker);

        public override string Name => ComponentName;

        public Building Building { get; private set; }

        private Queue<IWorkTask> workQueue;

        private Unit unit;

        public static BuildingWorker Load(LevelManager level, PluginData pluginData) {

        }

        public override PluginData SaveState() {
            throw new NotImplementedException();
        }

        protected override void OnUpdate(float timeStep) {
            base.OnUpdate(timeStep);

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
