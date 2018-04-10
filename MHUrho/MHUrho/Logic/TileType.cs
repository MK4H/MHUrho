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
        private const string IDAttributeName = "ID";
        private const string NameAttributeName = "name";
        private const string TexturePathElementName = "texturePath";
        private const string MovementSpeedElementName = "movementSpeed";

        public int ID { get; set; }

        public float MovementSpeedModifier { get; private set; }

        public string Name { get; private set; }

        public GamePack Package { get; private set; }

        //TODO: Check that texture is null
        public Rect TextureCoords { get; private set; }

        private string imagePath;

        public static string GetNameFromXml(XElement tileTypeElement) {
            return tileTypeElement.Attribute("name").Value;
        }

        public void Load(XElement xml, GamePack package) {
            //TODO: Check for errors
            ID = xml.GetIntFromAttribute(IDAttributeName);
            Name = xml.Attribute(NameAttributeName).Value;
            imagePath = XmlHelpers.GetFullPath(xml, TexturePathElementName, package.XmlDirectoryPath);
            MovementSpeedModifier = XmlHelpers.GetFloat(xml, MovementSpeedElementName);
            Package = package;
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