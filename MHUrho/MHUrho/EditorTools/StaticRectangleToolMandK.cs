using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools
{
    class StaticRectangleToolMandK : StaticRectangleTool, IMandKTool
    {
        public IEnumerable<Button> Buttons => Enumerable.Empty<Button>();

        public IntVector2 Size { get; set; }

        private GameMandKController input;
        private Map map;

        private bool enabled;

        private ITile fixedCenter;

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

        public void FixHighlight(ITile centerTile) {
            fixedCenter = centerTile;
        }

        public void FreeHighlight() {
            fixedCenter = null;
        }

        private void OnMouseMove(MouseMovedEventArgs e) {
            if (fixedCenter != null) {
                map.HighlightArea(fixedCenter, Size, WorldMap.HighlightMode.Full, Color.Green);
                return;
            }

            var centerTile = input.GetTileUnderCursor();
            if (centerTile != null) {
                map.HighlightArea(centerTile, Size, WorldMap.HighlightMode.Full, Color.Green);
            }

        }
    }
}
