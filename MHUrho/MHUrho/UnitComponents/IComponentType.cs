using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    public interface IComponentType : ITypePlugin
    {
        MHUrhoComponent CreateNewInstance(LevelManager level, XElement xmlData);

        MHUrhoComponent LoadSavedInstance(LevelManager level, PluginDataWrapper storedData);
    }
}
