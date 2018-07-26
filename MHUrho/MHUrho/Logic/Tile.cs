using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Helpers;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic
{
	public class Tile : ITile {
		public const int ImageWidth = 100;
		public const int ImageHeight = 100;

		internal class Loader : ITileLoader {

			ITile ITileLoader.Tile => Tile;

			public Tile Tile { get; private set; }
			/// <summary>
			/// Holds the image of the tile between the steps of loading
			/// </summary>
			StTile storedTile;

			protected Loader(StTile storedTile, Tile tile)
			{
				this.Tile = tile;
				this.storedTile = storedTile;
			}

			public static StTile Save(Tile tile)
			{
				
				var storedTile = new StTile();
				storedTile.TopLeftPosition = tile.TopLeft.ToStIntVector2();
				storedTile.Height = tile.TopLeftHeight;
				storedTile.TileTypeID = tile.Type.ID;

				foreach (var passingUnit in tile.Units) {
					storedTile.UnitIDs.Add(passingUnit.ID);
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
			public static Loader StartLoading(StTile storedTile, Map map)
			{
				return new Loader(storedTile, new Tile(storedTile, map));
			}

			/// <summary>
			/// Continues loading by connecting references
			/// </summary>
			public void ConnectReferences(LevelManager level)
			{
				Tile.Type = PackageManager.Instance.ActiveGame.GetTileType(storedTile.TileTypeID);

				if (storedTile.UnitIDs.Count != 0) {
					Tile.units = new List<IUnit>();
				}

				foreach (var unit in storedTile.UnitIDs) {
					Tile.units.Add(level.GetUnit(unit));
				}

				//TODO: Connect buildings
				//foreach (var building in storage.Buil) {

				//}

			}

			public void FinishLoading()
			{
				storedTile = null;
			}
		}


		/// <summary>
		/// Units inside the tile
		/// </summary>
		public IReadOnlyList<IUnit> Units => units ?? new List<IUnit>();

		public IBuilding Building{ get; private set;}


		/// <summary>
		/// Tile type of this tile, only Map should set this
		/// </summary>
		public TileType Type { get; internal set; }
		
		/// <summary>
		/// The area in the map this tile represents
		/// </summary>
		public IntRect MapArea { get; private set; }


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

		public StTile Save()
		{
			return Loader.Save(this);
		}
		
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
		public void RemoveUnit(IUnit unit)
		{
			//TODO: Error, unit not present
			units.Remove(unit);
			if (units.Count == 0) {
				units = null;
			}
		}

		public void AddBuilding(IBuilding building) {
			if (Building != null) {
				throw new InvalidOperationException("Adding building to a tile that already has a building");
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
		public void CornerHeightChange() {
			foreach (var unit in Units) {
				unit.SetHeight(Map.GetTerrainHeightAt(unit.XZPosition));
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

		public IEnumerable<ITile> GetNeighbours()
		{
			//Top left neighbour
			yield return Map.GetTileByMapLocation(MapLocation + new IntVector2(-1, -1));
			//Top neighbour
			yield return Map.GetTileByMapLocation(MapLocation + new IntVector2(0, -1));
			//Top right neighbour
			yield return Map.GetTileByMapLocation(MapLocation + new IntVector2(1, -1));
			//Right neighbour
			yield return Map.GetTileByMapLocation(MapLocation + new IntVector2(-1, 0));
			//Bottom right neighbour
			yield return Map.GetTileByMapLocation(MapLocation + new IntVector2(-1, 1));
			//Bottom neighbour
			yield return Map.GetTileByMapLocation(MapLocation + new IntVector2(0, 1));
			//Bottom left neighbour 
			yield return Map.GetTileByMapLocation(MapLocation + new IntVector2(1, 1));
			//Left neighbour
			yield return Map.GetTileByMapLocation(MapLocation + new IntVector2(1, 0));

		}
	} 
}