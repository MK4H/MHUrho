using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.UnitComponents;

namespace DefaultPackage
{
    class ComponentFactory : IComponentFactory
    {
        private readonly Dictionary<string, LoadComponentDelegate> loaders;
        private readonly Dictionary<string, ConstructComponentDelegate> constructors;

        public ComponentFactory() {
            loaders = new Dictionary<string, LoadComponentDelegate>();
            //TODO: Maybe reflection
            loaders.Add(UnitSelector.ComponentName, UnitSelector.Load);
            loaders.Add(WorldWalker.ComponentName, WorldWalker.Load);
            loaders.Add(DirectShooter.ComponentName, DirectShooter.Load);
            loaders.Add(BuildingWorker.ComponentName, BuildingWorker.Load);
            //TODO: Add other components

            constructors = new Dictionary<string, ConstructComponentDelegate>();
        }



        public LoadComponentDelegate GetComponentLoader(string componentName) {
            return loaders[componentName];
        }

        public ConstructComponentDelegate GetComponentConstructor(string componentName) {
            return constructors[componentName];
        }
    }
}
