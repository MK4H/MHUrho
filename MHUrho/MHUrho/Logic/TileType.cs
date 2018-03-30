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
    public class TileType : IIDNameAndPackage {
        private const string NameAttribute = "name";
        private const string TexturePathElement = "texturePath";
        private const string MovementSpeedElement = "movementSpeed";

        public int ID { get; set; }

        public float MovementSpeedModifier { get; private set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        //TODO: Check that texture is null
        public Rect TextureCoords { get; private set; }

        private readonly string imagePath;

        public static TileType Load(XElement xml, int newID, string pathToPackageXmlDirname, ResourcePack package) {
            //TODO: Check for errors
            string name = xml.Attribute(NameAttribute).Value;
            string imagePath = XmlHelpers.GetFullPath(xml, TexturePathElement, pathToPackageXmlDirname);
            float movementSpeed = XmlHelpers.GetFloat(xml, MovementSpeedElement);

            TileType newTileType = new TileType(name, movementSpeed, imagePath, package) {
                ID = newID
            };

            return newTileType;
        }

        public StEntityType Save() {
            var storedTileType = new StEntityType {
                Name = Name,
                TypeID = ID,
                PackageID = Package.ID
            };

            return storedTileType;
        }

        protected TileType(string name, float movementSpeedModifier, string imagePath, ResourcePack package) {
            ID = 0;
            this.Name = name;
            this.MovementSpeedModifier = movementSpeedModifier;
            this.imagePath = imagePath;
            this.Package = package;
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