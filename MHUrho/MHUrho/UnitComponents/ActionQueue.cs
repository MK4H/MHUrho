using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => ActionQueue;

			public ActionQueue ActionQueue { get; private set; }

			public Loader() {

			}

			public static PluginData SaveState(ActionQueue actionQueue) {
				throw new NotImplementedException();
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData)
			{
				throw new NotImplementedException();
			}

			public override void ConnectReferences(LevelManager level) {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
			}
		}

		public interface INotificationReceiver {
			void OnWorkTaskStarted(ActionQueue queue, WorkTask workTask);

			void OnWorkTaskFinished(ActionQueue queue, WorkTask workTask);
		}

		interface IWorkTask {
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
			
			float duration;

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

			bool finished = false;

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

		readonly Queue<IWorkTask> workQueue;

		INotificationReceiver notificationReceiver;

		public ActionQueue CreateNew<T>(T instancePlugin, ILevelManager level)
			where T : InstancePlugin, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new ActionQueue(instancePlugin, level);
		}

		protected ActionQueue(INotificationReceiver notificationReceiver, ILevelManager level) 
			: base(level)
		{
			this.notificationReceiver = notificationReceiver;
			workQueue = new Queue<IWorkTask>();
		}

		public override PluginData SaveState()
		{
			return Loader.SaveState(this);
		}

		public void EnqueueTask(WorkTask task) {
			workQueue.Enqueue(task);
		}

		public override void OnAttachedToNode(Node node) {
			base.OnAttachedToNode(node);

			OnTaskStarted += notificationReceiver.OnWorkTaskStarted;
			OnTaskFinished += notificationReceiver.OnWorkTaskFinished;
		}

		protected override void OnUpdateChecked(float timeStep) {

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

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(ActionQueue), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(ActionQueue), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}
	}
}
