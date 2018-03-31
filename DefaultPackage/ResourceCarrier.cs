using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;
using Urho.IO;
using Urho.Resources;

namespace DefaultPackage {
    class ResourceCarrier : DefaultComponent {
        public static string ComponentName => nameof(ResourceCarrier);

        public override string Name => ComponentName;

        public override PluginDataWrapper SaveState() {
            throw new NotImplementedException();
        }

        public override void OnAttachedToNode(Node node) {
            base.OnAttachedToNode(node);
        }


        public override DefaultComponent CloneComponent() {
            throw new NotImplementedException();
        }
    }
}
