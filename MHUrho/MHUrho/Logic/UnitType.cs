using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic
{
    public class UnitType
    {

        public int ID { get; private set; }

        public string Name { get; private set; }

        public ResourcePack Package { get; private set; }

        HashSet<string> PassableTileTypes;

        //TODO: More loaded properties
        Model model;

        public StUnitType Save() {
            var storedUnitType = new StUnitType();
            storedUnitType.Name = Name;
            storedUnitType.UnitTypeID = ID;
            storedUnitType.PackageID = Package.ID;

            return storedUnitType;
        }

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
