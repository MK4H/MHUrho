using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrhoStandard
{
    public interface IConfigManager
    {
        //TODO: Load from config file
        List<string> PackagePaths { get; }

        void AddPackagePath(string absolutePath);
        void RemovePackagePath(string absolutePath);

    }
}
