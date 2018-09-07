using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace MHUrho.Packaging
{
    static class GamePackXml
    {
		public static readonly XName TileTypes= PackageManager.XMLNamespace + "tileTypes";
		public static readonly XName TileType = PackageManager.XMLNamespace + "tileType";
		public static readonly XName UnitTypes = PackageManager.XMLNamespace + "unitTypes";
		public static readonly XName UnitType = PackageManager.XMLNamespace + "unitType";
		public static readonly XName BuildingTypes = PackageManager.XMLNamespace + "buildingTypes";
		public static readonly XName BuildingType = PackageManager.XMLNamespace + "buildingType";
		public static readonly XName ProjectileTypes = PackageManager.XMLNamespace + "projectileTypes";
		public static readonly XName ProjectileType = PackageManager.XMLNamespace + "projectileType";
		public static readonly XName ResourceTypes = PackageManager.XMLNamespace + "resourceTypes";
		public static readonly XName ResourceType = PackageManager.XMLNamespace + "resourceType";
		public static readonly XName PlayerAITypes = PackageManager.XMLNamespace + "playerAITypes";
		public static readonly XName PlayerAIType = PackageManager.XMLNamespace + "playerAIType";
		public static readonly XName Levels = PackageManager.XMLNamespace + "levels";
		public static readonly XName Level = PackageManager.XMLNamespace + "level";
	}
}
