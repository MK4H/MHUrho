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

namespace MHUrho.EditorTools {
    class VertexHeightToolMandK : VertexHeightTool, IMandKTool
    {
        private enum Mode { None, Selecting, Moving };

        private List<Button> buttons;
        private Mode mode;
        private GameMandKController input;


        public VertexHeightToolMandK(GameMandKController input) {

            //var buttonTexture = new Texture2D();
            //buttonTexture.FilterMode = TextureFilterMode.Nearest;
            //buttonTexture.SetNumLevels(1);
            //buttonTexture.SetSize(tileImage.Width, tileImage.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
            //buttonTexture.SetData(tileType.GetImage());



            var selectingButton = new Button();
            selectingButton.SetStyle("VertexHeightToolButton");
            selectingButton.Size = new IntVector2(100, 100);
            selectingButton.HorizontalAlignment = HorizontalAlignment.Center;
            selectingButton.VerticalAlignment = VerticalAlignment.Center;
            selectingButton.Pressed += SwitchToSelecting;
            selectingButton.FocusMode = FocusMode.ResetFocus;
            selectingButton.MaxSize = new IntVector2(100, 100);
            selectingButton.MinSize = new IntVector2(100, 100);
            selectingButton.Texture = PackageManager.Instance.ResourceCache.GetTexture2D("Textures/xamaring.png");

            buttons.Add(selectingButton);

            var movingButton = new Button();
            movingButton.SetStyle("VertexHeightToolButton");
            movingButton.Size = new IntVector2(100, 100);
            movingButton.HorizontalAlignment = HorizontalAlignment.Center;
            movingButton.VerticalAlignment = VerticalAlignment.Center;
            movingButton.Pressed += SwitchToMoving;
            movingButton.FocusMode = FocusMode.ResetFocus;
            movingButton.MaxSize = new IntVector2(100, 100);
            movingButton.MinSize = new IntVector2(100, 100);
            movingButton.Texture = PackageManager.Instance.ResourceCache.GetTexture2D("Textures/xamaring.png");

            buttons.Add(movingButton);
        }

        public void Enable() {
            input.UIManager.SelectionBarShowButtons(buttons);
        }

        public void Disable() {
            input.UIManager.SelectionBarClearButtons();
        }

        private void SwitchToSelecting(PressedEventArgs e) {
            mode = mode == Mode.Selecting ? Mode.None : Mode.Selecting;
        }

        private void SwitchToMoving(PressedEventArgs e) {
            mode = mode == Mode.Moving ? Mode.None : Mode.Moving;
        }
    }
}
