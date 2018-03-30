using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;

namespace MHUrho.Packaging
{
    internal abstract class EntityTypeFactory
    {
        public static EntityTypeFactory GetFactory<T>(Dictionary<string, TileType> tileTypes,
                                                      Dictionary<string, UnitType> unitTypes,
                                                      Dictionary<string, BuildingType> buildingTypes,
                                                      Dictionary<string, ProjectileType> projectileTypes) 
            where T : class, IIDNameAndPackage 
        {
            if (typeof(T) == typeof(UnitType)) {
                return new UnitTypeFactory(unitTypes);
            }
            else if (typeof(T) == typeof(BuildingType)) {
                return new BuildingTypeFactory(buildingTypes);
            }
            else if (typeof(T) == typeof(TileType)) {
                return new TileTypeFactory(tileTypes);
            }
            else if (typeof(T) == typeof(ProjectileType)) {
                return new ProjectileTypeFactory(projectileTypes);
            }
            else {
                throw new ArgumentException("Unknown type", nameof(T));
            }
        }

        public abstract string GroupName { get; }
        public abstract string ItemName { get; }
        public abstract string NameAttribute { get; }

        public abstract IIDNameAndPackage Load(XElement xml, int newID, string pathToPackageXmlDir, ResourcePack package);

        private class UnitTypeFactory : EntityTypeFactory {
            public override string GroupName => "unitTypes";

            public override string ItemName => "unitType";

            public override string NameAttribute => "name";

            public override IIDNameAndPackage Load(XElement xml, int newID, string pathToPackageXmlDir, ResourcePack package) {
                return UnitType.Load(xml, newID, pathToPackageXmlDir, package);
            }
        }

        private class BuildingTypeFactory : EntityTypeFactory {
            public override string GroupName => "buildingTypes";

            public override string ItemName => "buildingType";

            public override string NameAttribute => "name";

            public override IIDNameAndPackage Load(XElement xml, int newID, string pathToPackageXmlDir, ResourcePack package) {
                return BuildingType.Load(xml, newID, pathToPackageXmlDir, package);
            }
        }

        private class TileTypeFactory : EntityTypeFactory {
            public override string GroupName => "tileTypes";

            public override string ItemName => "tileType";

            public override string NameAttribute => "name";

            public override IIDNameAndPackage Load(XElement xml, int newID, string pathToPackageXmlDir, ResourcePack package) {
                return TileType.Load(xml, newID, pathToPackageXmlDir, package);
            }
        }

        private class ProjectileTypeFactory : EntityTypeFactory {
            public override string GroupName => "projectileTypes";

            public override string ItemName => "projectileType";

            public override string NameAttribute => "name";

            public override IIDNameAndPackage Load(XElement xml, int newID, string pathToPackageXmlDir, ResourcePack package) {
                return ProjectileType.Load(xml, newID, pathToPackageXmlDir, package);
            }
        }
    }
}
