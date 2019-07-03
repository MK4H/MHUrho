﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.Helpers.Extensions;
using MHUrho.Input.MandK;
using MHUrho.UserInterface.MandK;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	public class TreeType : BaseBuildingTypePlugin {

		class TreeBuilder : DirectionlessBuilder
		{

			readonly TreeType type;

			readonly UIElement uiElem;
			readonly Slider sizeSlider;


			public TreeBuilder(GameController input, GameUI ui, CameraMover camera, TreeType type)
				: base(input, ui, camera, type.myType)
			{
				this.type = type;
				InitUI(ui, out uiElem, out sizeSlider);
			}

			public override void OnMouseDown(MouseButtonDownEventArgs e)
			{
				if (e.Button != (int)MouseButton.Left)
				{
					return;
				}

				ITile tile = Input.GetTileUnderCursor();
				if (tile == null)
				{
					return;
				}

				IntRect rect = GetBuildingRectangle(tile, BuildingType);
				if (BuildingType.CanBuild(rect, Input.Player, Level))
				{
					IBuilding building = Level.BuildBuilding(BuildingType, rect.TopLeft(), Quaternion.Identity, Input.Player);
					Tree tree = (Tree)building.BuildingPlugin;
					tree.SetSize((sizeSlider.Value / sizeSlider.Range) + startSize);
				}
			}

			static void InitUI(GameUI ui, out UIElement uiElem, out Slider sizeSlider)
			{
				if ((uiElem = ui.CustomWindow.GetChild("TreeUI")) == null)
				{
					ui.CustomWindow.LoadLayout("Assets/UI/TreeCWUI.xml");
					uiElem = ui.CustomWindow.GetChild("TreeUI");
				}

				sizeSlider = (Slider)uiElem.GetChild("sizeSlider", true);
				sizeSlider.Range = 10;

				uiElem.Visible = false;
			}

			public override void Dispose()
			{
				uiElem.Dispose();
				sizeSlider.Dispose();
			}

			public override void Enable()
			{
				base.Enable();

				uiElem.Visible = true;
			}

			public override void Disable()
			{
				base.Disable();

				uiElem.Visible = false;
			}

		}


		public static string TypeName =  "Tree1";
		public static int TypeID =  2;

		public override string Name => TypeName;
		public override int ID => TypeID;

		/// <summary>
		/// Contains tiles the tree grows in and the time it takes to grow to full
		/// </summary>
		public IReadOnlyDictionary<TileType, double> TileGrowth => tileGrowth;

		public Vector3 BaseScale { get; private set; }

		static Random sizeRandom = new Random();

		const string growsInElem = "growsIn";
		const string baseScaleElem = "baseScale";
		const string typeAttr = "type";
		const string timeAttr = "time";

		const float startSize = 0.05f;

		BuildingType myType;

		readonly Dictionary<TileType, double> tileGrowth;

		public TreeType()
		{
			tileGrowth = new Dictionary<TileType, double>();
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			float finalSize = (float)(1 + (sizeRandom.NextDouble() - 0.5) / 10);
			ITile tile = level.Map.GetContainingTile(building.Center);
			if (TileGrowth.TryGetValue(tile.Type, out double time)) {
				return new Tree(level, building, (float)time, startSize, finalSize, this);
			}
			return null;
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding building)
		{
			return new Tree(level, building, this);
		}

		public override bool CanBuild(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, IPlayer owner, ILevelManager level)
		{
			//Tree is just 1 tile big
			if (topLeftTileIndex != bottomRightTileIndex) {
				throw new ArgumentException("Wrong size of rectangle provided");
			}

			ITile tile = level.Map.GetTileByTopLeftCorner(topLeftTileIndex);
			//Check if the owner is neutral player, the tile is free and the tree grows on the given tile type
			return owner == level.NeutralPlayer && tile.Building == null && tileGrowth.ContainsKey(tile.Type);
		}

		public override Builder GetBuilder(GameController input, GameUI ui, CameraMover camera)
		{
			return new TreeBuilder(input, ui, camera, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			myType = package.GetBuildingType(ID);

			IEnumerable<XElement> growsIn = extensionElement.Elements(package.PackageManager.GetQualifiedXName(growsInElem));
			foreach (var element in growsIn)
			{
				TileType tileType = GetTileType(element, package);
				double? growthRate = GetGrowthTime(element);
				if (tileType == null || growthRate == null)
				{
					continue;
				}

				tileGrowth.Add(tileType, growthRate.Value);
			}

			BaseScale = extensionElement.Element(package.PackageManager.GetQualifiedXName(baseScaleElem)).GetVector3();
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
			catch (ArgumentOutOfRangeException) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"Invalid element in {Name} building type's extension element: {growsInElem} element invalid value in {typeAttr} attribute.");
				return null;
			}
		}

		double? GetGrowthTime(XElement growsIn)
		{
			string timetxt = growsIn.Attribute(timeAttr)?.Value;

			if (timetxt == null) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"Invalid element in {Name} building type's extension element: {growsInElem} element missing {timeAttr} attribute.");
				return null;
			}

			if (double.TryParse(timetxt, out double time)) {
				return time;
			}
			Urho.IO.Log.Write(LogLevel.Warning,
							$"Invalid element in {Name} building type's extension element: {growsInElem} element invalid value in {typeAttr} attribute.");
			return null;
		}
	}

	public class Tree : BuildingInstancePlugin {

		readonly TreeType type;

		static readonly Random spreadRNG = new Random();

		double spreadProbability = 0.01;
		double spreadTimeout = 5;
		IntVector2 spreadSize = new IntVector2(100, 100);
		int spreadTries = 10;

		double curSpreadTimeout;



		public float FinalSize { get; set; }
		float changePerSecond;
		float currentSize;

		/// <summary>
		/// Size of step in seconds.
		/// </summary>
		const float stepSize = 1;

		float currentStepTimeout = stepSize;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="level"></param>
		/// <param name="building"></param>
		/// <param name="growthTime">Time after which the tree grows into its full size</param>
		/// <param name="startSize">Start size of the tree as a multiplier of the standard size in prefab.</param>
		/// <param name="finalSize">Final size of the tree as a multiplier of the standard size in prefab.</param>
		/// <param name="type"></param>
		public Tree(ILevelManager level, IBuilding building, float growthTime, float startSize, float finalSize,TreeType type)
			: base(level, building)
		{
			this.type = type;
			this.curSpreadTimeout = spreadTimeout;

			this.FinalSize = finalSize;
			this.changePerSecond = (finalSize - startSize) / growthTime;
			this.currentSize = startSize;
			building.Node.ScaleNode(startSize);
		}

		public Tree(ILevelManager level, IBuilding building, TreeType type)
			: base(level, building)
		{

		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			writer.StoreNext(FinalSize);
			writer.StoreNext(changePerSecond);
			writer.StoreNext(currentSize);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var reader = pluginData.GetReaderForWrappedSequentialData();
			reader.GetNext(out float finalSize);
			FinalSize = finalSize;
			reader.GetNext(out changePerSecond);
			reader.GetNext(out currentSize);

			SetSize(currentSize);
		}

		public override void Dispose()
		{
			
		}


		public override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);

			if (!Level.EditorMode) {
				Spread(timeStep);
				Grow(timeStep);
			}
		}

		/// <summary>
		/// Sets current size of the tree, if greater than <see cref="FinalSize"/>, sets final size too. 
		/// </summary>
		/// <param name="newSize">New current size of the tree.</param>
		public void SetSize(float newSize)
		{
			if (FinalSize < newSize) {
				FinalSize = newSize;
			}

			currentSize = newSize;
			Building.Node.Scale = type.BaseScale * currentSize;
		}

		public void Chomp()
		{
			currentSize -= 0.1f;
			if (currentSize < 0.1f) {
				Building.RemoveFromLevel();
			}
		}

		void Spread(float timeStep)
		{
			curSpreadTimeout -= timeStep;
			if (curSpreadTimeout > 0) {
				return;
			}

			curSpreadTimeout = spreadTimeout;

			if (spreadRNG.NextDouble() > spreadProbability) {
				return;
			}

			for (int i = 0; i < spreadTries; i++)
			{
				IntVector2 offset = new IntVector2(spreadRNG.Next(-(spreadSize.X / 2), spreadSize.X / 2),
													spreadRNG.Next(-(spreadSize.Y / 2), spreadSize.Y / 2));

				ITile target = Level.Map.GetTileByTopLeftCorner(Building.TopLeft + offset);

				if (target != null && target.Building == null && type.TileGrowth.ContainsKey(target.Type))
				{
					Level.BuildBuilding(Building.BuildingType, target.TopLeft, Quaternion.Identity, Building.Player);
					return;
				}
			}

		}

		void Grow(float timeStep)
		{
			if (currentSize >= FinalSize) {
				return;
			}

			currentStepTimeout -= timeStep;
			if (currentStepTimeout > 0) {
				return;
			}

			currentStepTimeout = stepSize;

			float newSize = currentSize + changePerSecond * stepSize;
			SetSize(newSize);
		} 
	}

	
}
