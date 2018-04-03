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
        public static string ComponentName = "WorldWalker";

        public override string Name => ComponentName;

        /// <summary>
        /// If the unit should try and own the tile it stops at
        /// If true, it tries and if it fails, pathfinds to the closest empty tile
        /// If false, the unit just stays as a passing unit
        /// Default true
        /// </summary>
        public bool OwnTileAtReachingTarget { get; set; } = true;

        public event MovementStartedDelegate OnMovementStarted;
        public event MovementEndedDelegate OnMovementEnded;
        public event MovementFailedDelegate OnMovementFailed;

        private Map map;
        private LevelManager level;
        private Unit unit;

        private Path path;

        private ITile target;

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
            this.target = target;
            this.Enabled = activated;
        }

        public void GoAlong(Path path) {
            if (this.path == null) {
                OnMovementStarted?.Invoke(unit);
            }

            this.path = path;
            if (!path.MoveNext()) {
                //TODO: cannot enumerate path
                throw new ArgumentException("Given path couldnt be enumerated");
            }

            target = map.GetTile(path.Current);
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
            return GoTo(map.GetTile(location));
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
            Debug.Assert(target != null, "Target was null with scene updates enabled");

            if (map.GetContainingTile(unit.Position) != target) {
                MoveTowards(target.Center3, timeStep);
                return;
            }

            if (!MoveToMiddle(timeStep)) return;

            //Middle was reached
            if (!path.MoveNext()) {
                //Reached destination
                Path newPath = null;
                if (OwnTileAtReachingTarget && !path.Target.TryAddOwningUnit(unit)) {
                    newPath = map.GetPath(unit, map.FindClosestEmptyTile(path.Target));
                }

                path.Dispose();
                target = null;
                Enabled = false;

                if (newPath != null) {
                    GoAlong(newPath);
                }
                else {
                    OnMovementEnded?.Invoke(unit);
                    path = null;
                }

                return;
            }

            target = map.GetTile(path.Current);
        }

        /// <summary>
        /// Moves unit to the middle of the Tile in Tile property
        /// </summary>
        /// <param name="elapsedSeconds"></param>
        /// <returns>If middle was reached</returns>
        private bool MoveToMiddle(float elapsedSeconds) {
            if (target.Center3 == unit.Position) return true;

            Vector3 newPosition = unit.Position + GetMoveVector(target.Center3, elapsedSeconds);
            if (Vector3.Distance(unit.Position, target.Center3) < Vector3.Distance(newPosition, target.Center3)) {
                unit.MoveTo(target.Center3);
                return true;
            }
            else {
                unit.MoveTo(newPosition);
                return false;
            }
        }

        /// <summary>
        /// Moves unit towards the destination
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="elapsedSeconds"></param>
        private void MoveTowards(Vector3 destination, float elapsedSeconds) {
            if (!unit.MoveBy(GetMoveVector(destination, elapsedSeconds))) {
                //Recalculate path
                var newPath = map.GetPath(unit, path.Target);
                if (newPath == null || !newPath.MoveNext()) {
                    //Cant get there
                    throw new NotImplementedException();
                }

                path.Dispose();
                path = newPath;
            }
        }

        /// <summary>
        /// Calculates by how much should the unit move
        /// </summary>
        /// <param name="destination">The point in space that the unit is trying to get to</param>
        /// <param name="elapsedSeconds"> How many seconds passed since the last update</param>
        /// <returns></returns>
        private Vector3 GetMoveVector(Vector3 destination, float elapsedSeconds) {
            Vector3 movementDirection = destination - unit.Position;
            movementDirection.Normalize();
            return movementDirection * LevelManager.CurrentLevel.GameSpeed * elapsedSeconds;
        }

        public override PluginData SaveState() {
            var storageData = new IndexedPluginDataWriter();
            if (Enabled) {
                storageData.Store(1,true);

                storageData.Store(2, path);
                storageData.Store(3, target.Location);
            }
            else {
                storageData.Store(1, false);
            }

            return storageData.PluginData;
        }

        public static WorldWalker Load(LevelManager level, PluginData data) {
            var indexedData = new IndexedPluginDataReader(data);
            var activated = indexedData.Get<bool>(1);
            Path path = null;
            ITile target = null;
            if (activated) {
                path = indexedData.Get<Path>(2);
                target = level.Map.GetTile(indexedData.Get<IntVector2>(3));
            }

            return new WorldWalker(level, activated, path, target);
        }
    }
}
