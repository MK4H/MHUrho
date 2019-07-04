using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;
using Urho.Resources;

namespace MHUrho.Logic
{
	public class ResourceType : ILoadableType, IDisposable {

		public int ID { get; private set; }

		public string Name { get; private set; }


		public GamePack Package { get; private set; }
	
		public IntRect IconRectangle { get; private set; }

		public void Load(XElement xml, GamePack package)
		{
			Package = package;
			try {
				ID = XmlHelpers.GetID(xml);
				Name = XmlHelpers.GetName(xml);
				IconRectangle = XmlHelpers.GetIconRectangle(xml);
			}
			catch (Exception e)
			{
				string message = $"Resource type loading failed: Invalid XML of the package {package.Name}";
				Urho.IO.Log.Write(LogLevel.Error, message);
				throw new PackageLoadingException(message, e);
			}
		}

		public override bool Equals(object obj)
		{
			return object.ReferenceEquals(this, obj);
		}

		public override int GetHashCode()
		{
			return ID;
		}

		/// <summary>
		/// Clears any cache state dependent on the current level
		/// </summary>
		public void ClearCache()
		{
			//If you add any cache dependent on current level, clear it here
		}

		public void Dispose() {

		}

	}
}
