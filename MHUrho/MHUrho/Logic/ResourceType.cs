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
		const string IDAttributeName = "ID";
		const string NameAttribute = "name";

		public int ID { get; private set; }

		public string Name { get; private set; }


		public GamePack Package { get; private set; }
	
		public IntRect IconRectangle { get; private set; }

		public void Load(XElement xml, GamePack package) {
			ID = xml.GetIntFromAttribute(IDAttributeName);
			Name = xml.Attribute(NameAttribute).Value;
			IconRectangle = XmlHelpers.GetIconRectangle(xml);
			Package = package;
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
