using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using MHUrho.Storage;
using Urho;

namespace MHUrho.UnitComponents
{
    public delegate void MovementStartedDelegate(Unit unit);

    public delegate void MovementEndedDelegate(Unit unit);

    public delegate void MovementFailedDelegate(Unit unit);

    public class WorldWalker : DefaultComponent {

        public static string ComponentName = nameof(WorldWalker);
        public static DefaultComponents ComponentID = DefaultComponents.WorldWalker;

        public override string Name => ComponentName;
        public override DefaultComponents ID => ComponentID;

        public event MovementStartedDelegate OnMovementStarted;
        public event MovementEndedDelegate OnMovementEnded;
        public event MovementFailedDelegate OnMovementFailed;

        private readonly Map map;
        private LevelManager level;
        private Unit unit;

        private Path path;

        private ITile nextTile;
        private Vector3 nextWaypoint;

        public WorldWalker(LevelManager level) {
            ReceiveSceneUpdates = true;
            this.level = level;
            this.map = level.Map;
            Enabled = false;
        }

        protected WorldWalker(LevelManager level, bool activated, Path path, ITile target) {

            ReceiveSceneUpdates = true;
            this.level = level;
            this.map = level.Map;
            this.path = path;
            this.nextTile = target;
            this.Enabled = activated;
        }

        public static WorldWalker Load(LevelManager level, PluginData data) {
            var indexedData = new IndexedPluginDataReader(data);
            var activated = indexedData.Get<bool>(1);
            Path path = null;
            ITile target = null;
            if (activated) {
                path = indexedData.Get<Path>(2);
                target = level.Map.GetTileByMapLocation(indexedData.Get<IntVector2>(3));
            }

            return new WorldWalker(level, activated, path, target);
        }

        public override PluginData SaveState() {
            var storageData = new IndexedPluginDataWriter();
            if (Enabled) {
                storageData.Store(1, true);

                storageData.Store(2, path);
                storageData.Store(3, nextTile.MapLocation);
            }
            else {
                storageData.Store(1, false);
            }

            return storageData.PluginData;
        }

        public void GoAlong(Path path) {
            if (this.path == null) {
                OnMovementStarted?.Invoke(unit);
            }

            this.path = path;
            if (!path.MoveNext()) {
                //TODO: cannot enumerate path
                throw new ArgumentException("Given path could not be enumerated");
            }

            nextTile = map.GetTileByMapLocation(path.Current);
            nextWaypoint = nextTile.Center3;

            nextWaypoint = GetNextWaypoint();

            Enabled = true;
        }

        public bool GoTo(ITile tile) {
            var newPath = map.GetPath(unit, tile);
            if (newPath == null) {
                OnMovementFailed?.Invoke(unit);
                return false;
            }
            GoAlong(newPath);
            return true;
        }

        public bool GoTo(IntVector2 location) {
            return GoTo(map.GetTileByMapLocation(location));
        }


        public WorldWalker OnMovementStartedCall(MovementStartedDelegate handler) {
            OnMovementStarted += handler;
            return this;
        }

        public WorldWalker OnMovementFinishedCall(MovementEndedDelegate handler) {
            OnMovementEnded += handler;
            return this;
        }

        public WorldWalker OnMovementFailedCall(MovementFailedDelegate handler) {
            OnMovementFailed += handler;
            return this;
        }

        public override void OnAttachedToNode(Node node) {
            base.OnAttachedToNode(node);

            unit = Node.GetComponent<Unit>();

            if (unit == null) {
                throw new
                    InvalidOperationException($"Cannot attach {nameof(WorldWalker)} to a node that does not have {nameof(Unit)} component");
            }
        }

        protected override void OnUpdate(float timeStep) {
            base.OnUpdate(timeStep);
            //TODO: WHY DO I HAVE TO CHECK THIS MANUALLY ?
            if (EnabledEffective == false) return;
            Debug.Assert(nextTile != null, "Target was null with scene updates enabled");


            if (!MoveTowards(nextWaypoint,timeStep)) {
                return;
            }

            //nextWaypoint was reached
            nextWaypoint = GetNextWaypoint();
        }


        /// <summary>
        /// Moves unit towards the <paramref name="point"/>
        /// </summary>
        /// <param name="point">Point to move towards</param>
        /// <param name="timeStep">timeStep of the game</param>
        private bool MoveTowards(Vector3 point ,float timeStep) {
            bool reachedPoint = false;

            Vector3 newPosition = unit.Position + GetMoveVector(point, timeStep);
            if (ReachedPoint(Node.Position, newPosition, point)) {
                newPosition = point;
                reachedPoint = true;
            }

            if (unit.MoveTo(newPosition)) {
                //Unit could move
                return reachedPoint;
            }
            //Unit couldnt move to newPosition

            //Recalculate path
            var newPath = map.GetPath(unit, path.Target);
            if (newPath == null || !newPath.MoveNext()) {
                //Cant get there
                OnMovementFailed?.Invoke(unit);
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
            Vector3 movementDirection = destination - unit.Position;
            movementDirection.Normalize();
            return movementDirection * LevelManager.CurrentLevel.GameSpeed * timeStep;
        }

        private bool ReachedPoint(Vector3 currentPosition, Vector3 nextPosition, Vector3 point) {
            return Vector3.Distance(currentPosition, point) < Vector3.Distance(nextPosition, point);
        }

        private Vector3 GetNextWaypoint() {
            if (nextWaypoint == nextTile.Center3) {
                if (!path.MoveNext()) {
                    //Reached destination

                    OnMovementEnded?.Invoke(unit);
                    ReachedDestination();
                    return new Vector3();
                }

                nextTile = map.GetTileByMapLocation(path.Current);
                var nextWaypointXZ = (nextWaypoint.XZ2() + nextTile.Center) / 2;

                return new Vector3(nextWaypointXZ.X, map.GetHeightAt(nextWaypointXZ), nextWaypointXZ.Y);
            }

            return nextTile.Center3;

        }

        private void ReachedDestination() {
            path.Dispose();
            nextTile = null;
            nextWaypoint = new Vector3();
            Enabled = false;
        }
    }
}
