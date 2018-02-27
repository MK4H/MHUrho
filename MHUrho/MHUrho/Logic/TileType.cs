using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Storage;
using Urho;
using MHUrho.Packaging;

namespace MHUrho.Logic
{
    public class TileType
    {
        public int ID { get; private set; }

        public float MovementSpeedModifier { get; private set; }

        public Texture3D Texture { get; private set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        public StTileType Save() {
            var storedTileType = new StTileType();
            storedTileType.Name = Name;
            storedTileType.TileTypeID = ID;
            storedTileType.PackageID = Package.ID;

            return storedTileType;
        }

        protected TileType()
        {

        }
    }
}