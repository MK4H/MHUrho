using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
    class TileTypeToolMandK : TileTypeTool, IMandKTool {
        private Dictionary<Button, TileType> tileTypeButtons;

        private GameMandKController input;
        private Map map;
        private StaticRectangleToolMandK highlight;

        private Button selected;
        private ITile centerTile;

        private bool mouseButtonDown;
        private bool enabled;

        public TileTypeToolMandK(GameMandKController input, Map map) {

            this.input = input;
            this.map = map;
            this.tileTypeButtons = new Dictionary<Button, TileType>();
            this.highlight = new StaticRectangleToolMandK(input, map, new IntVector2(3,3));

            foreach (var tileType in PackageManager.Instance.TileTypes) {
                var tileImage = tileType.GetImage().ConvertToRGBA();

                var buttonTexture = new Texture2D();
                buttonTexture.FilterMode = TextureFilterMode.Nearest;
                buttonTexture.SetNumLevels(1);
                buttonTexture.SetSize(tileImage.Width, tileImage.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
                buttonTexture.SetData(tileType.GetImage());



                var button = new Button();
                button.SetStyle("TextureButton");
                button.Size = new IntVector2(100, 100);
                button.HorizontalAlignment = HorizontalAlignment.Center;
                button.VerticalAlignment = VerticalAlignment.Center;
                button.Pressed += Button_Pressed;
                button.Texture = buttonTexture;
                button.FocusMode = FocusMode.ResetFocus;
                button.MaxSize = new IntVector2(100, 100);
                button.MinSize = new IntVector2(100, 100);

                tileTypeButtons.Add(button, tileType);
            }
        }

        public void Enable() {
            if (enabled) return;


            highlight.Enable();
            input.UIManager.SelectionBarShowButtons(tileTypeButtons.Keys);
            enabled = true;
        }

        public void Disable() {
            if (!enabled) return;


            highlight.Disable();
            input.UIManager.SelectionBarClearButtons();
            input.MouseDown -= OnMouseDown;
            input.MouseUp -= OnMouseUp;
            input.MouseMove -= OnMouseMove;
            enabled = false;
            
        }

        public override void Dispose() {
            //TODO: Maybe dont disable, or change implementation of disable to not delete currently visible buttons
            Disable();
            foreach (var pair in tileTypeButtons) {
                pair.Key.Pressed -= Button_Pressed;
                pair.Key.Dispose();
            }
            tileTypeButtons = null;
        }

        private void Button_Pressed(PressedEventArgs e) {
            if (selected == e.Element) {
                input.UIManager.Deselect();
                selected = null;
                input.MouseDown -= OnMouseDown;
                input.MouseUp -= OnMouseUp;
                input.MouseMove -= OnMouseMove;
            }
            else {
                input.UIManager.SelectButton((Button)e.Element);
                selected = (Button)e.Element;
                input.MouseDown += OnMouseDown;
                input.MouseUp += OnMouseUp;
                input.MouseMove += OnMouseMove;
            }
        }

        private void OnMouseDown(MouseButtonDownEventArgs e) {
            if (selected != null) {
                centerTile = input.GetTileUnderCursor();
                //TODO: Rectangle
                if (centerTile != null) {
                    map.ChangeTileType(centerTile, tileTypeButtons[selected], highlight.Size);
                }
                mouseButtonDown = true;
            }
        }

        private void OnMouseUp(MouseButtonUpEventArgs e) {
            if (selected != null) {
                mouseButtonDown = false;
            }
        }

        private void OnMouseMove(MouseMovedEventArgs e) {
            if (selected != null && mouseButtonDown) {
                var newCenterTile = input.GetTileUnderCursor();
                if (newCenterTile != null && newCenterTile != centerTile) {
                    centerTile = newCenterTile;
                    map.ChangeTileType(centerTile, tileTypeButtons[selected], new IntVector2(3, 3));
                }
            }
        }

        
    }
}
