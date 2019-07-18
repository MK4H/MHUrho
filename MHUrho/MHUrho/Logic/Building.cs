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
	/// <summary>
	/// Represents a building in the game.
	/// </summary>
	class Building : Entity, IBuilding {

		/// <summary>
		/// Implements storing and loading buildings.
		/// </summary>
		class Loader : IBuildingLoader {

			/// <summary>
			/// The building being loaded
			/// </summary>
			public IBuilding Building => loadingBuilding;

			/// <summary>
			/// The building being loaded.
			/// </summary>
			Building loadingBuilding;

			/// <summary>
			/// The level the building is being loaded into.
			/// </summary>
			readonly LevelManager level;
		
			/// <summary>
			/// Used to store the reference to storedBuilding between Load and ConnectReferences calls
			/// </summary>
			readonly StBuilding storedBuilding;

			/// <summary>
			/// The type of the loading building.
			/// </summary>
			readonly BuildingType type;

			/// <summary>
			/// Loaders of the default components saved on the building.
			/// </summary>
			List<DefaultComponentLoader> componentLoaders;

			/// <summary>
			/// Creates a loader for the <paramref name="storedBuilding"/> that loads it into
			/// the <paramref name="level"/>.
			/// </summary>
			/// <param name="level">The level the building is being loaded to.</param>
			/// <param name="storedBuilding">The saved building to load.</param>
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
			/// Builds the building at <paramref name="topLeftCorner"/> if it is possible.
			/// </summary>
			/// <param name="id">The identifier of the new building.</param>
			/// <param name="topLeftCorner">Position of the top left corner of the building in the game world.</param>
			/// <param name="rotation">Initial rotation of the building after it is built.</param>
			/// <param name="type">Type of the building.</param>
			/// <param name="player">Owner of the building.</param>
			/// <param name="level">The level the building is being built in.</param>
			/// <returns>Null if it is not possible to build the building there, new Building if it is possible</returns>
			/// <exception cref="CreationException">Thrown when there was an exception during building creation, like missing assets or error in the plugin.</exception>
			public static Building CreateNew(int id,
											IntVector2 topLeftCorner,
											Quaternion rotation,
											BuildingType type,
											IPlayer player,
											ILevelManager level) {
				if (!type.CanBuild(topLeftCorner, player, level)) {
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

				Building newBuilding = null;
				try {
					new BuildingComponentSetup().SetupComponentsOnNode(buildingNode, level);

					newBuilding = new Building(id, level, rect, type, player);
					buildingNode.AddComponent(newBuilding);

					newBuilding.BuildingPlugin = newBuilding.BuildingType.GetNewInstancePlugin(newBuilding, level);
					return newBuilding;
				}
				catch (Exception e) {
					if (newBuilding != null) {
						newBuilding.RemoveFromLevel();
					}
					else {
						buildingNode.Remove();
					}					
					string message = $"There was an Exception while creating a new building: {e.Message}";
					Urho.IO.Log.Write(LogLevel.Error, message);
					throw new CreationException(message, e);
				}
				
			}

			/// <summary>
			/// Stores the <paramref name="building"/> in a <see cref="StBuilding"/> instance for serialization.
			/// </summary>
			/// <param name="building">The building to store.</param>
			/// <returns>Building stored in <see cref="StBuilding"/> ready for serialization.</returns>
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

			/// <summary>
			/// Executes the first step of loading process.
			/// </summary>
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

				loadingBuilding = new Building(storedBuilding.Id, level, rect, type);
				buildingNode.AddComponent(loadingBuilding);
				

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

			/// <summary>
			/// Executes the second step of loading process, connecting stored references to
			/// other game entities.
			/// </summary>
			public void ConnectReferences() {
				loadingBuilding.Player = level.GetPlayer(storedBuilding.PlayerID);

				foreach (var componentLoader in componentLoaders) {
					componentLoader.ConnectReferences();
				}

				loadingBuilding.BuildingPlugin.LoadState(new PluginDataWrapper(storedBuilding.UserPlugin, level));
			}

			/// <summary>
			/// Cleans up the data created only for loading.
			/// </summary>
			public void FinishLoading() {
				foreach (var componentLoader in componentLoaders) {
					componentLoader.FinishLoading();
				}
			}

		}

		/// <inheritdoc/>
		public IntRect Rectangle { get; private set; }
		/// <inheritdoc/>
		public IntVector2 TopLeft => Rectangle.TopLeft();
		/// <inheritdoc/>
		public IntVector2 TopRight => Rectangle.TopRight();
		/// <inheritdoc/>
		public IntVector2 BottomLeft => Rectangle.BottomLeft();
		/// <inheritdoc/>
		public IntVector2 BottomRight => Rectangle.BottomRight();

		/// <inheritdoc/>
		public override Vector3 Position {
			get => Node.Position;
			protected set => Node.Position = value;
		}

		/// <inheritdoc/>
		public override InstancePlugin Plugin => BuildingPlugin;

		/// <inheritdoc/>
		public Vector3 Center => Node.Position;

		/// <inheritdoc/>
		public BuildingType BuildingType { get; private set; }

		/// <inheritdoc/>
		public override IEntityType Type => BuildingType;

		/// <inheritdoc/>
		public IntVector2 Size => new IntVector2(Rectangle.Width(), Rectangle.Height());

		/// <inheritdoc/>
		public override Vector3 Forward => Node.WorldDirection;

		/// <inheritdoc/>
		public override Vector3 Backward => -Forward;

		/// <inheritdoc/>
		public override Vector3 Right => Node.WorldRight;

		/// <inheritdoc/>
		public override Vector3 Left => -Right;

		/// <inheritdoc/>
		public override Vector3 Up => Node.WorldUp;

		/// <inheritdoc/>
		public override Vector3 Down => -Up;

		/// <inheritdoc/>
		public BuildingInstancePlugin BuildingPlugin { get; private set; }

		/// <inheritdoc/>
		public IReadOnlyList<ITile> Tiles => tiles;

		/// <summary>
		/// Tiles that are covered by this building.
		/// </summary>
		readonly ITile[] tiles;

		/// <summary>
		/// Creates new building in the game.
		/// </summary>
		/// <param name="id">Identifier of the new building.</param>
		/// <param name="level">The level to create the building in.</param>
		/// <param name="rectangle">Rectangle of the part of the map taken by this building.</param>
		/// <param name="type">Type of this building.</param>
		/// <param name="player">Owner of the new building.</param>
		protected Building(int id, ILevelManager level, IntRect rectangle, BuildingType type, IPlayer player) 
			:base(id, level)
		{

			this.BuildingType = type;
			this.Player = player;
			this.Rectangle = rectangle;
			this.tiles = AllocTiles(true);
		}


		/// <summary>
		/// Constructor for loading instance.
		/// </summary>
		/// <param name="id">Identifier of the loaded building.</param>
		/// <param name="level">The level the building is loading into.</param>
		/// <param name="rectangle">Rectangle of the map taken by this building.</param>
		/// <param name="type">Type of this building.</param>
		protected Building(int id, ILevelManager level, IntRect rectangle, BuildingType type) 
			:base(id, level)
		{
			this.BuildingType = type;
			this.Rectangle = rectangle;
			//Tiles remember which buildings were on them.
			this.tiles = AllocTiles(false);
		}

		/// <summary>
		/// Creates a loader to load the building stored in <paramref name="storedBuilding"/> to the <paramref name="level"/>.
		/// </summary>
		/// <param name="level">The level to load the building into.</param>
		/// <param name="storedBuilding">The stored building.</param>
		/// <returns>Loader to load the building.</returns>
		public static IBuildingLoader GetLoader(LevelManager level, StBuilding storedBuilding)
		{
			return new Loader(level, storedBuilding);
		}

		/// <summary>
		/// Creates new building in the <paramref name="level"/>.
		/// </summary>
		/// <param name="id">The identifier of the new building.</param>
		/// <param name="topLeftCorner">The position of the top left corner of the building.</param>
		/// <param name="rotation">Rotation of the building after it is built.</param>
		/// <param name="type">The type of the building.</param>
		/// <param name="player">The owner of the building.</param>
		/// <param name="level">The level to create the building in.</param>
		/// <returns>New building.</returns>
		/// <exception cref="CreationException">Thrown when there was an unexpected exception during the creation of the building.</exception>
		public static Building CreateNew(int id,
										IntVector2 topLeftCorner,
										Quaternion rotation,
										BuildingType type,
										IPlayer player,
										ILevelManager level)
		{
			return Loader.CreateNew(id, topLeftCorner, rotation, type, player, level);
		}

		/// <summary>
		/// Stores the building in a <see cref="StBuilding"/> instance for serialization.
		/// </summary>
		/// <returns>Building stored in <see cref="StBuilding"/></returns>
		public StBuilding Save() {
			return Loader.Save(this);
		}

		/// <summary>
		/// Implementation of the visitor design pattern.
		/// </summary>
		/// <param name="visitor">The visiting visitor.</param>
		public override void Accept(IEntityVisitor visitor) {
			visitor.Visit(this);
		}

		/// <summary>
		/// Implementation of the generic visitor design pattern.
		/// </summary>
		/// <param name="visitor">The visiting visitor.</param>
		public override T Accept<T>(IEntityVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}

		/// <summary>
		/// Removes the building from the level.
		/// </summary>
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
			if (!IsDeleted) {
				Node.Remove();
				base.Dispose();
			}
		
		}

		/// <summary>
		/// Inform the building that it was hit by <paramref name="other"/> entity,
		/// provide <paramref name="userData"/> for plugin.
		/// </summary>
		/// <param name="other">The entity that hit this building.</param>
		/// <param name="userData">User data for plugin.</param>
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

		///<inheritdoc/>
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

		///<inheritdoc/>
		public bool CanChangeTileHeight(int x, int y)
		{
			try
			{
				return BuildingPlugin.CanChangeTileHeight(x,y);
			}
			catch (Exception e)
			{
				//Log and ignore
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Building  plugin call {nameof(BuildingPlugin.CanChangeTileHeight)} failed with Exception: {e.Message}");
				return false;
			}
		}

		///<inheritdoc/>
		public void TileHeightChanged(ITile tile)
		{
			try
			{
				BuildingPlugin.TileHeightChanged(tile);
			}
			catch (Exception e)
			{
				//NOTE: Maybe add cap to prevent message flood
				Urho.IO.Log.Write(LogLevel.Error, $"Building  plugin call {nameof(BuildingPlugin.TileHeightChanged)} failed with Exception: {e.Message}");
			}
		}

		///<inheritdoc/>
		public void ChangeHeight(float newHeight)
		{
			Node.Position = Node.Position.WithY(newHeight);
			foreach (var tile in Tiles) {
				foreach (var unit in tile.Units) {
					unit.TileHeightChanged(tile);
				}
			}
			SignalPositionChanged();
		}

		///<inheritdoc/>
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

		
		/// <summary>
		/// Removes the building from level.
		/// </summary>
		void IDisposable.Dispose()
		{
			RemoveFromLevel();
		}

		/// <summary>
		/// Handles the scene update.
		/// </summary>
		/// <param name="timeStep">The time elapsed since the last update.</param>
		protected override void OnUpdate(float timeStep)
		{
			if (IsDeleted || !EnabledEffective || !Level.LevelNode.Enabled) {
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


		ITile[] AllocTiles(bool registerBuilding) {
			var newTiles = new ITile[BuildingType.Size.X * BuildingType.Size.Y];

			for (int y = 0; y < BuildingType.Size.Y; y++) {
				for (int x = 0; x < BuildingType.Size.X; x++) {
					var tile = Level.Map.GetTileByTopLeftCorner(TopLeft.X + x, TopLeft.Y + y);
					newTiles[GetTileIndex(x, y)] = tile;
					if (registerBuilding) {
						tile.SetBuilding(this);
					}	
				}
			}

			return newTiles;
		}
	}
}