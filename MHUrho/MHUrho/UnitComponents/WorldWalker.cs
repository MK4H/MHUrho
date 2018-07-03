using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.UnitComponents
{

	internal delegate void MovementStartedDelegate(WorldWalker walker);

	internal delegate void MovementEndedDelegate(WorldWalker walker);

	internal delegate void MovementFailedDelegate(WorldWalker walker);

	public class WorldWalker : DefaultComponent {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => Walker;

			public WorldWalker Walker { get; private set; }

			public Loader() {

			}

			public static PluginData SaveState(WorldWalker walker) {
				var storageData = new IndexedPluginDataWriter(walker.Level);
				if (walker.Enabled) {
					storageData.Store(1, true);

					storageData.Store(2, walker.path);
				}
				else {
					storageData.Store(1, false);
				}

				return storageData.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData) {
				var notificationReceiver = plugin as INotificationReceiver;
				if (notificationReceiver == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
				}

				var indexedData = new IndexedPluginDataReader(storedData, level);
				var activated = indexedData.Get<bool>(1);
				Path path = null;
				if (activated) {
					path = indexedData.Get<Path>(2);
				}

				Walker = new WorldWalker(notificationReceiver, level, activated, path);

			}

			public override void ConnectReferences(LevelManager level) {

			}

			public  override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
			}
		}

		public interface INotificationReceiver {

			bool GetTime(INode from, INode to, out float time);

			float GetMinimalAproximatedTime(Vector3 from, Vector3 to);

			void OnMovementStarted(WorldWalker walker);

			void OnMovementFinished(WorldWalker walker);

			void OnMovementFailed(WorldWalker walker);
		}

		public static string ComponentName = nameof(WorldWalker);
		public static DefaultComponents ComponentID = DefaultComponents.WorldWalker;

		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		public bool MovementStarted { get; private set; }
		public bool MovementFinished { get; private set; }
		public bool MovementFailed { get; private set; }

		internal event MovementStartedDelegate OnMovementStarted;
		internal event MovementEndedDelegate OnMovementEnded;
		internal event MovementFailedDelegate OnMovementFailed;


		public IUnit Unit => (IUnit) Entity;

		INotificationReceiver notificationReceiver;

		Path path;

		public static WorldWalker GetInstanceFor<T>(T instancePlugin, ILevelManager level)
			where T : UnitInstancePlugin, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new WorldWalker(instancePlugin, level);
		}

		protected WorldWalker(INotificationReceiver notificationReceiver,ILevelManager level) 
			:base(level)
		{
			ReceiveSceneUpdates = true;
			this.notificationReceiver = notificationReceiver;
			Enabled = false;

			OnMovementStarted += notificationReceiver.OnMovementStarted;
			OnMovementEnded += notificationReceiver.OnMovementFinished;
			OnMovementFailed += notificationReceiver.OnMovementFailed;
		}

		protected WorldWalker(INotificationReceiver notificationReceiver, ILevelManager level, bool activated, Path path)
			:base(level)
		{

			ReceiveSceneUpdates = true;
			this.notificationReceiver = notificationReceiver;
			this.path = path;
			this.Enabled = activated;
		}




		public override PluginData SaveState()
		{
			return Loader.SaveState(this);
		}

		public void GoAlong(Path newPath) {
			if (path == null) {
				MovementStarted = true;
				MovementFailed = false;
				MovementFinished = false;

				OnMovementStarted?.Invoke(this);
			}

			path = newPath;

			Enabled = true;			
		}

		public bool GoTo(INode targetNode) {
			var newPath = Path.FromTo(Unit.Position, 
									targetNode, 
									Map, 
									notificationReceiver.GetTime,
									notificationReceiver.GetMinimalAproximatedTime);
			if (newPath == null) {
				MovementStarted = true;
				OnMovementStarted?.Invoke(this);
				MovementFailed = true;
				OnMovementFailed?.Invoke(this);
				return false;
			}
			GoAlong(newPath);
			return true;
		}

		public void Stop()
		{
			ReachedDestination();
		}

		public IEnumerator<Waypoint> GetRestOfThePath()
		{
			return GetRestOfThePath(new Vector3(0, 0, 0));
		}

		/// <summary>
		/// Returns the current position and the part of the path that has not been reached yet
		/// </summary>
		/// <param name="offset">Offset from the unit feet position that every Waypoint position will be transfered by</param>
		/// <returns>Returns the current position and the part of the path that has not been reached yet</returns>
		public IEnumerator<Waypoint> GetRestOfThePath(Vector3 offset)
		{
			return path?.GetEnumerator(offset) ?? ((IEnumerable<Waypoint>)new [] {new Waypoint(new TempNode(Unit.Position), 0, MovementType.Linear).WithOffset(offset)}).GetEnumerator();
		}



		protected override void OnUpdateChecked(float timeStep)
		{
			Debug.Assert(path != null, "Target was null with scene updates enabled");


			if (!MoveTowards(path.TargetWaypoint, timeStep)) {
				return;
			}

			if (!path.WaypointReached(notificationReceiver.GetTime)) {
				ReachedDestination();
			}
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);

			if (Entity == null || !(Entity is Unit)) {
				throw new
					InvalidOperationException($"Cannot attach {nameof(WorldWalker)} to a node that does not have {nameof(Logic.Unit)} component");
			}

			AddedToEntity(typeof(WorldWalker), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(WorldWalker), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}

		/// <summary>
		/// Moves unit towards the <paramref name="point"/>
		/// </summary>
		/// <param name="waypoint">waypoint to move towards</param>
		/// <param name="timeStep">timeStep of the game</param>
		/// <returns>If unit reached the waypoint</returns>
		bool MoveTowards(Waypoint waypoint, float timeStep) {

			switch (waypoint.MovementType) {
				case MovementType.None:
					return false;
				case MovementType.Teleport:
					if (path.Update(Unit.Position, timeStep, notificationReceiver.GetTime)) {
						//Still can teleport

						//Check timeout
						if (waypoint.TimeToWaypoint > 0) return false;
						
						//Teleport timeout finished
						Unit.MoveTo(waypoint.Position);
						return true;
					}
					//Cannot teleport, something in the map changed, recalculate path

					break;
				default:
					//Default to linear movement
					Vector3 newPosition = Unit.Position + GetMoveVector(waypoint, timeStep);


					if (path.Update(newPosition, timeStep, notificationReceiver.GetTime)) {
						//Can still move towards the waypoint
						bool reachedWaypoint = false;
						if (ReachedPoint(Unit.Position, newPosition, waypoint.Position)) {
							newPosition = waypoint.Position;
							reachedWaypoint = true;
						}
						Unit.MoveTo(newPosition);
						return reachedWaypoint;
					}

					//Cannot move towards the waypoint, something in the map changed, recalculate path
					break;
			}

			//Unit couldnt move to newPosition

			//Recalculate path
			var newPath = Path.FromTo(Unit.Position,
									path.GetTarget(),
									Map,
									notificationReceiver.GetTime,
									 notificationReceiver.GetMinimalAproximatedTime);

			if (newPath == null) {
				//Cant get there
				MovementFailed = true;
				OnMovementFailed?.Invoke(this);
				ReachedDestination();
			}
			else {
				path = newPath;
			}
			return false;
		}

		/// <summary>
		/// Calculates by how much should the unit move
		/// </summary>
		/// <param name="waypoint">The waypoint to move towards</param>
		/// <param name="timeStep"> How many seconds passed since the last update</param>
		/// <returns>The change of units position to make it reach the waypoint in time</returns>
		Vector3 GetMoveVector(Waypoint waypoint, float timeStep) {
			Vector3 movementDirection = waypoint.Position - Unit.Position;

			float dist = movementDirection.Length;
			//If the destination is exactly equal to Unit.Position, prevent NaN from normalization
			// Reached point will proc and the returned value will be ignored, but cant be [0,0,0]
			if (movementDirection == new Vector3(0, 0, 0)) {
				return new Vector3(1, 0, 0);
			}
			movementDirection.Normalize();

			return movementDirection *
					timeStep *
					(dist / waypoint.TimeToWaypoint) ; // speed = distance / time
		}

		bool ReachedPoint(Vector3 currentPosition, Vector3 nextPosition, Vector3 point) {
			var currDiff = point - currentPosition;
			var nextDiff = point - nextPosition;
			return !(Math.Sign(currDiff.X) == Math.Sign(nextDiff.X) &&
					 Math.Sign(currDiff.Y) == Math.Sign(nextDiff.Y) &&
					 Math.Sign(currDiff.Z) == Math.Sign(nextDiff.Z));
		}

		void ReachedDestination() {
			MovementFinished = true;
			MovementStarted = false;
			MovementFailed = false;
			path = null;
			Enabled = false;
			OnMovementEnded?.Invoke(this);
		}
	}
}
