using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.IO;

using MHUrho.Helpers;
using Urho.Gui;
using Urho.Resources;

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
        private readonly UI ui;


        private CameraMovementType cameraType;

        private List<KeyAction> actions;
        private Dictionary<Key, Actions> keyActions;

        //private float KeyRotationSensitivity => KeyCameraSensitivity * 3;

        private const float CloseToBorder = 1/20f;

        private bool mouseInLeftRight;
        private bool mouseInTopBottom;

        public MouseController( CameraController cameraController, 
                                Input input, 
                                UI ui, 
                                Context context, 
                                ResourceCache resourceCache) {
            this.cameraController = cameraController;
            this.input = input;
            this.MouseSensitivity = 0.2f;
            this.CameraScrollSensitivity = 5f;
            this.CameraRotationSensitivity = 15f;
            this.cameraType = CameraMovementType.Fixed;
            this.ui = ui;

            cameraController.Drag = 10;

            var style = resourceCache.GetXmlFile("UI/DefaultStyle.xml");
            ui.Root.SetDefaultStyle(style);

            var cursor = ui.Root.CreateCursor("UICursor");
            ui.Cursor = cursor;
            ui.Cursor.SetStyleAuto(style);
            ui.Cursor.Position = new IntVector2(ui.Root.Width / 2, ui.Root.Height / 2);

            var cursorImage = resourceCache.GetImage("Textures/xamarin.png");
            ui.Cursor.DefineShape("MyShape",
                cursorImage,
                new IntRect(0, 0, cursorImage.Width - 1, cursorImage.Height - 1),
                new IntVector2(cursorImage.Width / 2, cursorImage.Height / 2));
            //TODO: Shape keeps reseting to NORMAL, even though i set it here
            ui.Cursor.Shape = "MyShape";
            ui.Cursor.UseSystemShapes = false;
            ui.Cursor.Visible = true;

            //TODO: TEMPORARY, probably move to UIManager or something
            var button = ui.Root.CreateButton("StartButton");
            button.SetStyleAuto(style);
            button.Size = new IntVector2(50, 50);
            button.Position = new IntVector2(100, 100);
            button.Pressed += Button_Pressed;
            button.HoverBegin += Button_HoverBegin;
            button.HoverEnd += Button_HoverEnd;
            button.SetColor(Color.Yellow);

            button = ui.Root.CreateButton("SaveButton");
            button.SetStyleAuto(style);
            button.Size = new IntVector2(50, 50);
            button.Position = new IntVector2(100, 100);
            button.Pressed += Button_Pressed;
            button.HoverBegin += Button_HoverBegin;
            button.HoverEnd += Button_HoverEnd;
            button.SetColor(Color.Green);

            button = ui.Root.CreateButton("LoadButton");
            button.SetStyleAuto(style);
            button.Size = new IntVector2(50, 50);
            button.Position = new IntVector2(200, 100);
            button.Pressed += Button_Pressed;
            button.HoverBegin += Button_HoverBegin;
            button.HoverEnd += Button_HoverEnd;
            button.SetColor(Color.Blue);

            button = ui.Root.CreateButton("EndButton");
            button.SetStyleAuto(style);
            button.Size = new IntVector2(50, 50);
            button.Position = new IntVector2(250, 100);
            button.Pressed += Button_Pressed;
            button.HoverBegin += Button_HoverBegin;
            button.HoverEnd += Button_HoverEnd;
            button.SetColor(Color.Red);

            input.SetMouseMode(MouseMode.Absolute);
            input.SetMouseVisible(false);

            

            FillActionList();

            //TODO: Load from config
            SetKeyBindings();

            RegisterCallbacks();

        }

        //TODO: TEMPORARY, probably move to UIManager or something
        private void Button_HoverEnd(HoverEndEventArgs obj) {
            Log.Write(LogLevel.Debug, "Hover end");
        }

        //TODO: TEMPORARY, probably move to UIManager or something
        private void Button_HoverBegin(HoverBeginEventArgs obj) {
            Log.Write(LogLevel.Debug, "Hover begin");
        }

        //TODO: TEMPORARY, probably move to UIManager or something
        private void Button_Pressed(PressedEventArgs obj) {
            Log.Write(LogLevel.Debug, "Button pressed");

            switch (obj.Element.Name) {
                case "StartButton":
                    break;
                case "SaveButton":
                    break;
                case "LoadButton":
                    break;
                case "EndButton":
                    break;
            }
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
            Log.Write(LogLevel.Debug, $"Mouse button down at: X={ui.Cursor.Position.X}, Y={ui.Cursor.Position.Y}");
        }

        private void MouseButtonUp(MouseButtonUpEventArgs e) {
            Log.Write(LogLevel.Debug, $"Mouse button up at: X={ui.Cursor.Position.X}, Y={ui.Cursor.Position.Y}");
        }

        private void MouseMoved(MouseMovedEventArgs e) {
            if (cameraType == CameraMovementType.FreeFloat) {
                cameraController.AddRotation(new Vector2(e.DY, -e.DX) * MouseSensitivity);
            }
            else if (cameraType == CameraMovementType.Fixed) {
                MouseBorderMovement(ui.Cursor.Position);
            }
        }

        private void MouseWheel(MouseWheelEventArgs e) {

        }

        private void MouseBorderMovement(IntVector2 mousePos) {

            Vector2 cameraMovement = new Vector2(cameraController.StaticMovement.X, cameraController.StaticMovement.Z);

            if (!mouseInLeftRight) {
                //Mouse was not in the border area before, check if it is now 
                // and if it is, set the movement
                if (mousePos.X < ui.Root.Width * CloseToBorder) {
                    cameraMovement.X = -CameraScrollSensitivity;
                    mouseInLeftRight = true;
                }
                else if (mousePos.X > ui.Root.Width * (1 - CloseToBorder)) {
                    cameraMovement.X = CameraScrollSensitivity;
                    mouseInLeftRight = true;
                }

            }
            else if (ui.Root.Width * CloseToBorder <= mousePos.X && mousePos.X <= ui.Root.Width * (1 - CloseToBorder)) {
                //Mouse was in the area, and now it is not, reset the movement
                cameraMovement.X = 0;
                mouseInLeftRight = false;
            }

            if (!mouseInTopBottom) {
                if (mousePos.Y < ui.Root.Height * CloseToBorder) {
                    cameraMovement.Y = CameraScrollSensitivity;
                    mouseInTopBottom = true;
                }
                else if (mousePos.Y > ui.Root.Height * (1 - CloseToBorder)) {
                    cameraMovement.Y = -CameraScrollSensitivity;
                    mouseInTopBottom = true;
                }
            }
            else if (ui.Root.Height * CloseToBorder <= mousePos.Y && mousePos.Y <= ui.Root.Height * (1 - CloseToBorder)) {
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
                ui.Cursor.Visible = true;
                
            }
            else {
                cameraController.SwitchToFree();
                cameraType = CameraMovementType.FreeFloat;
                ui.Cursor.Visible = false;
            }
        }
    }
}
