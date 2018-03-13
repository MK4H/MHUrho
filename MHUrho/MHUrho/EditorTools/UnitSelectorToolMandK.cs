using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Input;
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

        private void HandleSelection(IntVector2 topLeft, IntVector2 bottomRight) {
            //TODO: MAP SELECT
            Urho.IO.Log.Write(LogLevel.Debug, $"Selection: {topLeft} | {bottomRight}");
        }

        public override void Dispose() {
            
        }
    }
}
