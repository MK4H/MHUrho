using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.IO;

using MHUrho.Helpers;

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


        public float CameraScrollSensitivity { get; set; }

        public float CameraRotationSensitivity { get; set; }

        public float MouseSensitivity { get; set; }

        public bool MouseBorderCameraMovement { get; set; }

        private CameraController cameraController;
        private readonly Input input;
        private readonly Graphics graphics;


        private CameraMovementType cameraType;

        private List<KeyAction> actions;
        private Dictionary<Key, Actions> keyActions;

        //private float KeyRotationSensitivity => KeyCameraSensitivity * 3;

        private const float CloseToBorder = 1/20f;

        private bool mouseInLeftRight;
        private bool mouseInTopBottom;

        public MouseController( CameraController cameraController, 
                                Input input, Graphics graphics) {
            this.cameraController = cameraController;
            this.input = input;
            this.MouseSensitivity = 0.2f;
            this.CameraScrollSensitivity = 5f;
            this.CameraRotationSensitivity = 15f;
            this.cameraType = CameraMovementType.Fixed;
            this.graphics = graphics;

            cameraController.Drag = 10;

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
                cameraController.AddRotation(new Vector2(e.DY, -e.DX) * MouseSensitivity);
            }
            else if (cameraType == CameraMovementType.Fixed) {
                Log.Write(LogLevel.Debug, $"Mouse position: X={e.X}, Y={e.Y}");
                MouseBorderMovement(e);
            }
        }

        private void MouseWheel(MouseWheelEventArgs e) {

        }

        private void MouseBorderMovement(MouseMovedEventArgs e) {


            Vector2 cameraMovement = new Vector2(cameraController.StaticMovement.X,cameraController.StaticMovement.Z);

            if (!mouseInLeftRight ) {
                //Mouse was not in the border area before, check if it is now 
                // and if it is, set the movement
                if (e.X < graphics.Width * CloseToBorder) {
                    cameraMovement.X = -CameraScrollSensitivity;
                    mouseInLeftRight = true;
                }
                else if (e.X > graphics.Width * (1 - CloseToBorder)) {
                    cameraMovement.X = CameraScrollSensitivity;
                    mouseInLeftRight = true;
                }
                
            }
            else if (graphics.Width * CloseToBorder <= e.X && e.X <= graphics.Width * (1 - CloseToBorder)) {
                //Mouse was in the area, and now it is not, reset the movement
                cameraMovement.X = 0;
                mouseInLeftRight = false;
            }

            if (!mouseInTopBottom) {
                if (e.Y < graphics.Height * CloseToBorder) {
                    cameraMovement.Y = CameraScrollSensitivity;
                    mouseInTopBottom = true;
                }
                else if (e.Y > graphics.Height * (1 - CloseToBorder)) {
                    cameraMovement.Y = -CameraScrollSensitivity;
                    mouseInTopBottom = true;
                }
            }
            else if (graphics.Height * CloseToBorder <= e.Y && e.Y <= graphics.Height * (1 - CloseToBorder)) {
                cameraMovement.Y = 0;
                mouseInTopBottom = false;
            }
           
            cameraController.SetHorizontalMovement(cameraMovement);
        }

        private void StartCameraMoveLeft(int qualifiers) {
            var movement = cameraController.StaticMovement;
            movement.X = -CameraScrollSensitivity;
            cameraController.SetMovement(movement);
        }

        private void StopCameraMoveLeft(int qualifiers) {
            var movement = cameraController.StaticMovement;
            if (movement.X == -CameraScrollSensitivity) {
                movement.X = 0;
            }
            cameraController.SetMovement(movement);
        }

        private void StartCameraMoveRight(int qualifiers) {
            var movement = cameraController.StaticMovement;
            movement.X = CameraScrollSensitivity;
            cameraController.SetMovement(movement);
        }

        private void StopCameraMoveRight(int qualifiers) {
            var movement = cameraController.StaticMovement;
            if (movement.X == CameraScrollSensitivity) {
                movement.X = 0;
            }
            cameraController.SetMovement(movement);
        }

        private void StartCameraMoveForward(int qualifiers) {
            var movement = cameraController.StaticMovement;
            movement.Z = CameraScrollSensitivity;
            cameraController.SetMovement(movement);
        }

        private void StopCameraMoveForward(int qualifiers) {
            var movement = cameraController.StaticMovement;
            if (movement.Z == CameraScrollSensitivity) {
                movement.Z = 0;
            }
            cameraController.SetMovement(movement);
        }

        private void StartCameraMoveBackward(int qualifiers) {
            var movement = cameraController.StaticMovement;
            movement.Z = -CameraScrollSensitivity;
            cameraController.SetMovement(movement);
        }

        private void StopCameraMoveBackward(int qualifiers) {
            var movement = cameraController.StaticMovement;
            if (movement.Z == -CameraScrollSensitivity) {
                movement.Z = 0;
            }
            cameraController.SetMovement(movement);
        }

        private void StartCameraRotationRight(int qualifiers) {
            cameraController.SetYaw(-CameraRotationSensitivity);
        }

        private void StopCameraRotationRight(int qualifiers) {
            if (cameraController.StaticYaw == -CameraRotationSensitivity) {
                cameraController.SetYaw(0);
            }
        }

        private void StartCameraRotationLeft(int qualifiers) {
            cameraController.SetYaw(CameraRotationSensitivity);
        }

        private void StopCameraRotationLeft(int qualifiers) {
            if (cameraController.StaticYaw == CameraRotationSensitivity) {
                cameraController.SetYaw(0);
            }
        }

        private void StartCameraRotationUp(int qualifiers) {
            cameraController.SetPitch(-CameraRotationSensitivity);
        }

        private void StopCameraRotationUp(int qualifiers) {
            if (cameraController.StaticPitch == -CameraRotationSensitivity) {
                cameraController.SetPitch(0);
            }
        }

        private void StartCameraRotationDown(int qualifiers) {
            cameraController.SetPitch(CameraRotationSensitivity);
        }

        private void StopCameraRotationDown(int qualifiers) {
            if (cameraController.StaticPitch == CameraRotationSensitivity) {
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
