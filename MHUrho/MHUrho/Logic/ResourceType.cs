using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho.Resources;

namespace MHUrho.Logic
{
	public class ResourceType : ILoadableType, IDisposable {
		private const string IDAttributeName = "ID";
		private const string NameAttribute = "name";
		private const string IconPathElement = "iconPath";

		public int ID { get; set; }

		public string Name { get; private set; }


		public GamePack Package { get; private set; }
	
		public Image Icon { get; private set; }

		public void Load(XElement xml, GamePack package) {
			ID = xml.GetIntFromAttribute(IDAttributeName);
			Name = xml.Attribute(NameAttribute).Value;
			Icon = LoadIcon(xml, package);
			Package = package;
		}

		public void Dispose() {
			Icon?.Dispose();
		}

		private Image LoadIcon(XElement typeElement, GamePack package) {
			string iconPath = XmlHelpers.GetFullPathFromChild(typeElement, IconPathElement, package.XmlDirectoryPath);
			return PackageManager.Instance.GetImage(iconPath);
		}
	}
}
