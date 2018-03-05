using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.Control
{
    public class GameTouchController : TouchController, IGameController
    {
        private enum CameraMovementType { Horizontal, Vertical, FreeFloat }


        public float Sensitivity { get; set; }
        public bool ContinuousMovement { get; private set; } = true;

        public bool DoOnlySingleRaycasts { get; set; }

        //TODO: Move this to separate class probably
        public TileType SelectedTileType => selected == null ? null : tileTypeButtons[selected];

        private readonly CameraController cameraController;
        private readonly Octree octree;
        private readonly LevelManager levelManager;

        private CameraMovementType movementType;

        private readonly Dictionary<int, Vector2> activeTouches = new Dictionary<int, Vector2>();


        private UIElement selectionBar;

        private Dictionary<UIElement, TileType> tileTypeButtons;
        private UIElement selected;

        private bool UIPressed;

        public GameTouchController(MyGame game, LevelManager levelManager, CameraController cameraController, float sensitivity = 0.1f) : base(game) {
            this.cameraController = cameraController;
            this.Sensitivity = sensitivity;
            this.levelManager = levelManager;
            this.octree = levelManager.Scene.GetComponent<Octree>();
            this.DoOnlySingleRaycasts = true;

            //SwitchToContinuousMovement();
            SwitchToDiscontinuousMovement();
            movementType = CameraMovementType.Horizontal;

            Enable();

            DisplayTileTypes();
        }

        public void SwitchToContinuousMovement() {
            ContinuousMovement = true;

            cameraController.SmoothMovement = true;
        }

        public void SwitchToDiscontinuousMovement() {
            ContinuousMovement = false;

            cameraController.SmoothMovement = true;
            
        }

        public void Dispose() {
            Disable();

            selectionBar.Remove();
        }



        protected override void TouchBegin(TouchBeginEventArgs e) {
            if (!UIPressed) {
                activeTouches.Add(e.TouchID, new Vector2(e.X, e.Y));

                var clickedRay = cameraController.Camera.GetScreenRay(e.X / (float)UI.Root.Width,
                                                                      e.Y / (float)UI.Root.Height);

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
            else {
                UIPressed = false;
            }
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

        private void DisplayTileTypes() {

            tileTypeButtons = new Dictionary<UIElement, TileType>();

            selectionBar = UI.Root.CreateWindow();
            selectionBar.SetStyle("windowStyle");
            selectionBar.LayoutMode = LayoutMode.Horizontal;
            selectionBar.LayoutSpacing = 10;
            selectionBar.HorizontalAlignment = HorizontalAlignment.Left;
            selectionBar.Position = new IntVector2(0, UI.Root.Height - 100);
            selectionBar.Height = 100;
            selectionBar.SetFixedWidth(UI.Root.Width);
            selectionBar.SetColor(Color.Yellow);
            selectionBar.FocusMode = FocusMode.NotFocusable;
            selectionBar.ClipChildren = true;
            selectionBar.HoverBegin += SelectionBar_HoverBegin;

            foreach (var tileType in PackageManager.Instance.TileTypes) {
                var tileImage = tileType.GetImage().ConvertToRGBA();

                var buttonTexture = new Texture2D();
                buttonTexture.FilterMode = TextureFilterMode.Nearest;
                buttonTexture.SetNumLevels(1);
                buttonTexture.SetSize(tileImage.Width, tileImage.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
                buttonTexture.SetData(tileType.GetImage());



                var button = selectionBar.CreateButton();
                button.SetStyle("TextureButton");
                button.Size = new IntVector2(100, 100);
                button.HorizontalAlignment = HorizontalAlignment.Center;
                button.VerticalAlignment = VerticalAlignment.Center;
                button.Pressed += Button_Pressed;
                button.Pressed += UI_Pressed;
                button.Texture = buttonTexture;
                button.FocusMode = FocusMode.ResetFocus;
                button.MaxSize = new IntVector2(100, 100);
                button.MinSize = new IntVector2(100, 100);

                tileTypeButtons.Add(button, tileType);
            }
        }

        private void SelectionBar_HoverBegin(HoverBeginEventArgs obj) {
            UIPressed = true;
        }

        private void Button_Pressed(PressedEventArgs e) {
            selected?.SetColor(Color.White);
            if (selected != e.Element) {
                selected = e.Element;
                e.Element.SetColor(Color.Gray);
            }
            else {
                //DESELECT
                selected = null;
            }
        }

        private void UI_Pressed(PressedEventArgs e) {
            UIPressed = true;
        }
    }
}
