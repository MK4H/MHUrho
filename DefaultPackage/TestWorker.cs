using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace DefaultPackage
{
    public class TestWorkerType : IUnitTypePlugin
    {
        public bool IsMyType(string typeName) {
            return typeName == "TestWorker";
        }

        public IUnitInstancePlugin CreateNewInstance(LevelManager level, Node unitNode, Unit unit) {
            throw new NotImplementedException();
        }

        public IUnitInstancePlugin LoadNewInstance(LevelManager level, Node unitNode, Unit unit, PluginDataWrapper pluginData) {
            throw new NotImplementedException();
        }

        public void Initialize(XElement extensionElement, PackageManager packageManager) {
            throw new NotImplementedException();
        }
    }

    public class TestWorkerInstance : IUnitInstancePlugin {
        public bool Order(ITile tile) {
            throw new NotImplementedException();
        }

        public void OnUpdate(float timeStep) {
            throw new NotImplementedException();
        }

        public void SaveState(PluginDataWrapper pluginData) {
            throw new NotImplementedException();
        }
    }
}
