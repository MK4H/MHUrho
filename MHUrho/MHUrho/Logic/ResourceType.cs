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
    public class ResourceType : IEntityType, IDisposable {
        private const string NameAttribute = "name";
        private const string IconPathElement = "iconPath";

        public int ID { get; set; }

        public string Name { get; private set; }


        public ResourcePack Package { get; private set; }
    
        public Image Icon { get; private set; }

        public void Load(XElement xml, int newID, ResourcePack package) {
            ID = newID;
            Name = xml.Attribute(NameAttribute).Value;
            Icon = LoadIcon(xml, package);
            Package = package;
        }

        public StEntityType Save() {
            return new StEntityType {
                                        Name = Name,
                                        TypeID = ID,
                                        PackageID = Package.ID
                                    };
        }

        public void Dispose() {
            Icon?.Dispose();
        }

        private Image LoadIcon(XElement typeElement, ResourcePack package) {
            string iconPath = XmlHelpers.GetFullPath(typeElement, IconPathElement, package.XmlDirectoryPath);
            return PackageManager.Instance.ResourceCache.GetImage(iconPath);
        }
    }
}
