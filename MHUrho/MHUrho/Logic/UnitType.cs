using System;
using System.Collections.Generic;
using System.Text;

using Urho;

namespace MHUrho
{
    public class UnitType
    {
        public static Dictionary<string, UnitType> Types = new Dictionary<string, UnitType>();

        HashSet<string> PassableTileTypes;


        //TODO: More loaded properties
        Model model;

        public bool CanPass(string tileType)
        {
            return PassableTileTypes.Contains(tileType);
        }

        /// <summary>
        /// Loads unit types from predefined directory
        /// </summary>
        public static void LoadUnitTypes()
        {

        }
    }
}
