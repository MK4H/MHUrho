using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Urho;
using Urho.IO;

using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho.Gui;
using Urho.Urho2D;
using Urho.Resources;

namespace MHUrho.Control
{
    public class GameMandKController : MandKController, IGameController
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

        //TODO: Move the selection panel to its own class probably
        public TileType SelectedTileType => selected == null ? null : tileTypeButtons[selected]; 
        

        public bool DoOnlySingleRaycasts { get; set; }

        public float CameraScrollSensitivity { get; set; }

        public float CameraRotationSensitivity { get; set; }

        public bool MouseBorderCameraMovement { get; set; }

        private readonly LevelManager levelManager;
        private readonly CameraController cameraController;
        private readonly Octree octree;

        private List<KeyAction> actions;
        private Dictionary<Key, Actions> keyActions;

        private CameraMovementType cameraType;


        //private float KeyRotationSensitivity => KeyCameraSensitivity * 3;

        private const float CloseToBorder = 1/20f;

        private bool mouseInLeftRight;
        private bool mouseInTopBottom;

        private UIElement selectionBar;

        private Dictionary<UIElement, TileType> tileTypeButtons;
        private UIElement selected;

        private int hovering = 0;


        public GameMandKController(MyGame game, LevelManager levelManager, CameraController cameraController) : base(game) {
            this.CameraScrollSensitivity = 5f;
            this.CameraRotationSensitivity = 15f;
            this.cameraType = CameraMovementType.Fixed;
            this.cameraController = cameraController;
            this.octree = levelManager.Scene.GetComponent<Octree>();
            this.levelManager = levelManager;
            this.DoOnlySingleRaycasts = true;

            FillActionList();

            //TODO: Load from config
            SetKeyBindings();

            DisplayTileTypes();

            Enable();
        }

        public void Dispose() {
            selectionBar.Remove();
            Disable();
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

        private KeyAction GetAction(Actions action) {
            return actions[(int)action];
        }

        protected override void KeyDown(KeyDownEventArgs e) {
            if (keyActions.TryGetValue(e.Key, out Actions action)) {
                if (!e.Repeat) {
                    GetAction(action).KeyDown?.Invoke(e.Qualifiers);
                }
                else {
                    GetAction(action).Repeat?.Invoke(e.Qualifiers);
                }
            }
        }

        protected override void KeyUp(KeyUpEventArgs e) {
            if (keyActions.TryGetValue(e.Key, out Actions action)) {
                GetAction(action).KeyUp?.Invoke(e.Qualifiers);
            }
        }

        protected override void MouseButtonDown(MouseButtonDownEventArgs e) {
            if (hovering == 0) {
                Log.Write(LogLevel.Debug, $"Mouse button down at: X={UI.Cursor.Position.X}, Y={UI.Cursor.Position.Y}");

                var clickedRay = cameraController.Camera.GetScreenRay(UI.Cursor.Position.X / (float)UI.Root.Width,
                                                                      UI.Cursor.Position.Y / (float)UI.Root.Height);

                if (DoOnlySingleRaycasts) {
                    var raycastResult = octree.RaycastSingle(clickedRay);
                    if (raycastResult.HasValue) {
                        levelManager.HandleRaycast(raycastResult.Value);
                    }
                }
                else {
                    var raycastResults = octree.Raycast(clickedRay);
                    if (raycastResults.Count != 0) {
                        levelManager.HandleRaycast(raycastResults);
                    }
                }
            }

            
            
            
        }

        protected override void MouseButtonUp(MouseButtonUpEventArgs e) {
            if (hovering == 0) {
                Log.Write(LogLevel.Debug, $"Mouse button up at: X={UI.Cursor.Position.X}, Y={UI.Cursor.Position.Y}");
            }
            
        }

        protected override void MouseMoved(MouseMovedEventArgs e) {
            if (cameraType == CameraMovementType.FreeFloat) {
                cameraController.AddRotation(new Vector2(e.DY, -e.DX) * MouseSensitivity);
            }
            else if (cameraType == CameraMovementType.Fixed) {
                MouseBorderMovement(UI.Cursor.Position);
            }

        }

        protected override void MouseWheel(MouseWheelEventArgs e) {

        }

        private void MouseBorderMovement(IntVector2 mousePos) {

            Vector2 cameraMovement = new Vector2(cameraController.StaticMovement.X, cameraController.StaticMovement.Z);

            if (!mouseInLeftRight) {
                //Mouse was not in the border area before, check if it is now 
                // and if it is, set the movement
                if (mousePos.X < UI.Root.Width * CloseToBorder) {
                    cameraMovement.X = -CameraScrollSensitivity;
                    mouseInLeftRight = true;
                }
                else if (mousePos.X > UI.Root.Width * (1 - CloseToBorder)) {
                    cameraMovement.X = CameraScrollSensitivity;
                    mouseInLeftRight = true;
                }

            }
            else if (UI.Root.Width * CloseToBorder <= mousePos.X && mousePos.X <= UI.Root.Width * (1 - CloseToBorder)) {
                //Mouse was in the area, and now it is not, reset the movement
                cameraMovement.X = 0;
                mouseInLeftRight = false;
            }

            if (!mouseInTopBottom) {
                if (mousePos.Y < UI.Root.Height * CloseToBorder) {
                    cameraMovement.Y = CameraScrollSensitivity;
                    mouseInTopBottom = true;
                }
                else if (mousePos.Y > UI.Root.Height * (1 - CloseToBorder)) {
                    cameraMovement.Y = -CameraScrollSensitivity;
                    mouseInTopBottom = true;
                }
            }
            else if (UI.Root.Height * CloseToBorder <= mousePos.Y && mousePos.Y <= UI.Root.Height * (1 - CloseToBorder)) {
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
                UI.Cursor.Visible = true;
                
            }
            else {
                cameraController.SwitchToFree();
                cameraType = CameraMovementType.FreeFloat;
                UI.Cursor.Visible = false;
            }
        }



        private void DisplayTileTypes() {

            tileTypeButtons = new Dictionary<UIElement, TileType>();

            selectionBar = UI.Root.CreateWindow();
            selectionBar.SetStyle("windowStyle");
            selectionBar.LayoutMode = LayoutMode.Horizontal;
            selectionBar.LayoutSpacing = 10;
            selectionBar.HorizontalAlignment = HorizontalAlignment.Left;
            selectionBar.Position = new IntVector2(0,UI.Root.Height - 100);
            selectionBar.Height = 100;
            selectionBar.SetFixedWidth( UI.Root.Width );
            selectionBar.SetColor(Color.Yellow);
            selectionBar.FocusMode = FocusMode.NotFocusable;
            selectionBar.ClipChildren = true;
            selectionBar.HoverBegin += UIHoverBegin;
            selectionBar.HoverEnd += UIHoverEnd;

            foreach (var tileType in PackageManager.Instance.TileTypes) {
                var tileImage = tileType.GetImage().ConvertToRGBA();

                var buttonTexture = new Texture2D();
                buttonTexture.FilterMode = TextureFilterMode.Nearest;
                buttonTexture.SetNumLevels(1);
                buttonTexture.SetSize(tileImage.Width, tileImage.Height, Urho.Graphics.RGBAFormat , TextureUsage.Static);
                buttonTexture.SetData(tileType.GetImage());



                var button = selectionBar.CreateButton();
                button.SetStyle("TextureButton");
                button.Size = new IntVector2(100, 100);
                button.HorizontalAlignment = HorizontalAlignment.Center;
                button.VerticalAlignment = VerticalAlignment.Center;
                button.Pressed += Button_Pressed;
                button.HoverBegin += Button_HoverBegin;
                button.HoverBegin += UIHoverBegin;
                button.HoverEnd += Button_HoverEnd;
                button.HoverEnd += UIHoverEnd;
                button.Texture = buttonTexture;
                button.FocusMode = FocusMode.ResetFocus;
                button.MaxSize = new IntVector2(100, 100);
                button.MinSize = new IntVector2(100, 100);

                tileTypeButtons.Add(button, tileType);
            }
        }

        private void Button_Pressed(PressedEventArgs e) {
            selected?.SetColor(Color.White);
            if (selected != e.Element) {
                selected = e.Element;
                e.Element.SetColor(Color.Gray);
            }
            else {
                selected = null;
            }
        }

        private void Button_HoverBegin(HoverBeginEventArgs e) {
            if (e.Element != selected) {
                e.Element.SetColor(new Color(0.9f, 0.9f, 0.9f));
            }  
        }

        private void Button_HoverEnd(HoverEndEventArgs e) {
            if (e.Element != selected) {
                e.Element.SetColor(Color.White);
            }
        }

        private void UIHoverBegin(HoverBeginEventArgs e) {
            hovering++;
        }

        private void UIHoverEnd(HoverEndEventArgs e) {
            hovering--;
        }
    }
}
