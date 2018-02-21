using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Control
{
    public class TouchControler
    {
        private enum CameraMovementType { Horizontal, Vertical, FreeFloat }


        public float Sensitivity { get; set; }
        public bool ContinuousMovement { get; private set; } = true;

        private CameraController cameraController;
        private readonly Input input;

        private CameraMovementType movementType;

        private readonly Dictionary<int, Vector2> activeTouches = new Dictionary<int, Vector2>();

        

        public TouchControler(CameraController cameraController, Input input, float sensitivity = 0.1f) {
            this.cameraController = cameraController;
            this.input = input;
            this.Sensitivity = sensitivity;

            SwitchToContinuousMovement();
            //SwitchToDiscontinuousMovement();
            movementType = CameraMovementType.Horizontal;

            RegisterHandlers();
        }

        public void SwitchToContinuousMovement() {
            ContinuousMovement = true;

            cameraController.ApplyDrag = false;
            cameraController.SmoothMovement = true;
        }

        public void SwitchToDiscontinuousMovement() {
            ContinuousMovement = false;

            cameraController.ApplyDrag = true;
            cameraController.SmoothMovement = true;
            
        }

        private void TouchBegin(TouchBeginEventArgs e) {
            activeTouches.Add(e.TouchID, new Vector2(e.X, e.Y));

            if (ContinuousMovement) {
                cameraController.ApplyDrag = false;
            }
        }

        private void TouchEnd(TouchEndEventArgs e) {
            activeTouches.Remove(e.TouchID);

            if (ContinuousMovement) {
                cameraController.ApplyDrag = true;
            }
        }

        private void TouchMove(TouchMoveEventArgs e) {
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



        private void RegisterHandlers() {
            input.TouchBegin += TouchBegin;
            input.TouchEnd += TouchEnd;
            input.TouchMove += TouchMove;
        }
    }
}
