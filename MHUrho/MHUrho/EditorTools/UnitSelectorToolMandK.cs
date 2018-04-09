using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Control;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
    class UnitSelectorToolMandK : UnitSelectorTool, IMandKTool {

        private class SelectedInfo {
            public int Count => UnitSelectors.Count;

            public readonly List<UnitSelector> UnitSelectors;
            public readonly Button Button;

            public SelectedInfo(Button button, List<UnitSelector> unitSelectors) {
                Button = button;
                UnitSelectors = unitSelectors;
            }
        }

        public IEnumerable<Button> Buttons => buttons.Keys;

        private readonly GameMandKController input;
        private Map Map => input.LevelManager.Map;
        private readonly FormationController formation;

        private readonly DynamicRectangleToolMandK dynamicHighlight;

        private readonly Dictionary<UnitType, SelectedInfo> selected;

        private readonly Dictionary<Button, UnitType> buttons;

        private bool enabled;


        public UnitSelectorToolMandK(GameMandKController input) {
            this.input = input;
            this.selected = new Dictionary<UnitType, SelectedInfo>();
            this.buttons = new Dictionary<Button, UnitType>();

            this.formation = new FormationController(Map);
            this.dynamicHighlight = new DynamicRectangleToolMandK(input);
        }

        public void Enable() {
            if (enabled) return;

            dynamicHighlight.SelectionHandler += HandleSelection;
            dynamicHighlight.SingleClickHandler += HandleSingleClick;

            dynamicHighlight.Enable();

            input.UIManager.SelectionBarShowButtons(Buttons);

            enabled = true;
        }

        public void Disable() {
            if (!enabled) return;

            dynamicHighlight.SelectionHandler -= HandleSelection;
            dynamicHighlight.SingleClickHandler -= HandleSingleClick;
            dynamicHighlight.Disable();

            input.UIManager.SelectionBarClearButtons();

            enabled = false;
        }

        public override void Dispose() {
            Disable();
            dynamicHighlight.Dispose();
        }

        private void HandleSelection(IntVector2 topLeft, IntVector2 bottomRight) {
            Map.ForEachInRectangle(topLeft, bottomRight, SelectUnitsInTile);
        }

        private void HandleSingleClick(MouseButtonUpEventArgs e) {
            //TODO: Check that the raycastResults are ordered by distance
            foreach (var result in input.CursorRaycast()) {
                var selector = result.Node.GetComponent<Selector>();
                if (selector != null && selector.Player == input.Player) {
                    var unitSelector = selector as UnitSelector;
                    if (unitSelector != null && !unitSelector.Selected) {
                        unitSelector.Select();
                        AddUnit(unitSelector);
                        return;
                    }
                    else if (unitSelector != null && unitSelector.Selected) {
                        unitSelector.Deselect();
                        RemoveUnit(unitSelector);
                        return;
                    }
                }

                //TODO: Target

                var tile = Map.RaycastToTile(result);
                if (tile != null) {
                    formation.MoveToFormation(GetAllSelectedUnitSelectors(), tile);
                }
                
            }
        }

        //TODO: Select other things too
        private void SelectUnitsInTile(ITile tile) {
            //TODO: Maybe delete selector class, just search for unit
            foreach (var unit in tile.GetAllUnits()) {
                UnitSelector selector = unit.GetComponent<UnitSelector>();
                //Not selectable
                if (selector == null || selector.Selected) return;

                selector.Select();
                AddUnit(selector);
            }
        }

        private Button CreateButton(UnitType unitType) {
            var unitIcon = unitType.Icon;

            var buttonTexture = new Texture2D();
            buttonTexture.FilterMode = TextureFilterMode.Nearest;
            buttonTexture.SetNumLevels(1);
            buttonTexture.SetSize(unitIcon.Width, unitIcon.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
            buttonTexture.SetData(unitIcon);



            var button = new Button();
            button.SetStyle("SelectedUnitButton");
            button.Size = new IntVector2(100, 100);
            button.HorizontalAlignment = HorizontalAlignment.Center;
            button.VerticalAlignment = VerticalAlignment.Center;
            //button.Pressed += Button_Pressed;
            button.Texture = buttonTexture;
            button.FocusMode = FocusMode.ResetFocus;
            button.MaxSize = new IntVector2(100, 100);
            button.MinSize = new IntVector2(100, 100);
            button.Visible = true;

            var text = button.CreateText("Count");
            text.Value = "1";
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.VerticalAlignment = VerticalAlignment.Top;
            text.SetColor(new Color(r: 0f, g: 0f, b: 0f));
            text.SetFont(font: PackageManager.Instance.ResourceCache.GetFont("Fonts/Font.ttf"), size: 30);

            return button;
        }

        private void DisplayCount(Button button, int count) {
            Text text = (Text)button.GetChild("Count");
            text.Value = count.ToString();
        }

        private void AddUnit(UnitSelector unitSelector) {
            //TODO: Check owner of the units
            var unit = unitSelector.GetComponent<Unit>();
            if (selected.TryGetValue(unit.UnitType, out SelectedInfo info)) {
                if (info.UnitSelectors.Count == 0) {
                    input.UIManager.SelectionBarShowButton(info.Button);
                }
                info.UnitSelectors.Add(unitSelector);
                DisplayCount(info.Button, info.Count);
            }
            else {
                //Create info instance
                var button = CreateButton(unit.UnitType);
                selected.Add(unit.UnitType, new SelectedInfo(button, new List<UnitSelector> { unitSelector }));
                buttons.Add(button, unit.UnitType);
                input.UIManager.SelectionBarAddButton(button);
                input.UIManager.SelectionBarShowButton(button);
            }
        }

        private void RemoveUnit(UnitSelector unitSelector) {
            var info = selected[unitSelector.GetComponent<Unit>().UnitType];
            info.UnitSelectors.Remove(unitSelector);
            if (info.Count == 0) {
                input.UIManager.SelectionBarHideButton(info.Button);
            }
            else {
                DisplayCount(info.Button, info.Count);
            }
        }

        private IEnumerable<UnitSelector> GetAllSelectedUnitSelectors() {
            foreach (var unitType in selected.Values) {
                foreach (var unitSelector in unitType.UnitSelectors) {
                    yield return unitSelector;
                }
            }
        }
    }
}
