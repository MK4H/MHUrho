using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic
{
    public class UnitType : IIDNameAndPackage, IDisposable
    {

        public int ID { get; set; }

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

        public void Dispose() {
            //TODO: Release all disposable resources
            model.Dispose();
        }

    }
}
