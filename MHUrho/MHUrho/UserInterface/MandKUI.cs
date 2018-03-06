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
    class MandKUI : IDisposable
    {
        private readonly MyGame game;
        private readonly GameMandKController inputCtl;

        private UI UI => game.UI;

        private IPlayer player => inputCtl.Player;

        private Urho.Input Input => game.Input;

        private UIElement selectionBar;

        private Dictionary<UIElement, TileType> tileTypeButtons;
        private UIElement selected;

        private int hovering = 0;

        public MandKUI(MyGame game, GameMandKController inputCtl) {
            this.game = game;
            this.inputCtl = inputCtl;
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
            selectionBar.HoverBegin += UIHoverBegin;
            selectionBar.HoverEnd += UIHoverEnd;

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
                player.UISelect(tileTypeButtons[selected]);
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
            inputCtl.UIHovering = true;
        }

        private void UIHoverEnd(HoverEndEventArgs e) {
            if (--hovering == 0) {
                inputCtl.UIHovering = false;
            }
        }
    }
}
