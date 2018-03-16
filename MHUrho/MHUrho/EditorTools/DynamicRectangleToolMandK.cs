using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Urho.Gui;
using Urho;
using MHUrho.Input;
using MHUrho.WorldMap;

namespace MHUrho.EditorTools
{
    class DynamicRectangleToolMandK : DynamicRectangleTool, IMandKTool {
        public delegate void HandleSelectedRectangle(IntVector2 topLeft, IntVector2 bottomRight);

        public IEnumerable<Button> Buttons => Enumerable.Empty<Button>();

        public event HandleSelectedRectangle selectionHandler;

        private GameMandKController input;
        private Map map;

        private IntVector2 mouseDownPos;
        //TODO: Raycast into a plane and get point even outside the map
        private bool validMouseDown;

        private bool enabled;

        public DynamicRectangleToolMandK(GameMandKController input, Map map) {
            this.input = input;
            this.map = map;
        }

        public void Enable() {
            if (enabled) return;

            if (selectionHandler == null) {
                throw new
                    InvalidOperationException($"{nameof(selectionHandler)} was not set, cannot enable without handler");
            }

            input.MouseDown += MouseDown;
            input.MouseUp += MouseUp;
            input.MouseMove += MouseMove;

            enabled = true;
        }

        public void Disable() {
            if (!enabled) return;

            input.MouseDown -= MouseDown;
            input.MouseUp -= MouseUp;
            input.MouseMove -= MouseMove;

            enabled = false;
        }

        public override void Dispose() {
            Disable();
        }

        private void MouseDown(MouseButtonDownEventArgs e) {
            var tile = input.GetTileUnderCursor();
            //TODO: Raycast into a plane and get point even outside the map
            if (tile != null) {
                mouseDownPos = tile.Location;
                validMouseDown = true;
            }
        }

        private void MouseUp(MouseButtonUpEventArgs e) {
            var tile = input.GetTileUnderCursor();

            if (tile != null && validMouseDown) {
                var endTilePos = tile.Location;
                var topLeft = new IntVector2(Math.Min(mouseDownPos.X, endTilePos.X),
                                             Math.Min(mouseDownPos.Y, endTilePos.Y));
                var bottomRight = new IntVector2(Math.Max(mouseDownPos.X, endTilePos.X),
                                                 Math.Max(mouseDownPos.Y, endTilePos.Y));

                selectionHandler?.Invoke(topLeft, bottomRight);
                //TODO: Different highlight
                map.DisableHighlight();
                validMouseDown = false;
            }
        }

        private void MouseMove(MouseMovedEventArgs e) {
            //TODO: DRAW HIGHLIGHT SOMEHOW
            if (!validMouseDown) return;

            var tile = input.GetTileUnderCursor();

            if (tile != null && validMouseDown) {
                var endTilePos = tile.Location;
                var topLeft = new IntVector2(Math.Min(mouseDownPos.X, endTilePos.X),
                                             Math.Min(mouseDownPos.Y, endTilePos.Y));
                var bottomRight = new IntVector2(Math.Max(mouseDownPos.X, endTilePos.X),
                                                 Math.Max(mouseDownPos.Y, endTilePos.Y));

                map.HighlightArea(topLeft, bottomRight);
            }
        }

        
    }
}
