using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.UnitComponents
{

	internal delegate void MovementStartedDelegate(WorldWalker walker);

	internal delegate void MovementEndedDelegate(WorldWalker walker);

	internal delegate void MovementFailedDelegate(WorldWalker walker);

	public class WorldWalker : DefaultComponent {

		public interface INotificationReceiver {
			bool CanGoFromTo(ITile fromTile, ITile toTile);

			float GetMovementSpeed(ITile onTile);

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


		public Unit Unit { get; private set; }

		private readonly Map map;
		private ILevelManager level;
		private INotificationReceiver notificationReceiver;

		private Path.PathEnumerator path;

		public static WorldWalker GetInstanceFor<T>(T instancePlugin, ILevelManager level)
			where T : UnitInstancePluginBase, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new WorldWalker(instancePlugin, level);
		}

		protected WorldWalker(INotificationReceiver notificationReceiver,ILevelManager level) {
			ReceiveSceneUpdates = true;
			this.notificationReceiver = notificationReceiver;
			this.level = level;
			this.map = level.Map;
			Enabled = false;
		}

		protected WorldWalker(INotificationReceiver notificationReceiver, ILevelManager level, bool activated, Path.PathEnumerator path, ITile target) {

			ReceiveSceneUpdates = true;
			this.notificationReceiver = notificationReceiver;
			this.level = level;
			this.map = level.Map;
			this.path = path;
			this.Enabled = activated;
		}

		internal static WorldWalker Load(ILevelManager level, InstancePluginBase plugin, PluginData data) {

			var notificationReceiver = plugin as INotificationReceiver;
			if (notificationReceiver == null) {
				throw new
					ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
			}

			var indexedData = new IndexedPluginDataReader(data);
			var activated = indexedData.Get<bool>(1);
			Path.PathEnumerator path = null;
			ITile target = null;
			if (activated) {
				path = indexedData.Get<Path.PathEnumerator>(2);
				target = level.Map.GetTileByMapLocation(indexedData.Get<IntVector2>(3));
				
			}

			return new WorldWalker(notificationReceiver, level, activated, path, target);
		}

		internal override void ConnectReferences(ILevelManager level) {

		}

		public override PluginData SaveState() {
			var storageData = new IndexedPluginDataWriter();
			if (Enabled) {
				storageData.Store(1, true);

				storageData.Store(2, path);
			}
			else {
				storageData.Store(1, false);
			}

			return storageData.PluginData;
		}

		public void GoAlong(Path newPath) {
			if (path == null) {
				MovementStarted = true;
				MovementFailed = false;
				MovementFinished = false;

				OnMovementStarted?.Invoke(this);
			}

			this.path = newPath.GetPathEnumerator();
			if (!path.MoveNext()) {
				//TODO: cannot enumerate path
				throw new ArgumentException("Given path could not be enumerated");
			}

			Enabled = true;			
		}

		public bool GoTo(ITile tile) {
			var newPath = Path.FromTo(Unit.Tile, 
									tile, 
									map, 
									notificationReceiver.CanGoFromTo,
									notificationReceiver.GetMovementSpeed);
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

		public bool GoTo(IntVector2 location) {
			return GoTo(map.GetTileByMapLocation(location));
		}

		public override void OnAttachedToNode(Node node) {
			base.OnAttachedToNode(node);

			Unit = Node.GetComponent<Unit>();

			if (Unit == null) {
				throw new
					InvalidOperationException($"Cannot attach {nameof(WorldWalker)} to a node that does not have {nameof(Logic.Unit)} component");
			}

			this.OnMovementStarted += notificationReceiver.OnMovementStarted;
			this.OnMovementEnded += notificationReceiver.OnMovementFinished;
			this.OnMovementFailed += notificationReceiver.OnMovementFailed;
		}

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);

			if (!EnabledEffective) return;
			Debug.Assert(path != null, "Target was null with scene updates enabled");


			if (!MoveTowards(path.Current.Position,timeStep)) {
				return;
			}

			if (!path.MoveNext()) {
				ReachedDestination();
			}
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			AddedToEntity(typeof(WorldWalker), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			return RemovedFromEntity(typeof(WorldWalker), entityDefaultComponents);
		}

		/// <summary>
		/// Moves unit towards the <paramref name="point"/>
		/// </summary>
		/// <param name="point">Point to move towards</param>
		/// <param name="timeStep">timeStep of the game</param>
		private bool MoveTowards(Vector3 point ,float timeStep) {
			bool reachedPoint = false;

			Vector3 newPosition = Unit.Position + GetMoveVector(point, timeStep);
			if (ReachedPoint(Unit.Position, newPosition, point)) {
				newPosition = point;
				reachedPoint = true;
			}

			if (Unit.MoveTo(newPosition)) {
				//Unit could move
				return reachedPoint;
			}
			//Unit couldnt move to newPosition

			//Recalculate path
			var newPath = Path.FromTo(Unit.Tile,
									path.Path.GetTarget(map),
									map,
									notificationReceiver.CanGoFromTo,
									notificationReceiver.GetMovementSpeed).GetPathEnumerator();
			if (newPath == null || !newPath.MoveNext()) {
				//Cant get there
				MovementFailed = true;
				OnMovementFailed?.Invoke(this);
				newPath?.Dispose();
				ReachedDestination();
			}
			else {
				path.Dispose();
				path = newPath;
			}
			return false;
		}

		/// <summary>
		/// Calculates by how much should the unit move
		/// </summary>
		/// <param name="destination">The point in space that the unit is trying to get to</param>
		/// <param name="timeStep"> How many seconds passed since the last update</param>
		/// <returns></returns>
		private Vector3 GetMoveVector(Vector3 destination, float timeStep) {
			Vector3 movementDirection = destination - Unit.Position;
			//If the destination is exactly equal to Unit.Position, prevent NaN from normalization
			// Reached point will proc and the returned value will be ignored, but cant be [0,0,0]
			if (movementDirection == new Vector3(0, 0, 0)) {
				return new Vector3(1, 0, 0);
			}
			movementDirection.Normalize();
			return movementDirection * LevelManager.CurrentLevel.GameSpeed * timeStep;
		}

		private bool ReachedPoint(Vector3 currentPosition, Vector3 nextPosition, Vector3 point) {
			var currDiff = point - currentPosition;
			var nextDiff = point - nextPosition;
			return !(Math.Sign(currDiff.X) == Math.Sign(nextDiff.X) &&
					 Math.Sign(currDiff.Y) == Math.Sign(nextDiff.Y) &&
					 Math.Sign(currDiff.Z) == Math.Sign(nextDiff.Z));
		}

		private void ReachedDestination() {
			MovementFinished = true;
			MovementStarted = false;
			MovementFailed = false;
			path.Dispose();
			path = null;
			Enabled = false;
			OnMovementEnded?.Invoke(this);
		}
	}
}
