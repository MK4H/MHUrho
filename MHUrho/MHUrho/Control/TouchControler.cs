using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Control
{
    public class TouchControler : Component
    {
        private CameraControler cameraControler;
        private readonly Input input;

        private readonly Dictionary<int, Vector2> activeTouches = new Dictionary<int, Vector2>();

        private Vector2 movement;

        private const float NearZero = 0.001f;

        public float Sensitivity { get; set; }


        private void TouchBegin(TouchBeginEventArgs e) {
            activeTouches.Add(e.TouchID, new Vector2(e.X, e.Y));
        }

        private void TouchEnd(TouchEndEventArgs e) {
            activeTouches.Remove(e.TouchID);
        }

        private void TouchMove(TouchMoveEventArgs e) {
            try {
                var prevPosition = activeTouches[e.TouchID];

                //TODO: Modes
                var delta = (new Vector2(e.X, e.Y) - prevPosition) * Sensitivity;
                movement += delta;
            }
            catch (KeyNotFoundException) {
                Urho.IO.Log.Write(LogLevel.Warning, "TouchID was not valid in TouchMove");
            }
        }

        //TODO: This
        public override void OnSetEnabled() {
            base.OnSetEnabled();
        }

        protected override void OnUpdate(float timeStep) {
            if (timeStep > 0 && movement.LengthFast > NearZero) {
                //I want up on the screen to mean forward
                //TODO: Setting to invert axis
                var moveBy = new Vector2(movement.X, -movement.Y);
                cameraControler.MoveHorizontal(moveBy * timeStep);
                movement /= (1 + 2 * timeStep);
            }
        }

        public TouchControler(CameraControler cameraControler, Input input, float sensitivity = 0.001f) {
            this.cameraControler = cameraControler;
            this.input = input;
            this.Sensitivity = sensitivity;
            this.ReceiveSceneUpdates = true;

            RegisterHandlers();
        }

        private void RegisterHandlers() {
            input.TouchBegin += TouchBegin;
            input.TouchEnd += TouchEnd;
            input.TouchMove += TouchMove;
        }
    }
}
