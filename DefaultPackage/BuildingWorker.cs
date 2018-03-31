using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;

namespace DefaultPackage {
    

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

        public Building Building { get; set; }

        private Queue<IWorkTask> workQueue;

        private Unit unit;

        public static BuildingWorker Load(LevelManager level, PluginData pluginData) {
            throw new NotImplementedException();
        }

        public override PluginDataWrapper SaveState() {
            throw new NotImplementedException();
        }

        public override DefaultComponent CloneComponent() {
            return new BuildingWorker();
        }

        public override void OnAttachedToNode(Node node) {
            base.OnAttachedToNode(node);

            unit = node.GetComponent<Unit>();

            if (unit == null) {
                throw new InvalidOperationException("BuildingWorker can only be attached to node with unit component");
            }
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
