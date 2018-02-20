using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.IO;

namespace MHUrho.Control
{
    public class MouseController
    {

        private class KeyAction {
            public Action<int> KeyDown;
            public Action<int> Repeat;
            public Action<int> KeyUp;

            public KeyAction(Action<int> keyDown, Action<int> repeat, Action<int> keyUp) {
                this.KeyDown = keyDown;
                this.Repeat = repeat;
                this.KeyUp = keyUp;
            }
        }

        public float MouseMapSensitivity { get; set; }

        public float KeyMapSensitivity { get; set; }
        
        public bool MouseCameraMovement { get; set; }

        private CameraControler cameraControler;
        private readonly Input input;

        private Dictionary<Key, KeyAction> keyActions;

        public MouseController(CameraControler cameraControler, Input input, float mouseSensitivity = 0.1f, float keySensitivity = 3f) {
            this.cameraControler = cameraControler;
            this.input = input;
            this.MouseMapSensitivity = mouseSensitivity;
            this.KeyMapSensitivity = keySensitivity;

            //TODO: Load from config
            SetKeyBindings();

            RegisterCallbacks();
        }

        void SetKeyBindings() {
            keyActions = new Dictionary<Key, KeyAction>{
                { Key.W, new KeyAction(StartMoveCameraForward,null,StopMoveCameraForward) },
                { Key.S, new KeyAction(StartMoveCameraBackward, null, StopMoveCameraBackward) },
                { Key.A, new KeyAction(StartMoveCameraLeft, null, StopMoveCameraLeft) },
                { Key.D, new KeyAction(StartMoveCameraRight, null, StopMoveCameraRight) }
            };
        }

        private void RegisterCallbacks() {
            input.KeyUp += KeyUp;
            input.KeyDown += KeyDown;
            input.MouseButtonDown += MouseButtonDown;
            input.MouseButtonUp += MouseButtonUp;
            input.MouseMoved += MouseMoved;
            input.MouseWheel += MouseWheel;
        }

        private void KeyDown(KeyDownEventArgs e) {
            if (keyActions.TryGetValue(e.Key, out KeyAction action)) {
                if (!e.Repeat) {
                    action.KeyDown?.Invoke(e.Qualifiers);
                }
                else {
                    action.Repeat?.Invoke(e.Qualifiers);
                }
            }
        }

        private void KeyUp(KeyUpEventArgs e) {
            if (keyActions.TryGetValue(e.Key, out KeyAction action)) {
                action.KeyUp?.Invoke(e.Qualifiers);
            }
        }

        private void MouseButtonDown(MouseButtonDownEventArgs e) {

        }

        private void MouseButtonUp(MouseButtonUpEventArgs e) {

        }

        private void MouseMoved(MouseMovedEventArgs e) {

        }

        private void MouseWheel(MouseWheelEventArgs e) {

        }

        private void StartMoveCameraLeft(int qualifiers) {
            var movement = cameraControler.Movement;
            movement.X = -KeyMapSensitivity;
            cameraControler.SetMovement(movement);
        }

        private void StopMoveCameraLeft(int qualifiers) {
            var movement = cameraControler.Movement;
            if (movement.X == -KeyMapSensitivity) {
                movement.X = 0;
            }
            cameraControler.SetMovement(movement);
        }

        private void StartMoveCameraRight(int qualifiers) {
            var movement = cameraControler.Movement;
            movement.X = KeyMapSensitivity;
            cameraControler.SetMovement(movement);
        }

        private void StopMoveCameraRight(int qualifiers) {
            var movement = cameraControler.Movement;
            if (movement.X == KeyMapSensitivity) {
                movement.X = 0;
            }
            cameraControler.SetMovement(movement);
        }

        private void StartMoveCameraForward(int qualifiers) {
            var movement = cameraControler.Movement;
            movement.Z = KeyMapSensitivity;
            cameraControler.SetMovement(movement);
        }

        private void StopMoveCameraForward(int qualifiers) {
            var movement = cameraControler.Movement;
            if (movement.Z == KeyMapSensitivity) {
                movement.Z = 0;
            }
            cameraControler.SetMovement(movement);
        }

        private void StartMoveCameraBackward(int qualifiers) {
            var movement = cameraControler.Movement;
            movement.Z = -KeyMapSensitivity;
            cameraControler.SetMovement(movement);
        }

        private void StopMoveCameraBackward(int qualifiers) {
            var movement = cameraControler.Movement;
            if (movement.Z == -KeyMapSensitivity) {
                movement.Z = 0;
            }
            cameraControler.SetMovement(movement);
        }
    }
}
