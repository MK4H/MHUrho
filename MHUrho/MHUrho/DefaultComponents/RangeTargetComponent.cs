using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.DefaultComponents {

	public delegate void TargetMovedDelegate(IRangeTarget target);

	public interface IRangeTarget {
		int InstanceID { get; set; }

		bool Moving { get; }

		event TargetMovedDelegate TargetMoved;

		Vector3 CurrentPosition { get; }

		/// <summary>
		/// Gets waypoints begining with the current position with time 0,
		/// and all following waypoints to the end of the path
		/// </summary>
		/// <returns></returns>
		IEnumerable<Waypoint> GetFutureWaypoints();

		void AddShooter(RangeTargetComponent.IShooter shooter);

		void RemoveShooter(RangeTargetComponent.IShooter shooter);
	}

	public abstract class RangeTargetComponent : DefaultComponent, IRangeTarget {

		public interface IShooter {
			void OnTargetDestroy(IRangeTarget target);
		}


		public int InstanceID { get; set; }

		public abstract bool Moving { get; }

		/// <summary>
		/// Invoked every time target moves.
		/// </summary>
		public event TargetMovedDelegate TargetMoved;

		public abstract Vector3 CurrentPosition { get; }

		protected List<IShooter> shooters;

		protected RangeTargetComponent(ILevelManager level) 
			: base(level)
		{
			shooters = new List<IShooter>();
		}

		protected RangeTargetComponent(int ID, ILevelManager level) 
			: base(level)
		{
			this.InstanceID = ID;
			shooters = new List<IShooter>();
		}

		public abstract IEnumerable<Waypoint> GetFutureWaypoints();

		public void SignalTargetMoved(IEntity entity)
		{
			try {
				TargetMoved?.Invoke(this);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(TargetMoved)}: {e.Message}");
			}
		}

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


		/// <summary>
		/// Called on destruction of component, basically a destructor
		/// </summary>
		protected override void OnDeleted() {
			base.OnDeleted();

			foreach (var shooter in shooters) {
				try {
					shooter.OnTargetDestroy(this);
				}
				catch (Exception e) {
					Urho.IO.Log.Write(LogLevel.Error, $"There was an unexpected exception in {nameof(shooter.OnTargetDestroy)}: {e.Message}");
				}
			}

			Level.UnRegisterRangeTarget(InstanceID);
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(RangeTargetComponent), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(RangeTargetComponent), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}

	}
}
