using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    public delegate DefaultComponent LoadComponentDelegate(LevelManager level, PluginData storedData);
    public delegate DefaultComponent ConstructComponentDelegate(LevelManager level, XElement data);

    public interface IComponentFactory {
        LoadComponentDelegate GetComponentLoader(string componentName);

        ConstructComponentDelegate GetComponentConstructor(string componentName);
    }
}
