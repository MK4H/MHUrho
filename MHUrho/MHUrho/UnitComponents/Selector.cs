using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;

namespace MHUrho.UnitComponents
{
    abstract class Selector : Component {
        public abstract bool Ordered(ITile tile);

        public abstract bool Ordered(IUnit unit);
    }
}
