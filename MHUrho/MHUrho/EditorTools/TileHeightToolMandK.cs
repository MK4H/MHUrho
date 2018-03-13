using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools
{
    class TileHeightToolMandK : TileHeightTool, IMandKTool {

        public IEnumerable<Button> Buttons => new Button[0];

        private const float Sensitivity = 0.01f;

        //private List<Button> buttons;
        private GameMandKController input;
        private Map map;
        private StaticRectangleToolMandK highlight;

        private bool enabled;
        private bool mouseButtonDown;

        private ITile centerTile;

        public TileHeightToolMandK(GameMandKController input, Map map) {
            this.input = input;
            this.map = map;
            highlight = new StaticRectangleToolMandK(input, map, new Urho.IntVector2(3, 3));
        }

        public void Enable() {
            if (enabled) return;

            
            input.MouseDown += MouseDown;
            input.MouseUp += MouseUp;
            input.MouseMove += MouseMove;
            highlight.Enable();
            //input.UIManager.SelectionBarShowButtons(buttons);
            enabled = true;
        }

        public void Disable() {
            if (!enabled) return;

            input.MouseDown -= MouseDown;
            input.MouseUp -= MouseUp;
            input.MouseMove -= MouseMove;
            highlight.Disable();
            enabled = false;
        }

        private void MouseDown(MouseButtonDownEventArgs e) {
            centerTile = input.GetTileUnderCursor();
            if (centerTile != null) {
                input.HideCursor();
                mouseButtonDown = true;
                highlight.FixHighlight(centerTile);
            }
        }

        private void MouseUp(MouseButtonUpEventArgs e) {
            if (centerTile != null) {
                input.ShowCursor(new Vector3(centerTile.Center.X, map.GetHeightAt(centerTile.Center), centerTile.Center.Y));
                mouseButtonDown = false;
                centerTile = null;
                highlight.FreeHighlight();
            }
        }

        private void MouseMove(MouseMovedEventArgs e) {
            if (mouseButtonDown) {
                map.ChangeTileHeight(centerTile, highlight.Size, -e.DY * Sensitivity);
            }
        }
    }
}
