﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Packaging;
using MHUrho.Storage;
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
        /// Tile type of this tile
        /// </summary>
        public TileType Type { get; set; }
        
        /// <summary>
        /// The area in the map this tile represents
        /// </summary>
        public IntRect MapArea { get; private set; }

        /// <summary>
        /// X index in the Map array
        /// </summary>
        public int XIndex => MapArea.Left;
        /// <summary>
        /// Y index in the Map array
        /// </summary>
        public int YIndex => MapArea.Top;

        /// <summary>
        /// Location in the Map matrix
        /// </summary>
        public IntVector2 Location => new IntVector2(MapArea.Left,MapArea.Top); 

        public Vector2 Center => new Vector2(Location.X + 0.5f, Location.Y + 0.5f);

        public float Height { get; private set; }


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
        /// <returns>Partially initialized tile</returns>
        public static Tile StartLoading(StTile storedTile) {
            return new Tile(storedTile);
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

        protected Tile(StTile storedTile) {
            this.storage = storedTile;
            this.MapArea = new IntRect(storedTile.Position.X, storedTile.Position.Y, storedTile.Position.X + 1, storedTile.Position.Y + 1);
            this.Height = storedTile.Height;
        }

        public Tile(int x, int y, TileType tileType) {
            MapArea = new IntRect(x, y, x + 1, y + 1);
            PassingUnits = new List<Unit>();
            Unit = null;
            this.Type = tileType;
            this.Height = 0;
        }

        /// <summary>
        /// TEMPORARY
        /// </summary>
        /// <returns></returns>
        public bool SpawnUnit(Player player)
        {
            Unit unit = new Unit(this, player);
            LevelManager.CurrentLevel.RegisterUnit(unit);

            if (this.Unit != null)
            {
                PassingUnits.Add(unit);
                return false;
            }
            else
            {
                this.Unit = unit;
                return true;
            }
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

        
    }
}