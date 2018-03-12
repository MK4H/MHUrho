using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools
{
    class StaticRectangleToolMandK : StaticRectangleTool, IMandKTool
    {
        public IEnumerable<Button> Buttons => null;

        public IntVector2 Size { get; set; }

        private GameMandKController input;
        private Map map;

        private bool enabled;

        public StaticRectangleToolMandK(GameMandKController input, Map map, IntVector2 size) {
            this.input = input;
            this.map = map;
            this.Size = size;
        }

        public override void Dispose() {

        }

        public void Enable() {
            if (enabled) return;

            input.MouseMove += OnMouseMove;
            enabled = true;
            
        }

        public void Disable() {
            if (!enabled) return;

            input.MouseMove -= OnMouseMove;
            map.DisableHighlight();
            enabled = false;
        }

        private void OnMouseMove(MouseMovedEventArgs e) {
            var centerTile = input.GetTileUnderCursor();
            if (centerTile != null) {
                map.HighlightArea(centerTile, Size);
            }

        }
    }
}
