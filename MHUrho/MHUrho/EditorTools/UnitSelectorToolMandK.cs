using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools
{
    class UnitSelectorToolMandK : UnitSelectorTool, IMandKTool
    {

        public IEnumerable<Button> Buttons => Enumerable.Empty<Button>();

        private GameMandKController input;
        private Map map;

        private readonly DynamicRectangleToolMandK dynamicHighlight;

        private List<Selector> selected;

        private bool enabled;

        public UnitSelectorToolMandK(GameMandKController input, Map map) {
            this.input = input;
            this.map = map;

            this.dynamicHighlight = new DynamicRectangleToolMandK(input, map);
        }

        public void Enable() {
            if (enabled) return;

            dynamicHighlight.selectionHandler += HandleSelection;

            dynamicHighlight.Enable();

            enabled = true;
        }

        public void Disable() {
            if (!enabled) return;

            dynamicHighlight.selectionHandler -= HandleSelection;
            dynamicHighlight.Disable();


            enabled = false;
        }

        public override void Dispose() {

        }

        private void HandleSelection(IntVector2 topLeft, IntVector2 bottomRight) {
            map.ForEachInRectangle(topLeft, bottomRight, SelectUnitsInTile);
        }



        private void SelectUnitsInTile(ITile tile) {
            Selector selector = tile.Unit.Node.GetComponent<Selector>();
            if (selector != null) {
                var unit = selector.Node.GetComponent<Unit>();
            }
        }
    }
}
