using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using MHUrho.PathFinding;
using MHUrho.PathFinding.AStar;
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

			readonly LevelManager level;
			readonly InstancePlugin plugin;
			readonly StDefaultComponent storedData;

			public Loader() {

			}

			protected Loader(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				this.level = level;
				this.plugin = plugin;
				this.storedData = storedData;
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

			public override void StartLoading() {
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

			public override void ConnectReferences() {

			}

			public  override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				return new Loader(level, plugin, storedData);
			}
		}

		/// <summary>
		/// Interface that must be implemented by the plugin of the entity using this component.
		/// </summary>
		public interface IUser {
			/// <summary>
			/// Gets node distance calculator.
			/// This method may be called many times.
			/// Every received instance will not be used after unit is moved by WorldWalker.
			/// </summary>
			/// <returns>Node distance calculator.</returns>
			INodeDistCalculator GetNodeDistCalculator();
		}

		
		/// <summary>
		/// State of the last request executed by this WorldWalker.
		/// </summary>
		public WorldWalkerState State { get; private set; }

		/// <summary>
		/// Current path the WorldWalker is following, or null if WorldWalker is not moving
		/// </summary>
		public Path Path { get; private set; }

		/// <summary>
		/// Invoked on start of a new movement.
		/// </summary>
		public event MovementStartedDelegate MovementStarted;

		/// <summary>
		/// Invoked on successful completition of a movement request
		/// </summary>
		public event MovementEndedDelegate MovementFinished;

		/// <summary>
		/// Invoked on a failure of movement, either there is currently no path
		/// to the provided target or path calculation failed.
		/// </summary>
		public event MovementFailedDelegate MovementFailed;

		/// <summary>
		/// Invoked on user cancellation of current movement.
		/// </summary>
		public event MovementCanceledDelegate MovementCanceled;

		/// <summary>
		/// Invoked on every path recalculation. Recalculation happens on failure to move along
		/// previously calculated path.
		/// </summary>
		public event PathRecalculatedDelegate PathRecalculated;

		/// <summary>
		/// The unit this WorldWalker moves around the world.
		/// </summary>
		public IUnit Unit => (IUnit) Entity;

		/// <summary>
		/// The plugin providing an implementation of the required methods.
		/// </summary>
		readonly IUser user;

		
		/// <summary>
		/// Creates new WorldWalker and attaches it to the Unit and its node.
		/// </summary>
		/// <typeparam name="T">Unit plugin that implements the <see cref="IUser"/> interface required for this component.</typeparam>
		/// <param name="plugin">Unit plugin of the unit this WorldWalker should be attached to.</param>
		/// <param name="level">Current level.</param>
		/// <returns>The newly created WorldWalker instance.</returns>
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

		/// <summary>
		/// Tries to start movement towards the <paramref name="targetNode"/>. Returns true if path was found, false if no path was found.
		/// </summary>
		/// <param name="targetNode">Target of the movement.</param>
		/// <returns>Returns true if path was found, false if no path was found.</returns>
		public bool GoTo(INode targetNode)
		{
			if (!TryGetNodeDistCalculator(out INodeDistCalculator nodeDistCalc)) {
				StopMovement(WorldWalkerState.Failed);
				return false;
			}

			var newPath = Path.FromTo(Unit.Position, 
									targetNode, 
									Level.Map,
									nodeDistCalc);


			if (newPath == null) {
				StopMovement(WorldWalkerState.Failed);
				return false;
			}
			else {
				StopMovement(WorldWalkerState.Canceled);
				StartMovement(newPath);
				return true;
			}
		}

		/// <summary>
		/// Stops the current movement if there is any
		/// </summary>
		public void Stop()
		{		
			StopMovement(WorldWalkerState.Canceled);		
		}

		/// <summary>
		/// Returns the current position and the part of the path that has not been reached yet.
		/// </summary>
		/// <returns>Remaining part of the path to be walked in the form of waypoints.</returns>
		public IEnumerable<Waypoint> GetRestOfThePath()
		{
			return GetRestOfThePath(new Vector3(0, 0, 0));
		}

		/// <summary>
		/// Returns the current position and the part of the path that has not been reached yet, each waypoint offset by <paramref name="offset"/>.
		/// </summary>
		/// <param name="offset">Offset from the unit feet position that every Waypoint position will be translated by</param>
		/// <returns>Returns the current position and the part of the path that has not been reached yet, each waypoint offset by <paramref name="offset"/>.</returns>
		public IEnumerable<Waypoint> GetRestOfThePath(Vector3 offset)
		{
			return Path?.GetRestOfThePath(offset) ?? new [] {new Waypoint(new TempNode(Unit.Position, Level.Map), 0, MovementType.Linear).WithOffset(offset)};
		}

		/// <summary>
		/// Scene update, checked that the level is started and the updates of this unit are enabled.
		/// </summary>
		/// <param name="timeStep">Time from the previous update.</param>
		protected override void OnUpdateChecked(float timeStep)
		{
			Debug.Assert(Path != null, "Target was null with scene updates enabled");

			//Move towards the next waypoint
			if (!MoveTowards(Path.TargetWaypoint, timeStep)) {
				return;
			}

			if (TryGetNodeDistCalculator(out INodeDistCalculator nodeDistCalc)) {
				StopMovement(WorldWalkerState.Failed);
				return;
			}

			//Signal to the path that waypoint was reached (MoveTowards returned true), check if it was the last
			if (!Path.WaypointReached(nodeDistCalc)) {
				//Last waypoint was reached
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
		/// <param name="waypoint">Waypoint to move towards</param>
		/// <param name="timeStep">TimeStep of the game, time from previous update.</param>
		/// <returns>If unit reached the waypoint</returns>
		bool MoveTowards(Waypoint waypoint, float timeStep) {

			if (TryGetNodeDistCalculator(out INodeDistCalculator nodeDistCalc))
			{
				StopMovement(WorldWalkerState.Failed);
				return false;
			}

			switch (waypoint.MovementType) {
				case MovementType.None:
					return false;
				case MovementType.Teleport:
					if (Path.Update(Unit.Position, timeStep, nodeDistCalc)) {
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

					if (Path.Update(newPosition, timeStep, nodeDistCalc)) {
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

			//Unit couldn't move to newPosition

			if (TryGetNodeDistCalculator(out nodeDistCalc)) {
				StopMovement(WorldWalkerState.Failed);
				return false;
			}

			//Recalculate path
			var newPath = Path.FromTo(Unit.Position,
									Path.GetTarget(),
									Level.Map,
									nodeDistCalc);

			if (newPath == null) {
				//Cant get there
				StopMovement(WorldWalkerState.Failed);
			}
			else {
				InvokeOnPathRecalculated(Path, newPath);
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
			InvokeOnMovementStarted();
		}

		void StopMovement(WorldWalkerState endState)
		{
			State = endState;
			switch (endState) {
				case WorldWalkerState.Finished:
					InvokeOnMovementFinished();
					break;
				case WorldWalkerState.Failed:
					InvokeOnMovementFailed();
					break;
				case WorldWalkerState.Canceled:
					if (Path != null) {
						InvokeOnMovementCanceled();
					}			
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(endState), endState, null);
			}
			Path = null;
			Enabled = false;
		}

		bool TryGetNodeDistCalculator(out INodeDistCalculator nodeDistCalculator)
		{
			try {
				nodeDistCalculator = user.GetNodeDistCalculator();
				return nodeDistCalculator != null;
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"There was an unexpected exception in {nameof(user.GetNodeDistCalculator)}: {e.Message}");
				nodeDistCalculator = null;
				return false;
			}
		}

		void InvokeOnMovementStarted()
		{
			try {
				MovementStarted?.Invoke(this);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(MovementStarted)}: {e.Message}");
			}
		}

		void InvokeOnMovementFinished(){
			try
			{
				MovementFinished?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(MovementFinished)}: {e.Message}");
			}
		}

		void InvokeOnMovementFailed()
		{
			try
			{
				MovementFailed?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(MovementFailed)}: {e.Message}");
			}
		}

		void InvokeOnMovementCanceled()
		{
			try
			{
				MovementCanceled?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(MovementCanceled)}: {e.Message}");
			}
		}

		void InvokeOnPathRecalculated(Path oldPath, Path newPath)
		{
			try
			{
				PathRecalculated?.Invoke(this, oldPath, newPath);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(PathRecalculated)}: {e.Message}");
			}
		}
	}
}
