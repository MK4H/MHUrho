using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Control
{
    public class GameTouchController : TouchController, IGameController
    {
        private enum CameraMovementType { Horizontal, Vertical, FreeFloat }


        public float Sensitivity { get; set; }
        public bool ContinuousMovement { get; private set; } = true;



        private CameraController cameraController;

        private CameraMovementType movementType;

        private readonly Dictionary<int, Vector2> activeTouches = new Dictionary<int, Vector2>();


        

        public GameTouchController(CameraController cameraController, MyGame game, float sensitivity = 0.1f) : base(game) {
            this.cameraController = cameraController;
            this.Sensitivity = sensitivity;

            //SwitchToContinuousMovement();
            SwitchToDiscontinuousMovement();
            movementType = CameraMovementType.Horizontal;

            Enable();
        }

        public void SwitchToContinuousMovement() {
            ContinuousMovement = true;

            cameraController.SmoothMovement = true;
        }

        public void SwitchToDiscontinuousMovement() {
            ContinuousMovement = false;

            cameraController.SmoothMovement = true;
            
        }

        protected override void TouchBegin(TouchBeginEventArgs e) {
            activeTouches.Add(e.TouchID, new Vector2(e.X, e.Y));
        }

        protected override void TouchEnd(TouchEndEventArgs e) {
            activeTouches.Remove(e.TouchID);

            cameraController.AddHorizontalMovement(cameraController.StaticHorizontalMovement);
            cameraController.HardResetHorizontalMovement();
        }

        protected override void TouchMove(TouchMoveEventArgs e) {
            try {
                switch (movementType) {
                    case CameraMovementType.Horizontal:
                        HorizontalMove(e);
                        break;
                    case CameraMovementType.Vertical:
                        break;
                    case CameraMovementType.FreeFloat:
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported CameraMovementType in TouchController");
                }

            }
            catch (KeyNotFoundException) {
                Urho.IO.Log.Write(LogLevel.Warning, "TouchID was not valid in TouchMove");
            }
        }


        private void HorizontalMove(TouchMoveEventArgs e) {
            if (ContinuousMovement) {
                var movement = (new Vector2(e.X, e.Y) - activeTouches[e.TouchID]) * Sensitivity;
                movement.Y = -movement.Y;
                cameraController.SetHorizontalMovement(movement);
            }
            else {
                var movement = new Vector2(e.DX, -e.DY) * Sensitivity;
                cameraController.AddHorizontalMovement(movement);
            }
        }


    }
}
