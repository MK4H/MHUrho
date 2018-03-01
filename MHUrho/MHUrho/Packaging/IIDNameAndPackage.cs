using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
    interface IIDNameAndPackage
    {
        int ID { get; set; }
        string Name { get; }

        ResourcePack Package { get; }
    }
}
