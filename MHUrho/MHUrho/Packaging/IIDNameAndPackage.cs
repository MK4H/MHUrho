using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
    public interface IIDNameAndPackage
    {
        int ID { get; set; }
        string Name { get; }
        GamePack Package { get; }
    }
}
