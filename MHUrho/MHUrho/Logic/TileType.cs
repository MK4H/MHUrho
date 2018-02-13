using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Urho;

namespace MHUrho
{
    public class TileType
    {
        static Dictionary<string, TileType> Types = new Dictionary<string, TileType>();

        /// <summary>
        /// Loads tile types from predefined directory
        /// </summary>
        public static void LoadTileTypes()
        {

        }

        public float MovementSpeedModifier { get; private set; }

        public Texture3D Texture { get; private set; }

        public string Name { get; set; }

        //TODO: TileType properties

        private TileType()
        {

        }
    }
}