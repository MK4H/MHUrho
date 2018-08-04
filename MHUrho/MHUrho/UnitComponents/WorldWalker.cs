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

	public delegate void MovementStartedDelegate(WorldWalker walker);

	public delegate void MovementEndedDelegate(WorldWalker walker);

	public delegate void MovementCanceledDelegate(WorldWalker walker);

	public delegate void MovementFailedDelegate(WorldWalker walker);

	public delegate void PathRecalculatedDelegate(WorldWalker walker, Path oldPath, Path newPath);


	public enum WorldWalkerState {
		Started,
		Finished,
		Failed,
		Canceled
	}

	public class WorldWalker : DefaultComponent {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => Walker;

			public WorldWalker Walker { get; private set; }

			public Loader() {

			}

			public static StDefaultComponent SaveState(WorldWalker walker)
			{
				var storedWalker = new StWorldWalker
									{
										Enabled = walker.Enabled,
										Path = walker.Path?.Save()
									};
				return new StDefaultComponent {WorldWalker = storedWalker};
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData) {
				var user = plugin as IUser;
				if (user == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(IUser)} interface", nameof(plugin));
				}

				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.WorldWalker) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedWorldWalker = storedData.WorldWalker;

				Walker = new WorldWalker(user, 
										level, 
										storedWorldWalker.Enabled,
										storedWorldWalker.Path != null ? Path.Load(storedWorldWalker.Path, level) : null);

			}

			public override void ConnectReferences(LevelManager level) {

			}

			public  override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
			}
		}

		public interface IUser {

			bool GetTime(INode from, INode to, out float time);


			/// <summary>
			/// Used as a heuristic for the A* algorithm
			///
			/// It has to be admissible, which means it must not overestimate the time
			/// GetMinimalAproxTime has to always be lower than the optimal path time
			///
			/// But the closer you get it to the optimal time, the faster the A* will run
			///
			/// If it is not admissible, the returned path may not be optimal and the runtime of A* may be longer
			///
			/// If the <paramref name="from"/> and <paramref name="to"/> are in the same Tile, it should return the optimal time
			/// as that will be used for the movement
			/// </summary>
			/// <param name="from"></param>
			/// <param name="to"></param>
			/// <returns></returns>
			float GetMinimalAproxTime(Vector3 from, Vector3 to);
		}

		

		public WorldWalkerState State { get; private set; }

		public Path Path { get; private set; }

		public event MovementStartedDelegate OnMovementStarted;
		public event MovementEndedDelegate OnMovementEnded;
		public event MovementFailedDelegate OnMovementFailed;
		public event MovementCanceledDelegate OnMovementCanceled;
		public event PathRecalculatedDelegate OnPathRecalculated;


		public IUnit Unit => (IUnit) Entity;

		readonly IUser user;

		

		public static WorldWalker CreateNew<T>(T plugin, ILevelManager level)
			where T : UnitInstancePlugin, IUser {

			if (plugin == null) {
				throw new ArgumentNullException(nameof(plugin));
			}

			var newInstance = new WorldWalker(plugin, level);
			plugin.Entity.AddComponent(newInstance);
			return newInstance;
		}

		protected WorldWalker(IUser user,ILevelManager level) 
			:base(level)
		{
			ReceiveSceneUpdates = true;

			Enabled = false;

			this.user = user;
		}

		protected WorldWalker(IUser user, ILevelManager level, bool activated, Path path)
			:base(level)
		{

			ReceiveSceneUpdates = true;
			this.Path = path;
			this.Enabled = activated;
			this.user = user;
		}




		public override StDefaultComponent SaveState()
		{
			return Loader.SaveState(this);
		}

		public void GoAlong(Path newPath) {
			if (Path != null) {
				StopMovement(WorldWalkerState.Canceled);
				
			}
			
			StartMovement(newPath);
		}

		public bool GoTo(INode targetNode)
		{
			var newPath = Path.FromTo(Unit.Position, 
									targetNode, 
									Map, 
									user.GetTime,
									user.GetMinimalAproxTime);
			if (newPath == null) {
				Stop();
				StopMovement(WorldWalkerState.Failed);
				return false;
			}
			else {
				GoAlong(newPath);
				return true;
			}
		}

		public void Stop()
		{
			if (State == WorldWalkerState.Started) {
				StopMovement(WorldWalkerState.Canceled);
			}

			State = WorldWalkerState.Canceled;
		}

		public IEnumerable<Waypoint> GetRestOfThePath()
		{
			return GetRestOfThePath(new Vector3(0, 0, 0));
		}

		/// <summary>
		/// Returns the current position and the part of the path that has not been reached yet
		/// </summary>
		/// <param name="offset">Offset from the unit feet position that every Waypoint position will be transfered by</param>
		/// <returns>Returns the current position and the part of the path that has not been reached yet</returns>
		public IEnumerable<Waypoint> GetRestOfThePath(Vector3 offset)
		{
			return Path?.GetRestOfThePath(offset) ?? new [] {new Waypoint(new TempNode(Unit.Position), 0, MovementType.Linear).WithOffset(offset)};
		}



		protected override void OnUpdateChecked(float timeStep)
		{
			Debug.Assert(Path != null, "Target was null with scene updates enabled");


			if (!MoveTowards(Path.TargetWaypoint, timeStep)) {
				return;
			}

			if (!Path.WaypointReached(user.GetTime)) {
				StopMovement(WorldWalkerState.Finished);
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
					if (Path.Update(Unit.Position, timeStep, user.GetTime)) {
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


					if (Path.Update(newPosition, timeStep, user.GetTime)) {
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
									Path.GetTarget(),
									Map,
									user.GetTime,
									 user.GetMinimalAproxTime);

			if (newPath == null) {
				//Cant get there
				StopMovement(WorldWalkerState.Failed);
			}
			else {
				OnPathRecalculated?.Invoke(this, Path, newPath);
				Path = newPath;
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

		void StartMovement(Path newPath)
		{
			Path = newPath;
			Enabled = true;
			State = WorldWalkerState.Started;
			OnMovementStarted?.Invoke(this);
		}

		void StopMovement(WorldWalkerState endState)
		{
			State = endState;
			switch (endState) {
				case WorldWalkerState.Finished:
					OnMovementEnded?.Invoke(this);
					break;
				case WorldWalkerState.Failed:
					OnMovementFailed?.Invoke(this);
					break;
				case WorldWalkerState.Canceled:
					OnMovementCanceled?.Invoke(this);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(endState), endState, null);
			}
			Path = null;
			Enabled = false;
		}
	}
}
