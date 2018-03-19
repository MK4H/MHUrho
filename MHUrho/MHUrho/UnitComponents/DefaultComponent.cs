using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    public abstract class DefaultComponent : Component {

        public abstract string Name { get; }

        public abstract PluginData SaveState();
    }
}
