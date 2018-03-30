using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Plugins
{
    public interface ITypePlugin
    {
        bool IsMyType(string typeName);
    }
}
