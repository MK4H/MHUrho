using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    public enum DefaultComponents {
        DirectShooter,
        Meele,
        ResourceCarrier,
        UnitSelector,
        WallClimber,
        WorkQueue,
        WorldWalker
    }

    public abstract class DefaultComponent : Component {

        public new abstract DefaultComponents ID{ get; }

        public abstract string Name { get; }

        public abstract PluginData SaveState();
    }
}
