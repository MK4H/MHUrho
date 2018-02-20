using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Control
{
    public class TouchControler
    {

        public float Sensitivity { get; set; }
        public bool ContinuousMovement { get; private set; } = true;

        private CameraControler cameraControler;
        private readonly Input input;

        private readonly Dictionary<int, Vector2> activeTouches = new Dictionary<int, Vector2>();

        

        public TouchControler(CameraControler cameraControler, Input input, float sensitivity = 0.1f) {
            this.cameraControler = cameraControler;
            this.input = input;
            this.Sensitivity = sensitivity;

            SwitchToContinuousMovement();
            cameraControler.MovementType = CameraMovementType.Horizontal;

            RegisterHandlers();
        }

        public void SwitchToContinuousMovement() {
            ContinuousMovement = true;

            cameraControler.ApplyDrag = false;
            cameraControler.SmoothMovement = true;
        }

        public void SwitchToDiscontinuousMovement() {
            ContinuousMovement = false;

            cameraControler.ApplyDrag = true;
            cameraControler.SmoothMovement = true;
            
        }

        private void TouchBegin(TouchBeginEventArgs e) {
            activeTouches.Add(e.TouchID, new Vector2(e.X, e.Y));

            if (ContinuousMovement) {
                cameraControler.ApplyDrag = false;
            }
        }

        private void TouchEnd(TouchEndEventArgs e) {
            activeTouches.Remove(e.TouchID);

            if (ContinuousMovement) {
                cameraControler.ApplyDrag = true;
            }
        }

        private void TouchMove(TouchMoveEventArgs e) {
            try {
                
                if (ContinuousMovement) {
                    var movement = new Vector2(e.DX, -e.DY) * Sensitivity;
                    cameraControler.AddHorizontalMovement(movement);
                }
                else {
                    var movement = (new Vector2(e.X, e.Y) - activeTouches[e.TouchID]) * Sensitivity;
                    cameraControler.SetHorizontalMovement(movement);
                }

            }
            catch (KeyNotFoundException) {
                Urho.IO.Log.Write(LogLevel.Warning, "TouchID was not valid in TouchMove");
            }
        }

       

        private void RegisterHandlers() {
            input.TouchBegin += TouchBegin;
            input.TouchEnd += TouchEnd;
            input.TouchMove += TouchMove;
        }
    }
}
