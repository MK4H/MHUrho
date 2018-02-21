using System;
using System.Collections.Generic;
using System.Text;

using Urho;

namespace MHUrho.Control
{
    public class CameraController : Component {

        public bool SmoothMovement { get; set; } = true;

        public bool ApplyDrag { get; set; } = true;

        public bool FreeFloat { get; private set; } = false;

        /// <summary>
        /// How much does the camera slow down per tick
        /// </summary>
        public float Drag { get; set; } = 2;

        public Vector3 Movement => movement;

        public float Yaw => rotation.Y;

        public float Pitch => rotation.X;

        public Camera Camera { get; private set; }

        /// <summary>
        /// Point on the ground
        /// Camera follows this point at constant offset while not in FreeFloat mode
        /// </summary>
        private Node cameraHolder;
        /// <summary>
        /// Node of the camera itself
        /// </summary>
        private Node cameraNode;
        

        private Vector3 movement;

        /// <summary>
        /// Only yaw and pitch, no roll
        /// </summary>
        private Vector2 rotation;

        private Vector3 fixedPosition;
        private Quaternion fixedRotation;

        private const float NearZero = 0.001f;

        public static CameraController GetCameraController(Scene scene) {
            Node cameraHolder = scene.CreateChild(name: "CameraHolder");
            Node cameraNode = cameraHolder.CreateChild(name: "camera");
            Camera camera = cameraNode.CreateComponent<Camera>();

            CameraController controller = cameraNode.CreateComponent<CameraController>();
            controller.cameraHolder = cameraHolder;
            controller.cameraNode = cameraNode;
            controller.Camera = camera;
            

            cameraNode.Position = new Vector3(0, 10, -5);
            cameraNode.LookAt(cameraHolder.Position, Vector3.UnitY);

            return controller;
        }

        public CameraController() {
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

        public void SwitchToFree() {
            if (!FreeFloat) {
                FreeFloat = true;
                //Save the fixed position relative to holder
                fixedPosition = cameraNode.Position;
                fixedRotation = cameraNode.Rotation;

                //Set it right above the holder
                cameraNode.Position = new Vector3(0, cameraNode.Position.Y, 0);
            }
        }

        public void SwitchToFixed() {
            if (FreeFloat) {
                FreeFloat = false;
                //Restore the fixed position relative to holder
                cameraNode.Position = fixedPosition;
                cameraNode.Rotation = fixedRotation;
            }
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

                if (FreeFloat) {
                    MoveRelativeToLookingDirection(tickMovement);
                    RotateCameraFree(tickRotation);
                }
                else {
                    MoveHorizontal(tickMovement.X, tickMovement.Z);
                    MoveVertical(tickMovement.Y);
                    RotateCameraFixed(tickRotation);
                }

                if (ApplyDrag) {
                    movement /= (1 + Drag * timeStep);
                    rotation /= (1 + Drag * timeStep);
                }
            }
        }

        /// <summary>
        /// Moves camera in the XZ plane, parallel to the ground
        /// X axis is right(+)/ left(-), 
        /// Z axis is in the direction of camera(+)/ in the direction opposite of the camera
        /// </summary>
        /// <param name="deltaX">Movement of the camera in left/right direction</param>
        /// <param name="deltaZ">Movement of the camera in forward/backward direction</param>
        private void MoveHorizontal(float deltaX, float deltaZ) {
            var delta3D = new Vector3(deltaX, 0, deltaZ);
            var rotation = Quaternion.FromAxisAngle(cameraHolder.Up,-cameraHolder.Rotation.YawAngle);

            var worldDelta = rotation * delta3D;
            cameraHolder.Translate(worldDelta,TransformSpace.World);
        }

        /// <summary>
        /// Moves camera in the Y axis, + is up, - is down
        /// </summary>
        /// <param name="delta">Amount of movement</param>
        private void MoveVertical(float delta) {
            var position = cameraNode.Position;
            position.Y += delta;
            cameraNode.Position = position;
            cameraNode.LookAt(cameraHolder.Position, Vector3.UnitY);
        }

        private void MoveRelativeToLookingDirection(Vector3 delta) {

            /*delta = Quaternion.FromAxisAngle(cameraNode.Direction, cameraNode.Rotation.RollAngle) * 
                    Quaternion.FromAxisAngle(cameraNode.Up, cameraNode.Rotation.YawAngle) * 
                    Quaternion.FromAxisAngle(cameraNode.Right, cameraNode.Rotation.PitchAngle) * 
                    delta;*/

            cameraNode.Translate(delta);
        }

        private void RotateCameraFixed(Vector2 rot) {
            cameraHolder.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, rot.Y));
            cameraNode.Rotate(Quaternion.FromAxisAngle(Vector3.UnitX, rot.X),TransformSpace.Parent);
        }

        private void RotateCameraFree(Vector2 rot) {
            cameraNode.Rotate(Quaternion.FromAxisAngle(cameraNode.Right, rot.X));
            cameraNode.Rotate(Quaternion.FromAxisAngle(cameraNode.Up, rot.Y));
        }
        
    }
}
