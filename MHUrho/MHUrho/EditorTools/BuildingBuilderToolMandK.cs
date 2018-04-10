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
        public IEnumerable<Button> Buttons => buildingTypeButtons.Keys;

        private Dictionary<Button, BuildingType> buildingTypeButtons;

        private GameMandKController input;

        private ILevelManager Level => input.LevelManager;
        private Map Map => Level.Map;

        private Button selected;

        private bool enabled;

        public BuildingBuilderToolMandK(GameMandKController input) {
            this.input = input;
            this.buildingTypeButtons = new Dictionary<Button, BuildingType>();

            foreach (var buildingType in PackageManager.Instance.ActiveGame.BuildingTypes) {
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

                buildingTypeButtons.Add(button, buildingType);
            }
        }

        public void Enable() {
            if (enabled) return;

            input.UIManager.SelectionBarShowButtons(buildingTypeButtons.Keys);
            input.MouseDown += OnMouseDown;
            input.MouseMove += OnMouseMove;
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
            input.MouseMove -= OnMouseMove;

            Map.DisableHighlight();
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
            }
            else {
                input.UIManager.SelectButton((Button)e.Element);
                selected = (Button)e.Element;
            }
        }

        private void OnMouseDown(MouseButtonDownEventArgs e) {
            if (selected == null) return;


            var tile = input.GetTileUnderCursor();
            if (tile == null) return;

            var buildingType = buildingTypeButtons[selected];

            GetBuildingRectangle(tile, buildingType, out IntVector2 topLeft, out IntVector2 bottomRight);

            if (buildingType.CanBuildIn(topLeft, bottomRight, Level)) {
                LevelManager.CurrentLevel.BuildBuilding(buildingTypeButtons[selected], topLeft, input.Player);
            }
        }

        private void OnMouseMove(MouseMovedEventArgs e) {
            if (selected == null) return;

            var tile = input.GetTileUnderCursor();
            if (tile == null) return;

            var buildingType = buildingTypeButtons[selected];

            GetBuildingRectangle(tile, buildingType, out IntVector2 topLeft, out IntVector2 bottomRight);

            Color color = buildingType.CanBuildIn(topLeft, bottomRight, Level) ? Color.Green : Color.Red;
            Map.HighlightArea(topLeft, bottomRight, WorldMap.HighlightMode.Full, color);
        }

        private void GetBuildingRectangle(ITile centerTile, BuildingType buildingType, out IntVector2 topLeft, out IntVector2 bottomRight) {
            topLeft = centerTile.TopLeft - buildingType.Size / 2;
            bottomRight = topLeft + buildingType.Size - new IntVector2(1,1);
            Map.SnapToMap(ref topLeft, ref bottomRight);
        }
    }
}
