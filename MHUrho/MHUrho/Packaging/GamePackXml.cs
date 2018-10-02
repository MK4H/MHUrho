using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace MHUrho.Packaging
{
	class GmaePackDirectoryXml {

		public static GmaePackDirectoryXml Inst { get; } = new GmaePackDirectoryXml();


		public XName GamePack => PackageManager.XMLNamespace + "gamePack";

	}

    class GamePackXml {

		public static GamePackXml Inst { get; } = new GamePackXml();

		public XName NameAttribute => "name";
		public XName Description => PackageManager.XMLNamespace + "description";
		public XName PathToThumbnail => PackageManager.XMLNamespace + "pathToThumbnail";
		public XName Levels => PackageManager.XMLNamespace + "levels";
		public XName PlayerAITypes => PackageManager.XMLNamespace + "playerAITypes";
		public XName ResourceTypes => PackageManager.XMLNamespace + "resourceTypes";
		public XName TileTypes => PackageManager.XMLNamespace + "tileTypes";
		public XName UnitTypes => PackageManager.XMLNamespace + "unitTypes";
		public XName ProjectileTypes => PackageManager.XMLNamespace + "projectileTypes";
		public XName BuildingTypes => PackageManager.XMLNamespace + "buildingTypes";
		public XName ResourceIconTexturePath => PackageManager.XMLNamespace + "resourceIconTexturePath";
		public XName TileIconTexturePath => PackageManager.XMLNamespace + "tileIconTexturePath";
		public XName UnitIconTexturePath => PackageManager.XMLNamespace + "unitIconTexturePath";
		public XName BuildingIconTexturePath => PackageManager.XMLNamespace + "buildingIconTexturePath";
		public XName PlayerIconTexturePath => PackageManager.XMLNamespace + "playerIconTexturePath";
	}

	class LevelsXml {

		public static LevelsXml Inst { get; } = new LevelsXml();

		public XName DataDirPath => PackageManager.XMLNamespace + "dataDirPath";
		public XName Level => PackageManager.XMLNamespace + "level";
	}

	class PlayerAITypesXml {

		public static PlayerAITypesXml Inst { get; } = new PlayerAITypesXml();

		public XName PlayerAIType => PackageManager.XMLNamespace + "playerAIType";
	}

	class ResourceTypesXml {

		public static ResourceTypesXml Inst { get; } = new ResourceTypesXml();

		public XName ResourceType => PackageManager.XMLNamespace + "resourceType";
	}

	class TileTypesXml {

		public static TileTypesXml Inst { get; } = new TileTypesXml();

		public XName DefaultTileType => PackageManager.XMLNamespace + "defaultTileType";

		public XName TileType => PackageManager.XMLNamespace + "tileType";
	}

	class UnitTypesXml {

		public static UnitTypesXml Inst { get; } = new UnitTypesXml();

		public XName UnitType => PackageManager.XMLNamespace + "unitType";
	}

	class ProjectileTypesXml {

		public static ProjectileTypesXml Inst { get; } = new ProjectileTypesXml();

		public XName ProjectileType => PackageManager.XMLNamespace + "projectileType";
	}

	class BuildingTypesXml {

		public static BuildingTypesXml Inst { get; } = new BuildingTypesXml();

		public XName BuildingType => PackageManager.XMLNamespace + "buildingType";
	}

	class LevelXml {

		public static LevelXml Inst { get; } = new LevelXml();

		public XName NameAttribute => "name";

		public XName Description => PackageManager.XMLNamespace + "description";

		public XName Thumbnail => PackageManager.XMLNamespace + "thumbnail";

		public XName AssemblyPath => PackageManager.XMLNamespace + "assemblyPath";

		public XName DataPath => PackageManager.XMLNamespace + "dataPath";

		public XName MapSize => PackageManager.XMLNamespace + "mapSize";
	}

	class PlayerAITypeXml {

		public static PlayerAITypeXml Inst { get; } = new PlayerAITypeXml();

		public XName NameAttribute => "name";

		public XName IDAttribute => "ID";

		public XName CategoryAttribute => "category";

		public XName IconTextureRectangle => PackageManager.XMLNamespace + "iconTextureRectangle";

		public XName AssemblyPath => PackageManager.XMLNamespace + "assemblyPath";

		public XName Extension => PackageManager.XMLNamespace + "extension";
	}

	class ResourceTypeXml {

		public static ResourceTypeXml Inst { get; } = new ResourceTypeXml();

		public XName NameAttribute => "name";

		public XName IconTextureRectangle => PackageManager.XMLNamespace + "iconTextureRectangle";
	}

	class TileTypeXml {

		public static TileTypeXml Inst { get; } = new TileTypeXml();

		public XName NameAttribute => "name";

		public XName IDAttribute => "ID";

		public XName IconTextureRectangle => PackageManager.XMLNamespace + "iconTextureRectangle";

		public XName TexturePath => PackageManager.XMLNamespace + "texturePath";

		public XName MinimapColor => PackageManager.XMLNamespace + "minimapColor";

		public XName ManuallySpawnable => PackageManager.XMLNamespace + "manuallySpawnable";
	}

	class UnitTypeXml : EntityWithIconXml {

		public new static UnitTypeXml Inst { get; } = new UnitTypeXml();
	}

	class ProjectileTypeXml : EntityXml {

		public new static ProjectileTypeXml Inst { get; } = new ProjectileTypeXml();
	}

	class BuildingTypeXml : EntityWithIconXml {

		public new static BuildingTypeXml Inst { get; } = new BuildingTypeXml();

		public XName Size => PackageManager.XMLNamespace + "size";
	}

	class EntityXml {

		public static EntityXml Inst { get; } = new EntityXml();

		public XName NameAttribute => "name";

		public XName IDAttribute => "ID";

		public XName Model => PackageManager.XMLNamespace + "model";

		public XName Material => PackageManager.XMLNamespace + "material";

		public XName AssemblyPath => PackageManager.XMLNamespace + "assemblyPath";

		public XName Extension => PackageManager.XMLNamespace + "extension";
	}

	class EntityWithIconXml : EntityXml {

		public new static EntityWithIconXml Inst { get; } = new EntityWithIconXml();


		public XName IconTextureRectangle => PackageManager.XMLNamespace + "iconTextureRectangle";

		public XName ManuallySpawnable => PackageManager.XMLNamespace + "manuallySpawnable";
	}

	class ModelXml {

		public static ModelXml Inst { get; } = new ModelXml();

		public XName ModelPath => PackageManager.XMLNamespace + "modelPath";

		public XName Scale => PackageManager.XMLNamespace + "scale";
	}

	class MaterialXml {

		public static MaterialXml Inst { get; } = new MaterialXml();

		public XName MaterialPath => PackageManager.XMLNamespace + "materialPath";

		public XName MaterialListPath => PackageManager.XMLNamespace + "materialListPath";
	}

	class IntVector2Xml {

		public static IntVector2Xml Inst { get; } = new IntVector2Xml();

		public XName XAttribute => "x";

		public XName YAttribute => "y";
	}

	class IntRectXml
	{
		public XName LeftAttribute => "left";

		public XName RightAttribute => "right";

		public XName TopAttribute => "top";

		public XName BottomAttribute => "bottom";
	}

	class Vector2Xml
	{
		public XName XAttribute => "x";

		public XName YAttribute => "y";
	}

	class Vector3Xml
	{
		public XName XAttribute => "x";

		public XName YAttribute => "y";

		public XName ZAttribute => "z";
	}

	class ColorXml
	{
		public XName RAttribute => "R";

		public XName GAttribute => "G";

		public XName BAttribute => "B";

		public XName AAttribute => "A";
	}
}
