using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace ShowcasePackage.Buildings
{
	public class TreeType : BuildingTypePlugin {

		public override string Name => "Tree1";
		public override int ID => 2;

		public IReadOnlyDictionary<TileType, double> TileGrowth => tileGrowth;

		const string growsInElem = "growsIn";
		const string typeAttr = "type";
		const string speedAttr = "speed";

		readonly Dictionary<TileType, double> tileGrowth;

		public TreeType()
		{
			tileGrowth = new Dictionary<TileType, double>();
		}

		public override void Initialize(XElement extensionElement, GamePack package)
		{
			IEnumerable<XElement> growsIn = extensionElement.Elements(PackageManager.Instance.GetQualifiedXName(growsInElem));
			foreach (var element in growsIn) {
				TileType tileType = GetTileType(element, package);
				double? growthRate = GetGrowthRate(element);
				if (tileType == null || growthRate == null) {
					continue;
				}

				tileGrowth.Add(tileType, growthRate.Value);
			}
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			return new Tree(level, building, this);
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return new Tree(level, building, this);
		}

		public override bool CanBuildIn(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, ILevelManager level)
		{
			//Tree is just 1 tile big
			if (topLeftTileIndex != bottomRightTileIndex) {
				throw new ArgumentException("Wrong size of rectangle provided");
			}

			TileType tileType = level.Map.GetTileByTopLeftCorner(topLeftTileIndex).Type;
			//Check if it grows on the given tile type
			return tileGrowth.ContainsKey(tileType);
		}

		TileType GetTileType(XElement growsIn, GamePack package)
		{
			string typeNameOrID = growsIn.Attribute(typeAttr)?.Value;
			if (typeNameOrID == null)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"Invalid element in {Name} building type's extension element: {growsInElem} element missing {typeAttr} attribute.");
				return null;
			}

			try {
				if (int.TryParse(typeNameOrID, out int id)) {
					return package.GetTileType(id);
				}

				return package.GetTileType(typeNameOrID);
			}
			catch (ArgumentOutOfRangeException e) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"Invalid element in {Name} building type's extension element: {growsInElem} element invalid value in {typeAttr} attribute.");
				return null;
			}
		}

		double? GetGrowthRate(XElement growsIn)
		{
			string speedtxt = growsIn.Attribute(speedAttr)?.Value;

			if (speedtxt == null) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"Invalid element in {Name} building type's extension element: {growsInElem} element missing {speedAttr} attribute.");
				return null;
			}

			if (double.TryParse(speedtxt, out double speed)) {
				return speed;
			}
			Urho.IO.Log.Write(LogLevel.Warning,
							$"Invalid element in {Name} building type's extension element: {growsInElem} element invalid value in {typeAttr} attribute.");
			return null;
		}
	}

	public class Tree : BuildingInstancePlugin {

		TreeType type;

		static Random spreadRNG = new Random();

		double spreadProbability = 0.01;
		double spreadTimeout = 5;
		IntVector2 spreadSize = new IntVector2(100, 100);
		int spreadTries = 10;

		double curSpreadTimeout;

		public Tree(ILevelManager level, IBuilding building, TreeType type)
			: base(level, building)
		{
			this.type = type;
			this.curSpreadTimeout = spreadTimeout;
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


		public override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);

			curSpreadTimeout -= timeStep;
			if (curSpreadTimeout < 0) {
				curSpreadTimeout = spreadTimeout;

				if (spreadRNG.NextDouble() < spreadProbability) {
					Spread();
				}
			}
		
		}

		void Spread()
		{
			for (int i = 0; i < spreadTries; i++) {
				IntVector2 offset = new IntVector2(spreadRNG.Next(-(spreadSize.X / 2), spreadSize.X / 2),
													spreadRNG.Next(-(spreadSize.Y / 2), spreadSize.Y / 2));

				ITile target = Level.Map.GetTileByTopLeftCorner(Building.TopLeft + offset);

				if (target != null && target.Building == null && type.TileGrowth.ContainsKey(target.Type)) {
					Level.BuildBuilding(Building.BuildingType, target.TopLeft, Quaternion.Identity, Building.Player);
					return;
				}
			}
		}
	}
}
