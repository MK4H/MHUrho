using System;
using System.Collections.Generic;
using System.Text;

using Urho;

namespace MHUrho.Control
{
    public class CameraControler : Component {

        public bool SmoothMovement { get; set; } = true;

        public bool ApplyDrag { get; set; } = true;

        /// <summary>
        /// How much does the camera slow down per tick
        /// </summary>
        public float Drag { get; set; } = 2;

        public Vector3 Movement => movement;

        public float Yaw => rotation.Y;

        public float Pitch => rotation.X;

        
        private readonly Node cameraNode;
        private readonly Camera camera;

        private Vector3 movement;

        /// <summary>
        /// Only yaw and pitch, no roll
        /// </summary>
        private Vector2 rotation;

        private const float NearZero = 0.001f;

        public CameraMovementType MovementType { get; set; } = CameraMovementType.Horizontal;

        public CameraControler(Camera camera) {
            cameraNode = camera.Node;
            this.camera = camera;
            cameraNode.Position = new Vector3(0, 10, -5);
            cameraNode.LookAt(new Vector3(0, 0, 1), new Vector3(0, 1, 0));

            this.ReceiveSceneUpdates = true;
        }


        public void AddVerticalMovement(float movement) {
            this.movement.Y += movement;
        }

        public void SetVerticalSpeed(float movement) {
            this.movement.Y = movement;
        }

        public void AddHorizontalMovement(Vector2 movement) {
            this.movement.X += movement.X;
            this.movement.Z += movement.Y;
        }

        public void SetHorizontalMovement(Vector2 movement) {
            this.movement.X = movement.X;
            this.movement.Z = movement.Y;
        }

        public void AddMovement(Vector3 movement) {
            this.movement += movement;
        }

        public void SetMovement(Vector3 movement) {
            this.movement = movement;
        }

        
        public void AddYaw(float yaw) {
            rotation.Y += yaw;
        }

        public void SetYaw(float yaw) {
            rotation.Y = yaw;
        }

        public void AddPitch(float pitch) {
            rotation.X += pitch;
        }

        public void SetPitch(float pitch) {
            rotation.X = pitch;
        }

        public void AddRotation(Vector2 rotation) {
            this.rotation += rotation;
        }

        public void SetRotation(Vector2 rotation) {
            this.rotation = rotation;
        }

        protected override void OnUpdate(float timeStep) {
            if (timeStep > 0 && (movement.LengthFast > NearZero || rotation.LengthFast > NearZero)) {

                Vector3 tickMovement;
                Vector2 tickRotation;
                if (SmoothMovement) {
                    tickMovement = movement * timeStep;
                    tickRotation = rotation * timeStep;
                }
                else {
                    tickMovement = movement;
                    tickRotation = rotation;
                    movement = Vector3.Zero;
                    rotation = Vector2.Zero;
                }

                switch (MovementType) {
                    case CameraMovementType.Horizontal:
                        MoveHorizontal(new Vector2(tickMovement.X, tickMovement.Z));
                        break;
                    case CameraMovementType.Vertical:
                        MoveVertical(tickMovement.Y);
                        break;
                    case CameraMovementType.InLookDirection:
                        MoveRelativeToLookingDirection(tickMovement);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported Camera Movement Type");

                }

                RotateCamera(tickRotation);

                if (ApplyDrag) {
                    movement /= (1 + Drag * timeStep);
                    rotation /= (1 + Drag * timeStep);
                }
            }
        }

        /// <summary>
        /// Moves camera in the XZ plane, parallel to the ground
        /// X axis is right(+)/ left(-), 
        /// Y axis is in the direction of camera(+)/ in the direction opposite of the camera
        /// </summary>
        /// <param name="delta">Movement of the camera</param>
        private void MoveHorizontal(Vector2 delta) {
            var delta3D = new Vector3(delta.X, 0, delta.Y);
            var rotation = new Quaternion(0, cameraNode.Rotation.YawAngle, 0);

            var worldDelta = rotation * delta3D;
            cameraNode.Translate(worldDelta,TransformSpace.World);
        }

        /// <summary>
        /// Moves camera in the Y axis, + is up, - is down
        /// </summary>
        /// <param name="delta">Amount of movement</param>
        private void MoveVertical(float delta) {
            var position = cameraNode.Position;
            position.Y += delta;
            cameraNode.Position = position;
        }

        private void MoveRelativeToLookingDirection(Vector3 delta) {
            var worldDelta = cameraNode.Rotation * delta;
            cameraNode.Translate(worldDelta);
        }

        private void MoveInLookingDirection(float delta) {
            var delta3D = new Vector3(0, 0, delta);
            var worldDelta = cameraNode.Rotation * delta3D;
            cameraNode.Translate(worldDelta);
        }

        private void RotateCamera(Vector2 rot) {
            cameraNode.Rotate(new Quaternion(rot.X, rot.Y, 0));
        }
        

        
    }
}
