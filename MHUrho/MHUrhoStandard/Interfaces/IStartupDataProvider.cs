using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using Urho.IO;

namespace MHUrhoStandard
{
    /// <summary>
    /// Provides consistent access API to data that is inaccesible via standard System.IO interface on Android
    /// </summary>
    public interface IStartupDataProvider
    {
        Stream GetFile(string relativePath);

        void Init(FileSystem fs);
    }
}
