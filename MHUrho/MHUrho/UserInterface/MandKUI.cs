using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class MandKUI : IDisposable
    {
        private readonly MyGame game;
        private readonly GameMandKController inputCtl;

        private UI UI => game.UI;

        private IPlayer player => inputCtl.Player;

        private Urho.Input Input => game.Input;

        private readonly  UIElement selectionBar;
        private UIElement selected;

        private int hovering = 0;

        public MandKUI(MyGame game, GameMandKController inputCtl) {
            this.game = game;
            this.inputCtl = inputCtl;

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
        }

        public void Dispose() {
            DisableUI();
            selectionBar.RemoveAllChildren();
            selectionBar.Remove();
            selectionBar.Dispose();
            Debug.Assert(selectionBar.IsDeleted, "Selection bar did not delete itself");
        }


        public void EnableUI() {
            selectionBar.HoverBegin += UIHoverBegin;
            selectionBar.HoverEnd += UIHoverEnd;

            foreach (Button button in selectionBar.Children) {
                button.HoverBegin += Button_HoverBegin;
                button.HoverBegin += UIHoverBegin;
                button.HoverEnd += Button_HoverEnd;
                button.HoverEnd += UIHoverEnd;
                button.Pressed += Button_Pressed;
            }
        }

        public void DisableUI() {
            selectionBar.HoverBegin -= UIHoverBegin;
            selectionBar.HoverEnd -= UIHoverEnd;

            foreach (Button button in selectionBar.Children) {
                button.HoverBegin -= Button_HoverBegin;
                button.HoverBegin -= UIHoverBegin;
                button.HoverEnd -= Button_HoverEnd;
                button.HoverEnd -= UIHoverEnd;
                button.Pressed -= Button_Pressed;
            }
        }

        public void HideUI() {
            selectionBar.Visible = false;
        }

        public void ShowUI() {
            selectionBar.Visible = true;
        }

        public void SelectionBarShowButtons(List<Button> buttons) {
            //Clear selection bar
            selectionBar.RemoveAllChildren();

            foreach (var button in buttons) {
                selectionBar.AddChild(button);
                button.Pressed += Button_Pressed;
                button.HoverBegin += Button_HoverBegin;
                button.HoverBegin += UIHoverBegin;
                button.HoverEnd += Button_HoverEnd;
                button.HoverEnd += UIHoverEnd;
            }
        }

        public void SelectionBarClearButtons() {
            selected = null;

            foreach (Button button in selectionBar.Children) {
                button.Pressed -= Button_Pressed;
                button.HoverBegin -= Button_HoverBegin;
                button.HoverBegin -= UIHoverBegin;
                button.HoverEnd -= Button_HoverEnd;
                button.HoverEnd -= UIHoverEnd;
            }

            selectionBar.RemoveAllChildren();
        }

        private void Button_Pressed(PressedEventArgs e) {
            selected?.SetColor(Color.White);
            e.Element.SetColor(new Color(0.9f, 0.9f, 0.9f));
            if (selected != e.Element) {
                selected = e.Element;
                e.Element.SetColor(Color.Gray);
                //player.UISelect(tileTypeButtons[selected]);
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

            Urho.IO.Log.Write(LogLevel.Debug, $"UIHovering :{hovering}");
        }

        private void UIHoverEnd(HoverEndEventArgs e) {
            if (--hovering == 0) {
                inputCtl.UIHovering = false;
            }

            Urho.IO.Log.Write(LogLevel.Debug, $"UIHovering :{hovering}");
        }

    }
}
