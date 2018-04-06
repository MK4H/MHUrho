using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Control;
using Urho;

namespace MHUrho.UnitComponents
{
    public abstract class Selector : DefaultComponent {
        public virtual IPlayer Player { get; }

        public virtual bool Selected { get; protected set; }

        public abstract bool Order(ITile tile);

        public abstract bool Order(Unit unit);

        public abstract void Select();

        public abstract void Deselect();
    }
}
