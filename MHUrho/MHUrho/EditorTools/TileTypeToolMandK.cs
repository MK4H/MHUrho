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

        private Button selected;

        public TileTypeToolMandK(GameMandKController input, Map map) {

            this.input = input;
            this.map = map;
            this.tileTypeButtons = new Dictionary<Button, TileType>();

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
            input.UIManager.SelectionBarShowButtons(tileTypeButtons.Keys);
        }

        public void Disable() {
            input.UIManager.SelectionBarClearButtons();
            input.MouseDown -= OnMouseDown;
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
            }
            else {
                input.UIManager.SelectButton((Button)e.Element);
                selected = (Button)e.Element;
                input.MouseDown += OnMouseDown;
            }
        }

        private void OnMouseDown(MouseButtonDownEventArgs e) {
            if (selected != null) {
                var raycast = input.CursorRaycast();
                var tile = map.RaycastToTile(raycast);
                //TODO: Rectangle
                if (tile != null) {
                    map.ChangeTileType(tile, tileTypeButtons[selected], new IntVector2(3, 3));
                }
            }
        }

        
    }
}
