using System;
using System.Collections.Generic;
using System.Text;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.Packaging
{
    public interface IAvailablePack
    {
		string Name { get; }

		string Description { get; }

		Texture2D Thumbnail { get; }

		string XmlDirectoryPath { get; }

		PackageManager PackageManager { get; }
    }
}
