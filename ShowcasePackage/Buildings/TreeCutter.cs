using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.DefaultComponents;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UserInterface.MandK;
using MHUrho.Helpers.Extensions;
using Urho;

namespace ShowcasePackage.Buildings
{
	public class TreeCutterType : BaseBuildingTypePlugin
	{
		public static string TypeName = "TreeCutter";
		public static int TypeID = 6;

		public override string Name => TypeName;
		public override int ID => TypeID;

		protected override void Initialize(XElement extensionElement, GamePack package)
		{

		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return new TreeCutter(level, building, this);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return new TreeCutter(level, building, this);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, IPlayer owner, ILevelManager level)
		{
			return topLeftTileIndex == bottomRightTileIndex &&
					level.Map
						.GetTilesInRectangle(topLeftTileIndex, bottomRightTileIndex)
						.All((tile) => tile.Building == null && tile.Units.Count == 0);
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new LineBuilder(input, ui, camera, input.Level.Package.GetBuildingType(ID));
		}
	}

	public class TreeCutter : BuildingInstancePlugin
	{
		readonly TreeCutterType type;

		public TreeCutter(ILevelManager level, IBuilding building, TreeCutterType type)
			: base(level, building)
		{
			this.type = type;
			StaticRangeTarget.CreateNew(this, level, building.Center);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{

		}

		public override void LoadState(PluginDataWrapper pluginData)
		{

		}

		public override void Dispose()
		{

		}

		
	}
}
