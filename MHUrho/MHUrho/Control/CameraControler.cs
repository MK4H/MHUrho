using System;
using System.Collections.Generic;
using System.Text;

using Urho;

namespace MHUrho.Control
{
    public class CameraControler {
        private readonly Node cameraNode;
        private readonly Camera camera;

        /// <summary>
        /// Moves camera in the XZ plane, parallel to the ground
        /// X axis is right(+)/ left(-), 
        /// Y axis is in the direction of camera(+)/ in the direction opposite of the camera
        /// </summary>
        /// <param name="delta">Movement of the camera</param>
        public void MoveHorizontal(Vector2 delta) {
            var delta3D = new Vector3(delta.X, 0, delta.Y);
            var rotation = new Quaternion(0, cameraNode.Rotation.YawAngle, 0);

            var worldDelta = rotation * delta3D;
            cameraNode.Translate(worldDelta,TransformSpace.World);
        }

        /// <summary>
        /// Moves camera in the Y axis, + is up, - is down
        /// </summary>
        /// <param name="delta">Amount of movement</param>
        public void MoveVertical(float delta) {
            var position = cameraNode.Position;
            position.Y += delta;
            cameraNode.Position = position;
        }

        public void MoveRelativeToLookingDirection(Vector3 delta) {
            var worldDelta = cameraNode.Rotation * delta;
            cameraNode.Translate(worldDelta);
        }

        public void MoveInLookingDirection(float delta) {
            var delta3D = new Vector3(0, 0, delta);
            var worldDelta = cameraNode.Rotation * delta3D;
            cameraNode.Translate(worldDelta);
        }


        public CameraControler(Camera camera) {
            cameraNode = camera.Node;
            this.camera = camera;
            cameraNode.Position = new Vector3(0, 10, -5);
            cameraNode.LookAt(new Vector3(0, 0, 1), new Vector3(0, 1, 0));

        }
    }
}
