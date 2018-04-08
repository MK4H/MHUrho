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
    class BuildingBuilderToolMandK : BuildingBuilderTool, IMandKTool
    {
        public IEnumerable<Button> Buttons => throw new NotImplementedException();

        private Dictionary<Button, BuildingType> buildingTypeButtons;

        private GameMandKController input;
        private Map map;

        private Button selected;

        private bool enabled;

        public BuildingBuilderToolMandK(GameMandKController input, Map map) {
            this.input = input;
            this.map = map;
            this.buildingTypeButtons = new Dictionary<Button, BuildingType>();

            foreach (var buildingType in PackageManager.Instance.BuildingTypes) {
                var buildingIcon = buildingType.Icon;

                var buttonTexture = new Texture2D();
                buttonTexture.FilterMode = TextureFilterMode.Nearest;
                buttonTexture.SetNumLevels(1);
                buttonTexture.SetSize(buildingIcon.Width, buildingIcon.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
                buttonTexture.SetData(buildingIcon);

                var button = new Button();
                button.SetStyle("BuildingButton");
                button.Size = new IntVector2(100, 100);
                button.HorizontalAlignment = HorizontalAlignment.Center;
                button.VerticalAlignment = VerticalAlignment.Center;
                button.Pressed += Button_Pressed;
                button.Texture = buttonTexture;
                button.FocusMode = FocusMode.ResetFocus;
                button.MaxSize = new IntVector2(100, 100);
                button.MinSize = new IntVector2(100, 100);
                button.Visible = false;
            }
        }

        public void Enable() {
            if (enabled) return;

            input.UIManager.SelectionBarShowButtons(buildingTypeButtons.Keys);
            enabled = true;
        }

        public void Disable() {
            if (!enabled) return;

            if (selected != null) {
                input.UIManager.Deselect();
                selected = null;
            }

            input.UIManager.SelectionBarClearButtons();
            input.MouseDown -= OnMouseDown;
            enabled = false;
        }

        public override void Dispose() {
            Disable();
            foreach (var pair in buildingTypeButtons) {
                pair.Key.Pressed -= Button_Pressed;
                pair.Key.Dispose();
            }
            buildingTypeButtons = null;
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
                var tile = input.GetTileUnderCursor();
                //TODO: Rectangle
                if (tile != null && buildingTypeButtons[selected].CanBuildAt(tile.MapLocation)) {
                    LevelManager.CurrentLevel.BuildBuilding(buildingTypeButtons[selected], tile.MapLocation, input.Player);
                }
                else {
                    //TODO: Change cursor
                }
            }
        }

        private void OnMouseMove(MouseMovedEventArgs e) {

        }
    }
}
