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




		/// <summary>
		/// Units inside the tile
		/// </summary>
		public IReadOnlyList<IUnit> Units => units;

		public IBuilding Building{ get; private set;}

		/// <summary>
		/// Modifier of the movement speed of units passing through this tile
		/// </summary>
		public float MovementSpeedModifier
		{
			get
			{
				//TODO: Other factors
				return Type.MovementSpeedModifier;
			}

			set
			{

			}
		}

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
		/// Stores tile image between the steps of loading
		/// After loading is set to null to reclaim resources
		/// </summary>
		StTile storage;

		List<IUnit> units;

		public StTile Save() {
			var storedTile = new StTile();
			storedTile.TopLeftPosition = TopLeft.ToStIntVector2();
			storedTile.Height = TopLeftHeight;
			storedTile.TileTypeID = Type.ID;

			foreach (var passingUnit in Units) {
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
		public static Tile StartLoading(StTile storedTile, Map map) {
			return new Tile(storedTile, map);
		}

		/// <summary>
		/// Continues loading by connecting references
		/// </summary>
		public void ConnectReferences(ILevelManager level) {
			Type = PackageManager.Instance.ActiveGame.GetTileType(storage.TileTypeID);


			foreach (var unit in storage.UnitIDs) {
				units.Add(level.GetUnit(unit));
			}

			//TODO: Connect buildings
			
		}

		public void FinishLoading() {
			storage = null;
		}

		protected Tile(StTile storedTile, Map map) {
			this.storage = storedTile;
			this.MapArea = new IntRect(storedTile.TopLeftPosition.X, 
									   storedTile.TopLeftPosition.Y, 
									   storedTile.TopLeftPosition.X + 1, 
									   storedTile.TopLeftPosition.Y + 1);
			this.TopLeftHeight = storedTile.Height;
			this.Map = map;
			units = new List<IUnit>();
		}

		public Tile(int x, int y, TileType tileType, Map map) {
			MapArea = new IntRect(x, y, x + 1, y + 1);
			units = new List<IUnit>();
			this.Type = tileType;
			this.TopLeftHeight = 0;
			this.Map = map;
		}

		public void AddUnit(IUnit unit)
		{
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
		/// <param name="signalNeighbours">If <see cref="ChangeTopLeftHeight"/> should signal neighbours automatically
		/// if false, you need to signal every tile that has a corner height change yourself by calling <see cref="CornerHeightChange"/></param>
		public void ChangeTopLeftHeight(float heightDelta, bool signalNeighbours = true) {
			TopLeftHeight += heightDelta;
			// For rectangle changing height goes through every tile 4 times, which is slow
			// So if i want to speed it up, i can just call CornerHeightChange for the whole
			// rectangle just once per tile
			if (signalNeighbours) {
				Map.ForEachAroundCorner(TopLeft, (tile) => { tile.CornerHeightChange(); });
			}
			
		}

		/// <summary>
		/// Sets the height of the top left corner of the tile to <paramref name="newHeight"/>
		/// </summary>
		/// <param name="newHeight">the height to set</param>
		/// <param name="signalNeighbours">If <see cref="SetTopLeftHeight"/> should signal neighbours automatically
		/// if false, you need to signal every tile that has a corner height change yourself by calling <see cref="CornerHeightChange"/></param>
		public void SetTopLeftHeight(float newHeight, bool signalNeighbours = true) {
			TopLeftHeight = newHeight;
			// For rectangle changing height goes through every tile 4 times, which is slow
			// So if i want to speed it up, i can just call CornerHeightChange for the whole
			// rectangle just once per tile
			if (signalNeighbours) {
				Map.ForEachAroundCorner(TopLeft, (tile) => { tile.CornerHeightChange(); });
			}
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
	} 
}