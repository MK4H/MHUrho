using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Helpers.Extensions;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic
{
	class Tile : ITile {
		/// <summary>
		/// Width of the texture defining the appearance of the tile.
		/// </summary>
		public const int ImageWidth = 100;

		/// <summary>
		/// Height of the texture defining the appearance of the tile.
		/// </summary>
		public const int ImageHeight = 100;

		/// <summary>
		/// Loader that loads the stored tile.
		/// </summary>
		class Loader : ITileLoader {

			/// <inheritdoc />
			ITile ITileLoader.Tile => Tile;

			/// <summary>
			/// Loading tile.
			/// </summary>
			public Tile Tile { get; private set; }

			/// <summary>
			/// The level the tile is loading into.
			/// </summary>
			readonly LevelManager level;

			/// <summary>
			/// The map the tile is part of.
			/// </summary>
			readonly Map map;

			/// <summary>
			/// Holds the data of the tile between the steps of loading
			/// </summary>
			readonly StTile storedTile;

			/// <summary>
			/// Creates a loader that loads a tile from data stored in <paramref name="storedTile"/>.
			/// </summary>
			/// <param name="level">The level the tile is part of.</param>
			/// <param name="map">The map the tile is part of.</param>
			/// <param name="storedTile">Stored data of the tile.</param>
			public Loader(LevelManager level, Map map, StTile storedTile)
			{
				this.level = level;
				this.map = map;
				this.storedTile = storedTile;
			}

			/// <summary>
			/// Stores the <paramref name="tile"/> in an instance of <see cref="StTile"/> for serialization.
			/// </summary>
			/// <param name="tile">The tile to save.</param>
			/// <returns>Data of the tile stored in an instance of <see cref="StTile"/>.</returns>
			public static StTile Save(Tile tile)
			{

				var storedTile = new StTile
								{
									TopLeftPosition = tile.TopLeft.ToStIntVector2(),
									Height = tile.TopLeftHeight,
									TileTypeID = tile.Type.ID,
									BuildingID = tile.Building?.ID ?? 0
								};

				foreach (var unit in tile.Units) {
					storedTile.UnitIDs.Add(unit.ID);
				}


				return storedTile;
				
			}

			/// <summary>
			/// Loads everything apart from thigs referenced by ID
			/// 
			/// After everything had it StartLoading called, call ConnectReferences on everything
			/// </summary>
			/// <param name="storedTile">Image of the tile</param>
			/// <param name="map">Map this tile is in</param>
			/// <returns>Partially initialized tile</returns>
			public void StartLoading()
			{
				Tile = new Tile(storedTile, map);
			}

			/// <summary>
			/// Continues loading by connecting references
			/// </summary>
			public void ConnectReferences()
			{
				Tile.Type = level.Package.GetTileType(storedTile.TileTypeID);

				if (storedTile.UnitIDs.Count != 0) {
					Tile.units = new List<IUnit>();
				}

				foreach (var unit in storedTile.UnitIDs) {
					Tile.units.Add(level.GetUnit(unit));
				}


				if (storedTile.BuildingID != 0) {
					Tile.Building = level.GetBuilding(storedTile.BuildingID);
				} 

			}

			/// <summary>
			/// Cleans up.
			/// </summary>
			public void FinishLoading()
			{

			}
		}


		/// <summary>
		/// Units inside the tile
		/// </summary>
		public IReadOnlyList<IUnit> Units => units ?? new List<IUnit>();

		//We enforce one building per tile to make GetHeightAt easier
		public IBuilding Building { get; private set; }


		/// <inheritdoc />
		public TileType Type { get; internal set; }

		/// <inheritdoc />
		public IntRect MapArea { get; }

		/// <inheritdoc />
		public IntVector2 MapLocation => TopLeft;

		/// <inheritdoc />
		public IntVector2 TopLeft => new IntVector2(MapArea.Left,MapArea.Top);

		/// <inheritdoc />
		public IntVector2 TopRight => new IntVector2(MapArea.Right, MapArea.Top);

		/// <inheritdoc />
		public IntVector2 BottomLeft => new IntVector2(MapArea.Left, MapArea.Bottom);

		/// <inheritdoc />
		public IntVector2 BottomRight => new IntVector2(MapArea.Right, MapArea.Bottom);

		/// <inheritdoc />
		public Vector2 Center => new Vector2(TopLeft.X + 0.5f, TopLeft.Y + 0.5f);

		/// <inheritdoc />
		public Vector3 Center3 => new Vector3(Center.X, Map.GetTerrainHeightAt(Center), Center.Y);

		/// <inheritdoc />
		public Vector3 TopLeft3 => new Vector3(MapArea.Left, Map.GetTerrainHeightAt(MapArea.Left, MapArea.Top), MapArea.Top);

		/// <inheritdoc />
		public Vector3 TopRight3 => new Vector3(MapArea.Right, Map.GetTerrainHeightAt(MapArea.Right, MapArea.Top), MapArea.Top);

		/// <inheritdoc />
		public Vector3 BottomLeft3 => new Vector3(MapArea.Left, Map.GetTerrainHeightAt(MapArea.Left, MapArea.Bottom), MapArea.Bottom);

		/// <inheritdoc />
		public Vector3 BottomRight3 => new Vector3(MapArea.Right, Map.GetTerrainHeightAt(MapArea.Right, MapArea.Bottom), MapArea.Bottom);

		/// <inheritdoc />
		public float TopLeftHeight { get; private set; }

		/// <inheritdoc />
		public float TopRightHeight => Map.GetTerrainHeightAt(MapArea.Left + 1, MapArea.Top);

		/// <inheritdoc />
		public float BottomLeftHeight => Map.GetTerrainHeightAt(MapArea.Left, MapArea.Top + 1);

		/// <inheritdoc />
		public float BottomRightHeight => Map.GetTerrainHeightAt(MapArea.Left + 1, MapArea.Top + 1);

		/// <inheritdoc />
		public IMap Map { get; private set; }

		/// <summary>
		/// List of units present on this tile, is NULL if there are no units on this tile
		/// </summary>
		List<IUnit> units;
		
		/// <summary>
		/// Creates an instance of a tile based on the data stored in <paramref name="storedTile"/>.
		/// </summary>
		/// <param name="storedTile">The stored data of the tile.</param>
		/// <param name="map">The map the tile will be part of.</param>
		protected Tile(StTile storedTile, IMap map) {
			this.MapArea = new IntRect(storedTile.TopLeftPosition.X, 
									   storedTile.TopLeftPosition.Y, 
									   storedTile.TopLeftPosition.X + 1, 
									   storedTile.TopLeftPosition.Y + 1);
			this.TopLeftHeight = storedTile.Height;
			this.Map = map;
			units = null;
		}

		/// <summary>
		/// Creates new instance of tile.
		/// </summary>
		/// <param name="x">X coordinate of the tile position.</param>
		/// <param name="y">Z coordinate of the tile position.</param>
		/// <param name="tileType">Type of the tile.</param>
		/// <param name="map">The map this tile belongs to.</param>
		public Tile(int x, int y, TileType tileType, Map map) {
			MapArea = new IntRect(x, y, x + 1, y + 1);
			units = null;
			this.Type = tileType;
			this.TopLeftHeight = 0;
			this.Map = map;
		}

		/// <summary>
		/// Returns a loader that will load the tile from the <paramref name="storedTile"/>.
		/// </summary>
		/// <param name="level">The level the tile is part of.</param>
		/// <param name="map">The map the tile is part of.</param>
		/// <param name="storedTile">The stored data of the tile.</param>
		/// <returns>The loader that will load the tile from the stored data.</returns>
		public static ITileLoader GetLoader(LevelManager level, Map map, StTile storedTile)
		{
			return new Loader(level, map, storedTile);
		}

		public override bool Equals(object obj)
		{
			return object.ReferenceEquals(this, obj);
		}

		public override int GetHashCode()
		{
			return MapLocation.GetHashCode();
		}

		/// <inheritdoc />
		public StTile Save()
		{
			return Loader.Save(this);
		}

		/// <inheritdoc />
		public void AddUnit(IUnit unit)
		{
			//Lazy allocation
			if (units == null) {
				units = new List<IUnit>();
			}
			units.Add(unit);
		}

		/// <inheritdoc />
		public bool RemoveUnit(IUnit unit)
		{
			if (units == null || !units.Remove(unit)) {
				return false;
			}

			if (units.Count == 0) {
				units = null;
			}
			return true;
		}

		/// <inheritdoc />
		public void SetBuilding(IBuilding building) {
			if (Building != null && building != Building) {
				throw new InvalidOperationException("There is a building already on this tile");
			}
			//Enumerate on a copy because some units may be destroyed during the enumeration
			foreach (var unit in units?.ToArray() ?? Enumerable.Empty<IUnit>())
			{
				unit.BuildingBuilt(building, this);
			}

			Building = building;
		}

		/// <inheritdoc />
		public void RemoveBuilding(IBuilding building) {
			if (Building != building) {
				throw new ArgumentException("Removing building that is not on this tile");
			}
			//Enumerate on a copy because some units may be destroyed during the enumeration
			foreach (var unit in units?.ToArray() ?? Enumerable.Empty<IUnit>()) {
				unit.BuildingDestroyed(building, this);
			}

			Building = null;
		}

		/// <inheritdoc />
		public void ChangeType(TileType newType) {
			Type = newType;
		}

		/// <inheritdoc />
		public void ChangeTopLeftHeight(float heightDelta) {
			TopLeftHeight += heightDelta;
		}

		/// <inheritdoc />

		public void SetTopLeftHeight(float newHeight) {
			TopLeftHeight = newHeight;
		}

		/// <inheritdoc />
		public void CornerHeightChange()
		{			
			//Enumerate on a copy because some units may be destroyed during the enumeration
			foreach (var unit in units?.ToArray() ?? Enumerable.Empty<IUnit>()) {
				//Moves unit above terrain
				float terrainHeight = Map.GetTerrainHeightAt(unit.XZPosition);
				if (unit.Position.Y < terrainHeight) {
					unit.SetHeight(terrainHeight);
				}
				unit.TileHeightChanged(this);
			}

			Building?.TileHeightChanged(this);
		}

		/// <inheritdoc />
		public float GetHeightAt(float x, float y)
		{
			return Building?.GetHeightAt(x, y) ?? Map.GetTerrainHeightAt(x, y);
		}

		/// <inheritdoc />
		public float GetHeightAt(Vector2 position)
		{
			return GetHeightAt(position.X, position.Y);
		}

		/// <summary>
		/// List of differences of neighbor coordinates.
		/// For easier implementation of <see cref="GetNeighbours()"/>.
		/// </summary>
		static readonly IntVector2[] NeighborDiff =
		{
			new IntVector2(-1, -1),
			new IntVector2(0, -1),
			new IntVector2(1, -1),
			new IntVector2(-1, 0),
			new IntVector2(-1, 1),
			new IntVector2(0, 1),
			new IntVector2(1, 1),
			new IntVector2(1, 0)
		};


		/// <inheritdoc />
		public IEnumerable<ITile> GetNeighbours()
		{
			foreach (var diff in NeighborDiff) {
				ITile tile = Map.GetTileByMapLocation(MapLocation + diff);
				if (tile != null) {
					yield return tile;
				}
			}
		}

		/// <inheritdoc />
		public bool CanChangeCornerHeight(int x, int y)
		{
			return Building?.CanChangeTileHeight(x, y) ?? true;
		}
	} 
}