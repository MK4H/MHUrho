using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Control;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
    public class MandKUI : IDisposable {
        private static Color selectedColor = Color.Gray;
        private static Color mouseOverColor = new Color(0.9f, 0.9f, 0.9f);

        private static Texture2D DefaultButtonTexture =
            PackageManager.Instance.ResourceCache.GetTexture2D("Textures/xamarin.png");


        private readonly MyGame game;
        private readonly GameMandKController inputCtl;

        private UI UI => game.UI;

        private IPlayer player => inputCtl.Player;

        private Urho.Input Input => game.Input;


        private readonly UIElement toolSelection;
        private readonly  UIElement selectionBar;

        private Dictionary<UIElement, IMandKTool> tools;

        private UIElement selectionBarSelected;
        private UIElement toolSelected;

        private int hovering = 0;

        public MandKUI(MyGame game, GameMandKController inputCtl) {
            this.game = game;
            this.inputCtl = inputCtl;
            this.tools = new Dictionary<UIElement, IMandKTool>();

            selectionBar = UI.Root.CreateWindow();
            selectionBar.SetStyle("windowStyle");
            selectionBar.LayoutMode = LayoutMode.Horizontal;
            selectionBar.LayoutSpacing = 10;
            selectionBar.HorizontalAlignment = HorizontalAlignment.Left;
            selectionBar.Position = new IntVector2(50, UI.Root.Height - 100);
            selectionBar.Height = 100;
            selectionBar.SetFixedWidth(UI.Root.Width - 50);
            selectionBar.SetColor(Color.Yellow);
            selectionBar.FocusMode = FocusMode.NotFocusable;
            selectionBar.ClipChildren = true;
            selectionBar.HoverBegin += UIHoverBegin;
            selectionBar.HoverEnd += UIHoverEnd;


            toolSelection = UI.Root.CreateWindow();
            toolSelection.LayoutMode = LayoutMode.Vertical;
            toolSelection.LayoutSpacing = 0;
            toolSelection.HorizontalAlignment = HorizontalAlignment.Left;
            toolSelection.VerticalAlignment = VerticalAlignment.Bottom;
            toolSelection.Position = new IntVector2(0, 0);
            toolSelection.Height = UI.Root.Height;
            toolSelection.SetFixedWidth(50);
            toolSelection.SetColor(Color.Blue);
            toolSelection.FocusMode = FocusMode.NotFocusable;
            toolSelection.ClipChildren = true;
            toolSelection.HoverBegin += UIHoverBegin;
            toolSelection.HoverEnd += UIHoverEnd;
        }

        public void Dispose() {
            ClearDelegates();
            selectionBar.RemoveAllChildren();
            selectionBar.Remove();
            selectionBar.Dispose();
            toolSelection.RemoveAllChildren();
            toolSelection.Remove();
            toolSelection.Dispose();

            Debug.Assert(selectionBar.IsDeleted, "Selection bar did not delete itself");
        }

        public void EnableUI() {
            selectionBar.Enabled = true;
            toolSelection.Enabled = true;
        }

        public void DisableUI() {
            selectionBar.Enabled = false;
            toolSelection.Enabled = false;
        }

        public void HideUI() {
            selectionBar.Visible = false;
            toolSelection.Visible = false;
        }

        public void ShowUI() {
            selectionBar.Visible = true;
            toolSelection.Visible = true;
        }

        public void SelectionBarShowButtons(IEnumerable<Button> buttons) {

            foreach (var button in buttons) {
                button.HoverBegin += Button_HoverBegin;
                button.HoverBegin += UIHoverBegin;
                button.HoverEnd += Button_HoverEnd;
                button.HoverEnd += UIHoverEnd;
                button.Visible = true;
            }
        }

        /// <summary>
        /// Deactivates buttons and hides them
        /// </summary>
        public void SelectionBarClearButtons() {
            selectionBarSelected = null;

            foreach (Button button in selectionBar.Children) {
                button.HoverBegin -= Button_HoverBegin;
                button.HoverBegin -= UIHoverBegin;
                button.HoverEnd -= Button_HoverEnd;
                button.HoverEnd -= UIHoverEnd;
                button.Visible = false;
            }
        }

        public void Deselect() {
            selectionBarSelected.SetColor(Color.White);
            selectionBarSelected = null;
        }

        public void SelectButton(Button button) {
            selectionBarSelected?.SetColor(Color.White);
            selectionBarSelected = button;
            selectionBarSelected.SetColor(Color.Gray);
        }

        public void AddTool(IMandKTool tool) {
            var button = toolSelection.CreateButton();
            button.SetStyle("toolButton");
            button.Size = new IntVector2(50, 50);
            button.HorizontalAlignment = HorizontalAlignment.Center;
            button.VerticalAlignment = VerticalAlignment.Center;
            button.Pressed += ToolSwitchbuttonPress;
            button.HoverBegin += UIHoverBegin;
            button.HoverEnd += UIHoverEnd;
            button.FocusMode = FocusMode.ResetFocus;
            button.MaxSize = new IntVector2(50, 50);
            button.MinSize = new IntVector2(50, 50);
            button.Texture = tool.Icon ?? DefaultButtonTexture;

            tools.Add(button, tool);

            foreach (var toolButton in tool.Buttons) {
                selectionBar.AddChild(toolButton);
            }

        }

        public void RemoveTool(IMandKTool tool) {
            throw new NotImplementedException();
        }

        private void Button_HoverBegin(HoverBeginEventArgs e) {
            if (e.Element != selectionBarSelected) {
                e.Element.SetColor(new Color(0.9f, 0.9f, 0.9f));
            }
        }

        private void Button_HoverEnd(HoverEndEventArgs e) {
            if (e.Element != selectionBarSelected) {
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

        private void ClearDelegates() {
            selectionBar.HoverBegin -= UIHoverBegin;
            selectionBar.HoverEnd -= UIHoverEnd;
            toolSelection.HoverBegin -= UIHoverBegin;
            toolSelection.HoverEnd -= UIHoverEnd;

            foreach (var button in selectionBar.Children) {
                button.HoverBegin -= Button_HoverBegin;
                button.HoverBegin -= UIHoverBegin;
                button.HoverEnd -= Button_HoverEnd;
                button.HoverEnd -= UIHoverEnd;
            }

            foreach (var button in toolSelection.Children) {
                button.HoverBegin -= Button_HoverBegin;
                button.HoverBegin -= UIHoverBegin;
                button.HoverEnd -= Button_HoverEnd;
                button.HoverEnd -= UIHoverEnd;
            }
        }

        private void ToolSwitchbuttonPress(PressedEventArgs e) {
            if (toolSelected != null) {
                tools[toolSelected].Disable();
            }

            toolSelected = e.Element;
            tools[toolSelected].Enable();
        }

    }
}
