using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    class ResourceCarrier : DefaultComponent {

        public static string ComponentName = nameof(ResourceCarrier);
        public static DefaultComponents ComponentID = DefaultComponents.ResourceCarrier;

        public override string Name => ComponentName;
        public override DefaultComponents ID => ComponentID;


        public override PluginData SaveState() {
            throw new NotImplementedException();
        }
    }
}
