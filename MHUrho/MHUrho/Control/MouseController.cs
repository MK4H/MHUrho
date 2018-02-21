using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.IO;

namespace MHUrho.Control
{
    public class MouseController
    {
        private enum CameraMovementType { Fixed, FreeFloat}

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
        
        public bool MouseBorderCameraMovement { get; set; }

        private CameraController cameraController;
        private readonly Input input;

        private CameraMovementType cameraType;

        private List<KeyAction> actions;
        private Dictionary<Key, Actions> keyActions;

        private float KeyRotationSensitivity => KeyCameraSensitivity * 3;

        public MouseController(CameraController cameraController, Input input, float mouseSensitivity = 0.1f, float keySensitivity = 5f) {
            this.cameraController = cameraController;
            this.input = input;
            this.MouseCameraSensitivity = mouseSensitivity;
            this.KeyCameraSensitivity = keySensitivity;
            this.cameraType = CameraMovementType.Fixed;

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
                {Key.F, Actions.CameraRotationDown },
                {Key.LeftShift, Actions.CameraSwitchMode }
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
            if (cameraType == CameraMovementType.FreeFloat) {

            }
        }

        private void MouseWheel(MouseWheelEventArgs e) {

        }

        private void StartCameraMoveLeft(int qualifiers) {
            var movement = cameraController.Movement;
            movement.X = -KeyCameraSensitivity;
            cameraController.SetMovement(movement);
        }

        private void StopCameraMoveLeft(int qualifiers) {
            var movement = cameraController.Movement;
            if (movement.X == -KeyCameraSensitivity) {
                movement.X = 0;
            }
            cameraController.SetMovement(movement);
        }

        private void StartCameraMoveRight(int qualifiers) {
            var movement = cameraController.Movement;
            movement.X = KeyCameraSensitivity;
            cameraController.SetMovement(movement);
        }

        private void StopCameraMoveRight(int qualifiers) {
            var movement = cameraController.Movement;
            if (movement.X == KeyCameraSensitivity) {
                movement.X = 0;
            }
            cameraController.SetMovement(movement);
        }

        private void StartCameraMoveForward(int qualifiers) {
            var movement = cameraController.Movement;
            movement.Z = KeyCameraSensitivity;
            cameraController.SetMovement(movement);
        }

        private void StopCameraMoveForward(int qualifiers) {
            var movement = cameraController.Movement;
            if (movement.Z == KeyCameraSensitivity) {
                movement.Z = 0;
            }
            cameraController.SetMovement(movement);
        }

        private void StartCameraMoveBackward(int qualifiers) {
            var movement = cameraController.Movement;
            movement.Z = -KeyCameraSensitivity;
            cameraController.SetMovement(movement);
        }

        private void StopCameraMoveBackward(int qualifiers) {
            var movement = cameraController.Movement;
            if (movement.Z == -KeyCameraSensitivity) {
                movement.Z = 0;
            }
            cameraController.SetMovement(movement);
        }

        private void StartCameraRotationRight(int qualifiers) {
            cameraController.SetYaw(-KeyRotationSensitivity);
        }

        private void StopCameraRotationRight(int qualifiers) {
            if (cameraController.Yaw == -KeyRotationSensitivity) {
                cameraController.SetYaw(0);
            }
        }

        private void StartCameraRotationLeft(int qualifiers) {
            cameraController.SetYaw(KeyRotationSensitivity);
        }

        private void StopCameraRotationLeft(int qualifiers) {
            if (cameraController.Yaw == KeyRotationSensitivity) {
                cameraController.SetYaw(0);
            }
        }

        private void StartCameraRotationUp(int qualifiers) {
            cameraController.SetPitch(-KeyRotationSensitivity);
        }

        private void StopCameraRotationUp(int qualifiers) {
            if (cameraController.Pitch == -KeyRotationSensitivity) {
                cameraController.SetPitch(0);
            }
        }

        private void StartCameraRotationDown(int qualifiers) {
            cameraController.SetPitch(KeyRotationSensitivity);
        }

        private void StopCameraRotationDown(int qualifiers) {
            if (cameraController.Pitch == KeyRotationSensitivity) {
                cameraController.SetPitch(0);
            }
        }

        private void CameraSwitchMode(int qualifiers) {
            if (cameraType == CameraMovementType.FreeFloat) {
                cameraController.SwitchToFixed();
                cameraType = CameraMovementType.Fixed;
            }
            else {
                cameraController.SwitchToFree();
                cameraType = CameraMovementType.FreeFloat;
            }
        }
    }
}
