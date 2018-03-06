using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;

namespace MHUrho.Input
{
    public abstract class TouchController
    {
        public bool Enabled { get; private set; }

        protected MyGame Game;
        protected Urho.Input Input => Game.Input;

        protected UI UI => Game.UI;

        protected TouchController(MyGame game) {
            this.Game = game;
            Enabled = false;
        }

        public void Enable() {
            if (Enabled) return;
            Enabled = true;

            Input.TouchBegin += TouchBegin;
            Input.TouchEnd += TouchEnd;
            Input.TouchMove += TouchMove;
        }

        public void Disable() {
            if (!Enabled) return;
            Enabled = false;

            Input.TouchBegin -= TouchBegin;
            Input.TouchEnd -= TouchEnd;
            Input.TouchMove -= TouchMove;
        }

        protected abstract void TouchBegin(TouchBeginEventArgs e);
        protected abstract void TouchEnd(TouchEndEventArgs e);
        protected abstract void TouchMove(TouchMoveEventArgs e);

    }
}
