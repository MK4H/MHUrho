using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;
using Urho.Urho2D;
using MHUrho.UserInterface;
using MHUrho.Packaging;
using MHUrho.Input;
using MHUrho.Control;
using MHUrho.WorldMap;

namespace MHUrho.EditorTools {
    class VertexHeightToolMandK : VertexHeightTool, IMandKTool {
        public IEnumerable<Button> Buttons => buttons;

        private const float Sensitivity = 0.01f;

        private enum Mode {
            None,
            Selecting,
            Moving
        };

        private List<Button> buttons;
        private Mode mode;
        private GameMandKController input;
        private Map Map => input.LevelManager.Map;

        private List<IntVector2> verticies;
        private Vector3 mainPoint;

        private bool enabled;

        public VertexHeightToolMandK(GameMandKController input) {

            //var buttonTexture = new Texture2D();
            //buttonTexture.FilterMode = TextureFilterMode.Nearest;
            //buttonTexture.SetNumLevels(1);
            //buttonTexture.SetSize(tileImage.Width, tileImage.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
            //buttonTexture.SetData(tileType.GetImage());
            this.buttons = new List<Button>();
            this.input = input;
            this.verticies = new List<IntVector2>();


            var selectingButton = new Button();
            selectingButton.SetStyle("VertexHeightToolButton");
            selectingButton.Size = new IntVector2(100, 100);
            selectingButton.HorizontalAlignment = HorizontalAlignment.Center;
            selectingButton.VerticalAlignment = VerticalAlignment.Center;
            selectingButton.Pressed += SelectingButtonPress;
            selectingButton.FocusMode = FocusMode.ResetFocus;
            selectingButton.MaxSize = new IntVector2(100, 100);
            selectingButton.MinSize = new IntVector2(100, 100);
            selectingButton.Texture = PackageManager.Instance.ResourceCache.GetTexture2D("Textures/xamarin.png");
            selectingButton.Visible = false;

            buttons.Add(selectingButton);

            var movingButton = new Button();
            movingButton.SetStyle("VertexHeightToolButton");
            movingButton.Size = new IntVector2(100, 100);
            movingButton.HorizontalAlignment = HorizontalAlignment.Center;
            movingButton.VerticalAlignment = VerticalAlignment.Center;
            movingButton.Pressed += MovingButtonPress;
            movingButton.FocusMode = FocusMode.ResetFocus;
            movingButton.MaxSize = new IntVector2(100, 100);
            movingButton.MinSize = new IntVector2(100, 100);
            movingButton.Texture = PackageManager.Instance.ResourceCache.GetTexture2D("Textures/xamarin.png");
            movingButton.Visible = false;

            buttons.Add(movingButton);

        }

        public void Enable() {
            if (enabled) return;

            input.UIManager.SelectionBarShowButtons(buttons);
            input.RegisterToolAction(9, SwitchToSelectingWithKey);
            enabled = true;
        }

        public void Disable() {
            if (!enabled) return;

            if (mode != Mode.None) {
                input.UIManager.Deselect();
                mode = Mode.None;

                input.MouseDown -= MouseDownMove;
                input.MouseMove -= OnMouseMove;
                input.MouseDown -= MouseDownSelect;
                input.ShowCursor();
            }

            verticies.Clear();
            input.UIManager.SelectionBarClearButtons();
            input.UnregisterToolAction(9);
            input.MouseMove -= OnMouseMove;
            enabled = false;
        }

        public override void Dispose() {
            Disable();
            foreach (var button in buttons) {
                button.Dispose();
            }
        }

        private void OnMouseMove(MouseMovedEventArgs e) {
            if (mode == Mode.Moving) {
                Map.ChangeHeight(verticies, -e.DY * Sensitivity);
            }
        }

        private void MouseDownSelect(MouseButtonDownEventArgs e) {
            var raycastResult = input.CursorRaycast();
            var vertex = Map.RaycastToVertex(raycastResult);
            if (vertex.HasValue) {
                //TODO: this is slow, make it faster
                if (verticies.Contains(vertex.Value)) {
                    verticies.Remove(vertex.Value);
                }
                else {
                    verticies.Add(vertex.Value);
                }
            }
        }

        private void MouseDownMove(MouseButtonDownEventArgs e) {
            SwitchFromMoving();
        }

        private void SelectingButtonPress(PressedEventArgs e) {
            if (mode != Mode.Selecting) {
                SwitchToSelecting();
            }
            else {
                SwitchFromSelecting();
            }
        }

        private void MovingButtonPress(PressedEventArgs e) {
            if (mode != Mode.Moving) {
                SwitchToMoving();
            }
            else {
                SwitchFromMoving();
            }
        }

        private void SwitchToSelectingWithKey(int qualifiers) {
            SwitchToSelecting();
        }

        private void SwitchToMoving() {
            switch (mode) {
                case Mode.None:
                    break;
                case Mode.Selecting:
                    SwitchFromSelecting();
                    break;
                case Mode.Moving:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            input.HideCursor();
            input.MouseDown += MouseDownMove;
            input.MouseMove += OnMouseMove;
            mode = Mode.Moving;
            //TODO: maybe change the index to passing the button itself
            input.UIManager.SelectButton(buttons[1]);
        }

        private void SwitchToSelecting() {
            switch (mode) {
                case Mode.None:
                    break;
                case Mode.Selecting:
                    return;
                case Mode.Moving:
                    SwitchFromMoving();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            input.ShowCursor();
            input.MouseDown += MouseDownSelect;
            mode = Mode.Selecting;
            input.UIManager.SelectButton(buttons[0]);
        }

        private void SwitchFromMoving() {
            input.MouseMove -= OnMouseMove;
            input.MouseDown -= MouseDownMove;
            input.UIManager.Deselect();
            input.ShowCursor();
            mode = Mode.None;
        }

        private void SwitchFromSelecting() {
            input.MouseDown -= MouseDownSelect;
            input.UIManager.Deselect();
            mode = Mode.None;
        }
    }
}
