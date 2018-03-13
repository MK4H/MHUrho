using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using Urho;

namespace MHUrho.UnitComponents
{
    class WorldWalker : Component
    {
        private Map map;
        private LevelManager level;

        private Path path;

        private ITile target;

        public WorldWalker(LevelManager level) {
            ReceiveSceneUpdates = false;
            this.level = level;
            this.map = level.Map;
        }

        public void GoAlong(Path path) {
            this.path = path;
            if (!path.MoveNext()) {
                //TODO: cannot enumerate path
                throw new ArgumentException("Given path couldnt be enumerated");
            }

            target = map.GetTile(path.Current);
            ReceiveSceneUpdates = true;
        }

        protected override void OnUpdate(float timeStep) {
            base.OnUpdate(timeStep);
            Debug.Assert(target != null, "Target was null with scene updates enabled");

            if (map.GetContainingTile(Node.Position) == target && MoveToMiddle(timeStep)) {
                if (!path.MoveNext()) {
                    //Reached destination
                    ReceiveSceneUpdates = false;
                    path.Dispose();
                    target = null;
                    return;
                }

                target = map.GetTile(path.Current);
            }
            else {
                MoveTowards(target.Center3, timeStep);
            }

        }

        /// <summary>
        /// Moves unit to the middle of the Tile in Tile property
        /// </summary>
        /// <param name="elapsedSeconds"></param>
        /// <returns>If middle was reached</returns>
        private bool MoveToMiddle(float elapsedSeconds) {
            Vector3 newPosition = Node.Position + GetMoveVector(target.Center3, elapsedSeconds);
            if (Vector3.Distance(Node.Position, target.Center3) < Vector3.Distance(Node.Position, newPosition)) {
                Node.Position = target.Center3;
                return true;
            }
            else {
                Node.Position = newPosition;
                return false;
            }
        }

        /// <summary>
        /// Moves unit towards the destination
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="elapsedSeconds"></param>
        private void MoveTowards(Vector3 destination, float elapsedSeconds) {
            var newPosition = Node.Position + GetMoveVector(destination, elapsedSeconds);

            ITile newTile = map.GetContainingTile(newPosition);
            if (newTile == target) {
                //Check if we can still pass through target, it could have changed between path calculation and now
                //TODO: THIS
            }
        }

        /// <summary>
        /// Calculates by how much should the unit move
        /// </summary>
        /// <param name="destination">The point in space that the unit is trying to get to</param>
        /// <param name="elapsedSeconds"> How many seconds passed since the last update</param>
        /// <returns></returns>
        private Vector3 GetMoveVector(Vector3 destination, float elapsedSeconds) {
            Vector3 MovementDirection = destination - Node.Position;
            MovementDirection.Normalize();
            return MovementDirection * LevelManager.CurrentLevel.GameSpeed * elapsedSeconds;
        }

        /// <summary>
        /// Radius of the circle around the middle that counts as the middle
        /// For float rounding errors
        /// </summary>
        private const float Tolerance = 0.1f;

        /// <summary>
        /// Checks if the unit is in the middle of the current tile
        /// </summary>
        /// <returns></returns>
        private bool AmInTheMiddle() {
            return Vector2.Subtract(target.Center, Node.Position.XZ2()).LengthFast < Tolerance;
        }
    }
}
