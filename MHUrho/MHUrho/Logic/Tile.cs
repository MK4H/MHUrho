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
		public const int ImageWidth = 100;
		public const int ImageHeight = 100;

		class Loader : ITileLoader {

			ITile ITileLoader.Tile => Tile;

			public Tile Tile { get; private set; }

			readonly LevelManager level;
			readonly Map map;

			/// <summary>
			/// Holds the image of the tile between the steps of loading
			/// </summary>
			readonly StTile storedTile;

			public Loader(LevelManager level, Map map, StTile storedTile)
			{
				this.level = level;
				this.map = map;
				this.storedTile = storedTile;
			}

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


		/// <summary>
		/// Tile type of this tile, only Map should set this
		/// </summary>
		public TileType Type { get; internal set; }
		
		/// <summary>
		/// The area in the map this tile represents
		/// </summary>
		public IntRect MapArea { get;  }


		public IntVector2 MapLocation => TopLeft;

		/// <summary>
		/// Location in the Map matrix
		/// </summary>
		public IntVector2 TopLeft => new IntVector2(MapArea.Left,MapArea.Top);

		public IntVector2 TopRight => new IntVector2(MapArea.Right, MapArea.Top);

		public IntVector2 BottomLeft => new IntVector2(MapArea.Left, MapArea.Bottom);

		public IntVector2 BottomRight => new IntVector2(MapArea.Right, MapArea.Bottom);

		public Vector2 Center => new Vector2(TopLeft.X + 0.5f, TopLeft.Y + 0.5f);

		public Vector3 Center3 => new Vector3(Center.X, Map.GetTerrainHeightAt(Center), Center.Y);

		public Vector3 TopLeft3 => new Vector3(MapArea.Left, Map.GetTerrainHeightAt(MapArea.Left, MapArea.Top), MapArea.Top);

		public Vector3 TopRight3 => new Vector3(MapArea.Right, Map.GetTerrainHeightAt(MapArea.Right, MapArea.Top), MapArea.Top);

		public Vector3 BottomLeft3 => new Vector3(MapArea.Left, Map.GetTerrainHeightAt(MapArea.Left, MapArea.Bottom), MapArea.Bottom);

		public Vector3 BottomRight3 => new Vector3(MapArea.Right, Map.GetTerrainHeightAt(MapArea.Right, MapArea.Bottom), MapArea.Bottom);

		public float TopLeftHeight { get; private set; }

		public float TopRightHeight => Map.GetTerrainHeightAt(MapArea.Left + 1, MapArea.Top);

		public float BottomLeftHeight => Map.GetTerrainHeightAt(MapArea.Left, MapArea.Top + 1);

		public float BottomRightHeight => Map.GetTerrainHeightAt(MapArea.Left + 1, MapArea.Top + 1);

		public IMap Map { get; private set; }

		/// <summary>
		/// List of units present on this tile, is NULL if there are no units on this tile
		/// </summary>
		List<IUnit> units;
		
		protected Tile(StTile storedTile, IMap map) {
			this.MapArea = new IntRect(storedTile.TopLeftPosition.X, 
									   storedTile.TopLeftPosition.Y, 
									   storedTile.TopLeftPosition.X + 1, 
									   storedTile.TopLeftPosition.Y + 1);
			this.TopLeftHeight = storedTile.Height;
			this.Map = map;
			units = null;
		}

		public Tile(int x, int y, TileType tileType, Map map) {
			MapArea = new IntRect(x, y, x + 1, y + 1);
			units = null;
			this.Type = tileType;
			this.TopLeftHeight = 0;
			this.Map = map;
		}

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

		public StTile Save()
		{
			return Loader.Save(this);
		}

		public void AddUnit(IUnit unit)
		{
			//Lazy allocation
			if (units == null) {
				units = new List<IUnit>();
			}
			units.Add(unit);
		}

		/// <summary>
		/// Removes a unit from this tile, either the owning unit or one of the passing units
		/// </summary>
		/// <param name="unit">the unit to remove</param>
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

		public void SetBuilding(IBuilding building) {
			if (Building != null) {
				throw new InvalidOperationException("There is a building already on this tile");
			}

			Building = building;
		}

		public void RemoveBuilding(IBuilding building) {
			if (Building != building) {
				throw new ArgumentException("Removing building that is not on this tile");
			}

			Building = null;
		}

		public void ChangeType(TileType newType) {
			Type = newType;
		}

		/// <summary>
		/// Called by the Map to change height
		/// 
		/// If you want to change height, go through <see cref="Map.ChangeTileHeight(ITile, float)"/>
		/// </summary>
		/// <param name="heightDelta"></param>
		public void ChangeTopLeftHeight(float heightDelta) {
			TopLeftHeight += heightDelta;
		}

		/// <summary>
		/// Sets the height of the top left corner of the tile to <paramref name="newHeight"/>
		/// </summary>
		/// <param name="newHeight">the height to set</param>

		public void SetTopLeftHeight(float newHeight) {
			TopLeftHeight = newHeight;
		}

		/// <summary>
		/// Is called every time any of the 4 corners of the tile change height
		/// </summary>
		public void CornerHeightChange()
		{			
			foreach (var unit in Units) {
				//Moves unit above terrain
				float terrainHeight = Map.GetTerrainHeightAt(unit.XZPosition);
				if (unit.Position.Y < terrainHeight) {
					unit.SetHeight(terrainHeight);
				}
				unit.TileHeightChanged(this);
			}
		}

		public float GetHeightAt(float x, float y)
		{
			return Building?.GetHeightAt(x, y) ?? Map.GetTerrainHeightAt(x, y);
		}

		public float GetHeightAt(Vector2 position)
		{
			return GetHeightAt(position.X, position.Y);
		}

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
	} 
}