using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.IO;

namespace MHUrho.Control
{
    public class MouseController
    {
        private enum Actions {  CameraMoveForward = 0,
                                CameraMoveBackward,
                                CameraMoveLeft,
                                CameraMoveRight,
                                CameraRotationRight,
                                CameraRotationLeft,
                                CameraRotationUp,
                                CameraRotationDown,
                                CameraSwitchMode
        }

        private struct KeyAction {
            public Action<int> KeyDown;
            public Action<int> Repeat;
            public Action<int> KeyUp;

            public KeyAction(Action<int> keyDown, Action<int> repeat, Action<int> keyUp) {
                this.KeyDown = keyDown;
                this.Repeat = repeat;
                this.KeyUp = keyUp;
            }
        }

        

        public float MouseCameraSensitivity { get; set; }

        public float KeyCameraSensitivity { get; set; }
        
        public bool MouseCameraMovement { get; set; }

        private CameraControler cameraControler;
        private readonly Input input;


        private List<KeyAction> actions;
        private Dictionary<Key, Actions> keyActions;

        private float KeyRotationSensitivity => KeyCameraSensitivity * 3;

        public MouseController(CameraControler cameraControler, Input input, float mouseSensitivity = 0.1f, float keySensitivity = 3f) {
            this.cameraControler = cameraControler;
            this.input = input;
            this.MouseCameraSensitivity = mouseSensitivity;
            this.KeyCameraSensitivity = keySensitivity;

            FillActionList();

            //TODO: Load from config
            SetKeyBindings();

            RegisterCallbacks();
        }

        void FillActionList() {
            actions = new List<KeyAction> {
                new KeyAction(StartCameraMoveForward, null, StopCameraMoveForward),
                new KeyAction(StartCameraMoveBackward, null, StopCameraMoveBackward),
                new KeyAction(StartCameraMoveLeft, null, StopCameraMoveLeft),
                new KeyAction(StartCameraMoveRight, null, StopCameraMoveRight),
                new KeyAction(StartCameraRotationRight, null, StopCameraRotationRight),
                new KeyAction(StartCameraRotationLeft, null, StopCameraRotationLeft),
                new KeyAction(StartCameraRotationUp, null, StopCameraRotationUp),
                new KeyAction(StartCameraRotationDown, null, StopCameraRotationDown),
                new KeyAction(CameraSwitchMode, null, null)
            };
        }
        
        //TODO: Load from config
        void SetKeyBindings() {
            keyActions = new Dictionary<Key, Actions> {
                {Key.W, Actions.CameraMoveForward},
                {Key.S, Actions.CameraMoveBackward},
                {Key.A, Actions.CameraMoveLeft},
                {Key.D, Actions.CameraMoveRight},
                {Key.E, Actions.CameraRotationRight},
                {Key.Q, Actions.CameraRotationLeft},
                {Key.R, Actions.CameraRotationUp },
                {Key.F, Actions.CameraRotationDown }
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

        private KeyAction GetAction(Actions action) {
            return actions[(int)action];
        }

        private void KeyDown(KeyDownEventArgs e) {
            if (keyActions.TryGetValue(e.Key, out Actions action)) {
                if (!e.Repeat) {
                    GetAction(action).KeyDown?.Invoke(e.Qualifiers);
                }
                else {
                    GetAction(action).Repeat?.Invoke(e.Qualifiers);
                }
            }
        }

        private void KeyUp(KeyUpEventArgs e) {
            if (keyActions.TryGetValue(e.Key, out Actions action)) {
                GetAction(action).KeyUp?.Invoke(e.Qualifiers);
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

        private void StartCameraMoveLeft(int qualifiers) {
            var movement = cameraControler.Movement;
            movement.X = -KeyCameraSensitivity;
            cameraControler.SetMovement(movement);
        }

        private void StopCameraMoveLeft(int qualifiers) {
            var movement = cameraControler.Movement;
            if (movement.X == -KeyCameraSensitivity) {
                movement.X = 0;
            }
            cameraControler.SetMovement(movement);
        }

        private void StartCameraMoveRight(int qualifiers) {
            var movement = cameraControler.Movement;
            movement.X = KeyCameraSensitivity;
            cameraControler.SetMovement(movement);
        }

        private void StopCameraMoveRight(int qualifiers) {
            var movement = cameraControler.Movement;
            if (movement.X == KeyCameraSensitivity) {
                movement.X = 0;
            }
            cameraControler.SetMovement(movement);
        }

        private void StartCameraMoveForward(int qualifiers) {
            var movement = cameraControler.Movement;
            movement.Z = KeyCameraSensitivity;
            cameraControler.SetMovement(movement);
        }

        private void StopCameraMoveForward(int qualifiers) {
            var movement = cameraControler.Movement;
            if (movement.Z == KeyCameraSensitivity) {
                movement.Z = 0;
            }
            cameraControler.SetMovement(movement);
        }

        private void StartCameraMoveBackward(int qualifiers) {
            var movement = cameraControler.Movement;
            movement.Z = -KeyCameraSensitivity;
            cameraControler.SetMovement(movement);
        }

        private void StopCameraMoveBackward(int qualifiers) {
            var movement = cameraControler.Movement;
            if (movement.Z == -KeyCameraSensitivity) {
                movement.Z = 0;
            }
            cameraControler.SetMovement(movement);
        }

        private void StartCameraRotationRight(int qualifiers) {
            cameraControler.SetYaw(KeyRotationSensitivity);
        }

        private void StopCameraRotationRight(int qualifiers) {
            if (cameraControler.Yaw == KeyRotationSensitivity) {
                cameraControler.SetYaw(0);
            }
        }

        private void StartCameraRotationLeft(int qualifiers) {
            cameraControler.SetYaw(-KeyRotationSensitivity);
        }

        private void StopCameraRotationLeft(int qualifiers) {
            if (cameraControler.Yaw == -KeyRotationSensitivity) {
                cameraControler.SetYaw(0);
            }
        }

        private void StartCameraRotationUp(int qualifiers) {
            cameraControler.SetPitch(KeyRotationSensitivity);
        }

        private void StopCameraRotationUp(int qualifiers) {
            if (cameraControler.Pitch == KeyRotationSensitivity) {
                cameraControler.SetPitch(0);
            }
        }

        private void StartCameraRotationDown(int qualifiers) {
            cameraControler.SetPitch(-KeyRotationSensitivity);
        }

        private void StopCameraRotationDown(int qualifiers) {
            if (cameraControler.Pitch == -KeyRotationSensitivity) {
                cameraControler.SetPitch(0);
            }
        }

        private void CameraSwitchMode(int qualifiers) {

        }
    }
}
