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

		public XName GamePackElement => PackageManager.XMLNamespace + "gamePack";


		public XName NameAttribute => "name";
		public XName Description => PackageManager.XMLNamespace + "description";
		public XName PathToThumbnail => PackageManager.XMLNamespace + "pathToThumbnail";
		public XName Levels => PackageManager.XMLNamespace + "levels";
		public XName LevelLogicTypes => PackageManager.XMLNamespace + "levelLogicTypes";
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

		protected GamePackXml()
		{

		}
	}

	class LevelsXml {

		public static LevelsXml Inst { get; } = new LevelsXml();

		public XName DataDirPath => PackageManager.XMLNamespace + "dataDirPath";
		public XName Level => PackageManager.XMLNamespace + "level";

		protected LevelsXml()
		{

		}
	}

	class LevelLogicTypesXml {

		public static LevelLogicTypesXml Inst { get; } = new LevelLogicTypesXml();

		public XName LevelLogicType => PackageManager.XMLNamespace + "levelLogicType";

		protected LevelLogicTypesXml()
		{

		}
	}

	class PlayerAITypesXml {

		public static PlayerAITypesXml Inst { get; } = new PlayerAITypesXml();

		public XName PlayerAIType => PackageManager.XMLNamespace + "playerAIType";

		protected PlayerAITypesXml()
		{

		}
	}

	class ResourceTypesXml {

		public static ResourceTypesXml Inst { get; } = new ResourceTypesXml();

		public XName ResourceType => PackageManager.XMLNamespace + "resourceType";

		protected ResourceTypesXml()
		{

		}
	}

	class TileTypesXml {

		public static TileTypesXml Inst { get; } = new TileTypesXml();

		public XName DefaultTileType => PackageManager.XMLNamespace + "defaultTileType";

		public XName TileType => PackageManager.XMLNamespace + "tileType";

		protected TileTypesXml()
		{

		}
	}

	class UnitTypesXml {

		public static UnitTypesXml Inst { get; } = new UnitTypesXml();

		public XName UnitType => PackageManager.XMLNamespace + "unitType";

		protected UnitTypesXml()
		{

		}
	}

	class ProjectileTypesXml {

		public static ProjectileTypesXml Inst { get; } = new ProjectileTypesXml();

		public XName ProjectileType => PackageManager.XMLNamespace + "projectileType";

		protected ProjectileTypesXml()
		{

		}
	}

	class BuildingTypesXml {

		public static BuildingTypesXml Inst { get; } = new BuildingTypesXml();

		public XName BuildingType => PackageManager.XMLNamespace + "buildingType";

		protected BuildingTypesXml()
		{

		}
	}

	class LevelXml {

		public static LevelXml Inst { get; } = new LevelXml();

		public XName NameAttribute => "name";

		public XName Description => PackageManager.XMLNamespace + "description";

		public XName Thumbnail => PackageManager.XMLNamespace + "thumbnail";

		public XName LogicTypeName => PackageManager.XMLNamespace + "logicTypeName";

		public XName DataPath => PackageManager.XMLNamespace + "dataPath";

		public XName MapSize => PackageManager.XMLNamespace + "mapSize";

		protected LevelXml()
		{

		}
	}

	class LevelLogicTypeXml {

		public static LevelLogicTypeXml Inst { get; } = new LevelLogicTypeXml();

		public XName NameAttribute => "name";

		public XName IDAttribute => "ID";

		public XName AssemblyPath => PackageManager.XMLNamespace + "assemblyPath";

		public XName Extension => PackageManager.XMLNamespace + "extension";

		protected LevelLogicTypeXml()
		{

		}
	}

	class PlayerAITypeXml {

		public static PlayerAITypeXml Inst { get; } = new PlayerAITypeXml();

		public XName NameAttribute => "name";

		public XName IDAttribute => "ID";

		public XName CategoryAttribute => "category";

		public XName IconTextureRectangle => PackageManager.XMLNamespace + "iconTextureRectangle";

		public XName AssemblyPath => PackageManager.XMLNamespace + "assemblyPath";

		public XName Extension => PackageManager.XMLNamespace + "extension";

		protected PlayerAITypeXml()
		{

		}
	}

	class ResourceTypeXml {

		public static ResourceTypeXml Inst { get; } = new ResourceTypeXml();

		public XName NameAttribute => "name";

		public XName IconTextureRectangle => PackageManager.XMLNamespace + "iconTextureRectangle";

		protected ResourceTypeXml()
		{

		}
	}

	class TileTypeXml {

		public static TileTypeXml Inst { get; } = new TileTypeXml();

		public XName NameAttribute => "name";

		public XName IDAttribute => "ID";

		public XName IconTextureRectangle => PackageManager.XMLNamespace + "iconTextureRectangle";

		public XName TexturePath => PackageManager.XMLNamespace + "texturePath";

		public XName MinimapColor => PackageManager.XMLNamespace + "minimapColor";

		public XName ManuallySpawnable => PackageManager.XMLNamespace + "manuallySpawnable";

		protected TileTypeXml()
		{

		}
	}

	class UnitTypeXml : EntityWithIconXml {

		public new static UnitTypeXml Inst { get; } = new UnitTypeXml();

		protected UnitTypeXml()
		{

		}
	}

	class ProjectileTypeXml : EntityXml {

		public new static ProjectileTypeXml Inst { get; } = new ProjectileTypeXml();

		protected ProjectileTypeXml()
		{

		}
	}

	class BuildingTypeXml : EntityWithIconXml {

		public new static BuildingTypeXml Inst { get; } = new BuildingTypeXml();

		public XName Size => PackageManager.XMLNamespace + "size";

		protected BuildingTypeXml()
		{

		}
	}

	class EntityXml {

		public static EntityXml Inst { get; } = new EntityXml();

		public XName NameAttribute => "name";

		public XName IDAttribute => "ID";

		public XName Assets => PackageManager.XMLNamespace + "assets";

		public XName AssemblyPath => PackageManager.XMLNamespace + "assemblyPath";

		public XName Extension => PackageManager.XMLNamespace + "extension";

		protected EntityXml()
		{

		}
	}

	class EntityWithIconXml : EntityXml {

		public new static EntityWithIconXml Inst { get; } = new EntityWithIconXml();


		public XName IconTextureRectangle => PackageManager.XMLNamespace + "iconTextureRectangle";

		public XName ManuallySpawnable => PackageManager.XMLNamespace + "manuallySpawnable";

		protected EntityWithIconXml()
		{

		}
	}

	class AssetsXml {

		public static AssetsXml Inst { get; } = new AssetsXml();

		public XName TypeAttribute => "type";

		public XName Path => PackageManager.XMLNamespace + "path";

		public XName Scale => PackageManager.XMLNamespace + "scale";

		public XName Model => PackageManager.XMLNamespace + "model";

		public XName CollisionShape => PackageManager.XMLNamespace + "collisionShape";

		public const string XmlPrefabType = "xmlprefab";

		public const string BinaryPrefabType = "binaryprefab";

		public const string ItemsType = "items";

		protected AssetsXml()
		{

		}
	}

	class ModelXml {

		public static ModelXml Inst { get; } = new ModelXml();

		public XName TypeAttribute => "type";

		public XName ModelPath => PackageManager.XMLNamespace + "modelPath";

		public XName Material => PackageManager.XMLNamespace + "material";

		public const string StaticModelType = "static";

		public const string AnimatedModelType = "animated";

		protected ModelXml()
		{

		}
	}

	class MaterialXml {

		public static MaterialXml Inst { get; } = new MaterialXml();

		public XName MaterialListPath => PackageManager.XMLNamespace + "materialListPath";

		public XName SimpleMaterialPath => PackageManager.XMLNamespace + "simpleMaterialPath";

		public XName GeometryMaterial => PackageManager.XMLNamespace + "geometryMaterial";

		protected MaterialXml()
		{

		}
	}

	class GeometryMaterialXml {

		public static GeometryMaterialXml Inst { get; } = new GeometryMaterialXml();

		public XName IndexAttribute => "index";

		public XName MaterialPath => PackageManager.XMLNamespace + "materialPath";

		protected GeometryMaterialXml()
		{

		}
	}

	class CollisionShapeXml {

		public static CollisionShapeXml Inst { get; } = new CollisionShapeXml();

		public XName Box => PackageManager.XMLNamespace + "box";

		public XName Capsule => PackageManager.XMLNamespace + "capsule";

		public XName Cone => PackageManager.XMLNamespace + "cone";

		public XName ConvexHull => PackageManager.XMLNamespace + "convexHull";

		public XName Cylinder => PackageManager.XMLNamespace + "cylinder";

		public XName Sphere => PackageManager.XMLNamespace + "sphere";

		public XName Diameter => PackageManager.XMLNamespace + "diameter";

		public XName Height => PackageManager.XMLNamespace + "height";

		public XName Size => PackageManager.XMLNamespace + "size";

		public XName Scale => PackageManager.XMLNamespace + "scale";

		public XName ModelPath => PackageManager.XMLNamespace + "modelPath";

		public XName Position => PackageManager.XMLNamespace + "position";

		public XName Rotation => PackageManager.XMLNamespace + "rotation";
	}

	class IntVector2Xml {

		public static IntVector2Xml Inst { get; } = new IntVector2Xml();

		public XName XAttribute => "x";

		public XName YAttribute => "y";

		protected IntVector2Xml()
		{

		}
	}

	class IntRectXml {
		public static IntRectXml Inst { get; } = new IntRectXml();

		public XName LeftAttribute => "left";

		public XName RightAttribute => "right";

		public XName TopAttribute => "top";

		public XName BottomAttribute => "bottom";

		protected IntRectXml()
		{

		}
	}

	class Vector2Xml {

		public static Vector2Xml Inst { get; } = new Vector2Xml();

		public XName XAttribute => "x";

		public XName YAttribute => "y";

		protected Vector2Xml()
		{

		}
	}

	class Vector3Xml {
		public static Vector3Xml Inst { get; } = new Vector3Xml();

		public XName XAttribute => "x";

		public XName YAttribute => "y";

		public XName ZAttribute => "z";

		protected Vector3Xml()
		{

		}
	}

	class QuaternionXml {
		public static QuaternionXml Inst { get; } = new QuaternionXml();

		public XName XAngleAttribute => "xAngle";

		public XName YAngleAttribute => "yAngle";

		public XName ZAngleAttribute => "zAngle";
	}

	class ColorXml {
		public static ColorXml Inst { get; } = new ColorXml();

		public XName RAttribute => "R";

		public XName GAttribute => "G";

		public XName BAttribute => "B";

		public XName AAttribute => "A";

		protected ColorXml()
		{

		}
	}
}
