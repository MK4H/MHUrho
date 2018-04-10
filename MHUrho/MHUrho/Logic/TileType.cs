using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Storage;
using Urho;
using Urho.Resources;
using MHUrho.Packaging;

namespace MHUrho.Logic
{
    public class TileType : IEntityType {
        private const string NameAttributeName = "name";
        private const string TexturePathElementName = "texturePath";
        private const string MovementSpeedElementName = "movementSpeed";

        public int ID { get; set; }

        public float MovementSpeedModifier { get; private set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        //TODO: Check that texture is null
        public Rect TextureCoords { get; private set; }

        private string imagePath;

        public static string GetNameFromXml(XElement tileTypeElement) {
            return tileTypeElement.Attribute("name").Value;
        }

        public void Load(XElement xml, int newID, ResourcePack package) {
            //TODO: Check for errors
            ID = newID;
            Name = xml.Attribute(NameAttributeName).Value;
            imagePath = XmlHelpers.GetFullPath(xml, TexturePathElementName, package.XmlDirectoryPath);
            MovementSpeedModifier = XmlHelpers.GetFloat(xml, MovementSpeedElementName);
            Package = package;
        }

        public StEntityType Save() {
            var storedTileType = new StEntityType {
                Name = Name,
                TypeID = ID,
                PackageID = Package.ID
            };

            return storedTileType;
        }

        public TileType() {
            ID = 0;
        }

        /// <summary>
        /// Called by map, after constructiong the one big texture out of all the tileType images
        /// </summary>
        /// <param name="coords">Coords of the image in the map texture</param>
        public void SetTextureCoords(Rect coords) {
            TextureCoords = coords;
        }

        public Image GetImage() {
            return PackageManager.Instance.ResourceCache.GetImage(imagePath);
        }


    }
}