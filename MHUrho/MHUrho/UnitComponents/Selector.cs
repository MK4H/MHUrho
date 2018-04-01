using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Control;
using Urho;

namespace MHUrho.UnitComponents
{
    public abstract class Selector : MHUrhoComponent {
        public virtual IPlayer Player { get; }

        public virtual bool Selected { get; set; }

        public abstract bool Ordered(ITile tile);

        public abstract bool Ordered(Unit unit);
    }
}
