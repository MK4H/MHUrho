using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Control;
using Urho;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using MHUrho.WorldMap;
using Urho.Physics;

namespace MHUrho.Logic
{
	class Building : Entity, IBuilding {
		class Loader : IBuildingLoader {

			public IBuilding Building => loadingBuilding;

			Building loadingBuilding;

			readonly LevelManager level;
		
			/// <summary>
			/// Used to store the reference to storedBuilding between Load and ConnectReferences calls
			/// </summary>
			readonly StBuilding storedBuilding;
			readonly BuildingType type;

			List<DefaultComponentLoader> componentLoaders;

			public Loader(LevelManager level,
						StBuilding storedBuilding)
			{
				this.level = level;
				this.storedBuilding = storedBuilding;
				componentLoaders = new List<DefaultComponentLoader>();

				type = level.Package.GetBuildingType(storedBuilding.TypeID);
				if (type == null) {
					throw new ArgumentException("Type of this building was not loaded");
				}
			}

			/// <summary>
			/// Builds the building at <paramref name="topLeftCorner"/> if its possible
			/// </summary>
			/// <param name="topLeftCorner"></param>
			/// <param name="type"></param>
			/// <param name="buildingNode"></param>
			/// <param name="level"></param>
			/// <returns>Null if it is not possible to build the building there, new Building if it is possible</returns>
			public static Building CreateNew(int id,
											IntVector2 topLeftCorner,
											Quaternion rotation,
											BuildingType type,
											IPlayer player,
											ILevelManager level) {
				if (!type.CanBuild(type.GetBuildingTilesRectangle(topLeftCorner), player, level)) {
					return null;
				}

				var rect = new IntRect(topLeftCorner.X,
										topLeftCorner.Y,
										topLeftCorner.X + type.Size.X,
										topLeftCorner.Y + type.Size.Y);
				Vector2 center = rect.Center();
				Vector3 position = new Vector3(center.X,
												level.Map.GetTerrainHeightAt(center),
												center.Y);


				Node buildingNode;
				try {
					buildingNode = type.Assets.Instantiate(level, position, rotation);
				}
				catch (Exception e)
				{
					string message = $"There was an Exception while creating a new building: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new CreationException(message, e);
				}


				try {
					new BuildingComponentSetup().SetupComponentsOnNode(buildingNode, level);

					var newBuilding = new Building(id, level, rect, type, player);
					buildingNode.AddComponent(newBuilding);

					newBuilding.BuildingPlugin = newBuilding.BuildingType.GetNewInstancePlugin(newBuilding, level);
					return newBuilding;
				}
				catch (Exception e) {
					buildingNode.Remove();
					buildingNode.Dispose();
					string message = $"There was an Exception while creating a new building: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new CreationException(message, e);
				}
				
			}

			public static StBuilding Save(Building building) {
				var stBuilding = new StBuilding {
													Id = building.ID,
													TypeID = building.BuildingType.ID,
													PlayerID = building.Player.ID,
													Location = building.TopLeft.ToStIntVector2(),
													Rotation = building.Node.Rotation.ToStQuaternion(),
													UserPlugin = new PluginData()
												};
				try {
					building.BuildingPlugin.SaveState(new PluginDataWrapper(stBuilding.UserPlugin, building.Level));
				}
				catch (Exception e)
				{
					string message = $"Saving building plugin failed with Exception: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new SavingException(message, e);
				}

				foreach (var component in building.Node.Components) {
					var defaultComponent = component as DefaultComponent;
					if (defaultComponent != null) {
						stBuilding.DefaultComponents.Add(defaultComponent.SaveState());
					}
				}

				return stBuilding;
			}

			public void StartLoading() {
				if (type.ID != storedBuilding.TypeID) {
					throw new ArgumentException("Provided type is not the type of the stored building", nameof(type));
				}

				IntVector2 topLeftCorner = storedBuilding.Location.ToIntVector2();
				var rect = new IntRect(topLeftCorner.X,
										topLeftCorner.Y,
										topLeftCorner.X + type.Size.X,
										topLeftCorner.Y + type.Size.Y);
				Vector2 center = rect.Center();		
				Vector3 position = new Vector3(center.X,
												level.Map.GetTerrainHeightAt(center),
												center.Y);
				Quaternion rotation = storedBuilding.Rotation.ToQuaternion();

				
				Node buildingNode = type.Assets.Instantiate(level, position, rotation);
				new BuildingComponentSetup().SetupComponentsOnNode(buildingNode, level);

				buildingNode.AddComponent(loadingBuilding);
				
				loadingBuilding = new Building(storedBuilding.Id, level, rect, type);
				loadingBuilding.BuildingPlugin = type.GetInstancePluginForLoading(loadingBuilding, level);

				foreach (var defaultComponent in storedBuilding.DefaultComponents)
				{
					var componentLoader =
						level.DefaultComponentFactory
							.StartLoadingComponent(defaultComponent,
													level,
													loadingBuilding.BuildingPlugin);

					componentLoaders.Add(componentLoader);
					loadingBuilding.AddComponent(componentLoader.Component);
				}

				
			}


			public void ConnectReferences() {
				loadingBuilding.Player = level.GetPlayer(storedBuilding.PlayerID);

				foreach (var componentLoader in componentLoaders) {
					componentLoader.ConnectReferences();
				}

				loadingBuilding.BuildingPlugin.LoadState(new PluginDataWrapper(storedBuilding.UserPlugin, level));
			}

			public void FinishLoading() {
				foreach (var componentLoader in componentLoaders) {
					componentLoader.FinishLoading();
				}
			}

		}

		public IntRect Rectangle { get; private set; }

		public IntVector2 TopLeft => Rectangle.TopLeft();

		public IntVector2 TopRight => Rectangle.TopRight();

		public IntVector2 BottomLeft => Rectangle.BottomLeft();

		public IntVector2 BottomRight => Rectangle.BottomRight();


		public override Vector3 Position {
			get => Node.Position;
			protected set => Node.Position = value;
		}

		public override InstancePlugin Plugin => BuildingPlugin;

		public Vector3 Center => Node.Position;

		public BuildingType BuildingType { get; private set; }

		public override IEntityType Type => BuildingType;

		public IntVector2 Size => new IntVector2(Rectangle.Width(), Rectangle.Height());

		public override Vector3 Forward => Node.WorldDirection;

		public override Vector3 Backward => -Forward;

		public override Vector3 Right => Node.WorldRight;

		public override Vector3 Left => -Right;

		public override Vector3 Up => Node.WorldUp;

		public override Vector3 Down => -Up;

		public BuildingInstancePlugin BuildingPlugin { get; private set; }

		public IReadOnlyList<ITile> Tiles => tiles;

		readonly ITile[] tiles;

		protected Building(int id, ILevelManager level, IntRect rectangle, BuildingType type, IPlayer player) 
			:base(id, level)
		{

			this.BuildingType = type;
			this.Player = player;
			this.Rectangle = rectangle;
			this.tiles = AllocTiles();
		}

		protected Building(int id, ILevelManager level, IntRect rectangle, BuildingType type) 
			:base(id, level)
		{
			this.BuildingType = type;
			this.Rectangle = rectangle;
			this.tiles = AllocTiles();
		}


		public static IBuildingLoader GetLoader(LevelManager level, StBuilding storedBuilding)
		{
			return new Loader(level, storedBuilding);
		}

		public static Building CreateNew(int id,
										IntVector2 topLeftCorner,
										Quaternion rotation,
										BuildingType type,
										IPlayer player,
										ILevelManager level)
		{
			return Loader.CreateNew(id, topLeftCorner, rotation, type, player, level);
		}

		public StBuilding Save() {
			return Loader.Save(this);
		}

		public override void Accept(IEntityVisitor visitor) {
			visitor.Visit(this);
		}

		public override T Accept<T>(IEntityVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}

		public override void RemoveFromLevel() {

			if (IsRemovedFromLevel) return;

			base.RemoveFromLevel();

			//We need removeFromLevel to work even when called during loading
			try {
				BuildingPlugin?.Dispose();
			}
			catch (Exception e) {
				//Log and ignore
				Urho.IO.Log.Write(LogLevel.Error, $"Building  plugin call {nameof(BuildingPlugin.Dispose)} failed with Exception: {e.Message}");
			}
			
			Level.RemoveBuilding(this);
			foreach (var tile in tiles) {
				tile.RemoveBuilding(this);
			}

			Player?.RemoveBuilding(this);
			Node.Remove();
			
			Dispose();
			
		}

		public override void HitBy(IEntity other, object userData)
		{
			try {
				BuildingPlugin.OnHit(other, userData);
			}
			catch (Exception e) {
				//Log and ignore
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Building  plugin call {nameof(BuildingPlugin.OnHit)} failed with Exception: {e.Message}");
			}
		}

		public float? GetHeightAt(float x, float y)
		{
			try {
				return BuildingPlugin.GetHeightAt(x, y);
			}
			catch (Exception e) {
				//Log and ignore
				Urho.IO.Log.Write(LogLevel.Error, $"Building  plugin call {nameof(BuildingPlugin.GetHeightAt)} failed with Exception: {e.Message}");
				return null;
			}

		}

		public IFormationController GetFormationController(Vector3 centerPosition)
		{
			try {
				return BuildingPlugin.GetFormationController(centerPosition);
			}
			catch (Exception e) {
				//Log and ignore
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Building  plugin call {nameof(BuildingPlugin.GetFormationController)} failed with Exception: {e.Message}");
				return null;
			}
		}

		

		void IDisposable.Dispose()
		{
			RemoveFromLevel();
		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
			if (!EnabledEffective || !Level.LevelNode.Enabled) {
				return;
			}

			try {
				BuildingPlugin.OnUpdate(timeStep);
			}
			catch (Exception e)
			{
				//Log and ignore
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Building  plugin call {nameof(BuildingPlugin.OnUpdate)} failed with Exception: {e.Message}");
			}
		}

		int GetTileIndex(int x, int y) {
			return x + y * BuildingType.Size.X;
		}

		int GetTileIndex(IntVector2 location) {
			return GetTileIndex(location.X, location.Y);
		}


		ITile[] AllocTiles() {
			var newTiles = new ITile[BuildingType.Size.X * BuildingType.Size.Y];

			for (int y = 0; y < BuildingType.Size.Y; y++) {
				for (int x = 0; x < BuildingType.Size.X; x++) {
					var tile = Level.Map.GetTileByTopLeftCorner(TopLeft.X + x, TopLeft.Y + y);
					newTiles[GetTileIndex(x, y)] = tile;
					tile.SetBuilding(this);
				}
			}

			return newTiles;
		}
	}
}