using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
    class TouchUI : IDisposable {

        private readonly MyGame game;
        private readonly GameTouchController touchInputCtl;

        private Urho.Gui.UI UI => game.UI;

        private IPlayer player => touchInputCtl.Player;

        private Urho.Input Input => game.Input;

        private UIElement selectionBar;

        private Dictionary<UIElement, TileType> tileTypeButtons;
        private UIElement selected;


        public TouchUI(MyGame game, GameTouchController gameTouchController) {
            this.game = game;
            this.touchInputCtl = gameTouchController;

            DisplayTileTypes();
        }

        public void Dispose() {
            selectionBar.Remove();
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
            touchInputCtl.UIPressed = true;
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
            touchInputCtl.UIPressed = true;
        }
    }
}
