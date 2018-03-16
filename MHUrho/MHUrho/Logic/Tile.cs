using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic
{
    public class Tile : ITile {
        public const int ImageWidth = 100;
        public const int ImageHeight = 100;


        /// <summary>
        /// Unit that owns the tile, there can only be one
        /// </summary>
        public Unit Unit { get; private set; }

        /// <summary>
        /// Other units that are passing through the tile
        /// Units cannot stop in this tile if Unit is not null
        /// </summary>
        public List<Unit> PassingUnits { get; private set; }

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


        /// <summary>
        /// Location in the Map matrix
        /// </summary>
        public IntVector2 Location => new IntVector2(MapArea.Left,MapArea.Top); 

        public Vector2 Center => new Vector2(Location.X + 0.5f, Location.Y + 0.5f);

        public Vector3 Center3 => new Vector3(Center.X, Map.GetHeightAt(Center), Center.Y);

        public float Height { get; private set; }

        public Map Map { get; private set; }

        /// <summary>
        /// Stores tile image between the steps of loading
        /// After loading is set to null to reclaim resources
        /// </summary>
        private StTile storage;

        public StTile Save() {
            var storedTile = new StTile();
            storedTile.UnitID = Unit?.ID ?? 0;
            storedTile.Position = new StIntVector2 { X = Location.X, Y = Location.Y};
            storedTile.Height = Height;
            storedTile.TileTypeID = Type.ID;

            var storedPassingUnits = storedTile.PassingUnitIDs;
            foreach (var passingUnit in PassingUnits) {
                storedPassingUnits.Add(passingUnit.ID);
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
        public void ConnectReferences() {
            Type = PackageManager.Instance.GetTileType(storage.TileTypeID);

            //TODO: Connect units
            
        }

        public void FinishLoading() {
            storage = null;
        }

        protected Tile(StTile storedTile, Map map) {
            this.storage = storedTile;
            this.MapArea = new IntRect(storedTile.Position.X, storedTile.Position.Y, storedTile.Position.X + 1, storedTile.Position.Y + 1);
            this.Height = storedTile.Height;
            this.Map = map;
            PassingUnits = new List<Unit>();
        }

        public Tile(int x, int y, TileType tileType, Map map) {
            MapArea = new IntRect(x, y, x + 1, y + 1);
            PassingUnits = new List<Unit>();
            Unit = null;
            this.Type = tileType;
            this.Height = 0;
            this.Map = map;
        }

        public void AddPassingUnit(Unit unit)
        {
            PassingUnits.Add(unit);
        }

        /// <summary>
        /// Tries to set unit as owning unit, if there is not already one
        /// </summary>
        /// <param name="unit">The new owning unit</param>
        /// <returns>true if set, false if not set</returns>
        public bool TryAddOwningUnit(Unit unit)
        {
            //TODO: locking/threading
            if (Unit == null)
            {
                Unit = unit;
                return true;
            }
            return false;

        }

        /// <summary>
        /// Removes a unit from this tile, either the owning unit or one of the passing units
        /// </summary>
        /// <param name="unit">the unit to remove</param>
        public void RemoveUnit(Unit unit)
        {
            //TODO: Error, unit not present
            if (Unit == unit)
            {
                Unit = null;
            }
            else
            {
                PassingUnits.Remove(unit);
            }
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
        /// <param name="signalNeighbours">If <see cref="ChangeHeight(float, bool)"/> should signal neighbours automatically
        /// if false, you need to signal every tile that has a corner height change yourself by calling <see cref="CornerHeightChange"/></param>
        public void ChangeHeight(float heightDelta, bool signalNeighbours = true) {
            Height += heightDelta;
            // For rectangle changing height goes through every tile 4 times, which is slow
            // So if i want to speed it up, i can just call CornerHeightChange for the whole
            // rectangle just once per tile
            if (signalNeighbours) {
                Map.ForEachAroundCorner(Location, (tile) => { tile.CornerHeightChange(); });
            }
            
        }

        /// <summary>
        /// Sets the height of the top left corner of the tile to <paramref name="newHeight"/>
        /// </summary>
        /// <param name="newHeight">the height to set</param>
        /// <param name="signalNeighbours">If <see cref="SetHeight(float, bool)"/> should signal neighbours automatically
        /// if false, you need to signal every tile that has a corner height change yourself by calling <see cref="CornerHeightChange"/></param>
        public void SetHeight(float newHeight, bool signalNeighbours = true) {
            Height = newHeight;
            // For rectangle changing height goes through every tile 4 times, which is slow
            // So if i want to speed it up, i can just call CornerHeightChange for the whole
            // rectangle just once per tile
            if (signalNeighbours) {
                Map.ForEachAroundCorner(Location, (tile) => { tile.CornerHeightChange(); });
            }
        }

        /// <summary>
        /// Is called every time any of the 4 corners of the tile change height
        /// </summary>
        public void CornerHeightChange() {
            Unit?.SetHeight(Map.GetHeightAt(Unit.XZPosition));
            foreach (var unit in PassingUnits) {
                unit.SetHeight(Map.GetHeightAt(unit.XZPosition));
            }
        }

        public Path GetPath(Unit forUnit) {
            return Map.GetPath(forUnit, this);
        }
    } 
}