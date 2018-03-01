using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.Storage;
using Urho;
using Urho.Resources;
using MHUrho.Packaging;

namespace MHUrho.Logic
{
    public class TileType : IIDNameAndPackage, IDisposable
    {
        public int ID { get; set; }

        public float MovementSpeedModifier { get; private set; }

        public Image Texture { get; private set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        //TODO: Check that texture is null
        public Rect TextureCoords { get; private set; }

        public static TileType Load(XElement xml, string pathToPackageXML, ResourcePack package) {
            //TODO: Check for errors
            string name = xml.Attribute("name")?.Value;
            string texturePath = xml.Element("texture")?.Value;
            float movementSpeed = float.Parse(xml.Element("movementSpeed").Value);

            Image image = PackageManager.Instance.ResourceCache.GetImage(System.IO.Path.Combine(pathToPackageXML, texturePath));

            TileType newTileType = new TileType(name, movementSpeed, image, package);

            return newTileType;
        }

        public StTileType Save() {
            var storedTileType = new StTileType() {
                Name = Name,
                TileTypeID = ID,
                PackageID = Package.ID
            };

            return storedTileType;
        }

        protected TileType(string name, float movementSpeedModifier, Image image, ResourcePack package) {
            ID = 0;
            this.Name = name;
            this.MovementSpeedModifier = movementSpeedModifier;
            this.Texture = image;
            this.Package = package;
        }

        /// <summary>
        /// Called by map, after constructiong the one big texture out of all the tileType images, 
        /// switches tileType from holding the image itself to just holding the position in the big texture
        /// </summary>
        /// <param name="coords">Coords of the image in the map texture</param>
        public void SwitchImageToTextureCoords(Rect coords) {
            Texture.Dispose();
            Texture = null;

            TextureCoords = coords;
        }

        public void Dispose() {
            //TODO: Dispose all disposable resources
            Texture?.Dispose();
        }
    }
}