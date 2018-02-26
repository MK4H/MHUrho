using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Urho;
using ProtoBuf;

namespace MHUrho.Logic
{
    [ProtoContract]
    public class Tile : ITile
    {
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
        [ProtoMember(1,AsReference = true)]
        public TileType Type { get; set; }
        
        /// <summary>
        /// The area in the map this tile represents
        /// </summary>
        [ProtoMember(2)]
        public IntRect MapArea { get; private set; }

        /// <summary>
        /// X index in the Map array
        /// </summary>
        public int XIndex { get { return MapArea.Left; } }
        /// <summary>
        /// Y index in the Map array
        /// </summary>
        public int YIndex { get { return MapArea.Top; } }

        /// <summary>
        /// Location in the Map matrix
        /// </summary>
        public IntVector2 Location { get { return new IntVector2(MapArea.Left,MapArea.Top); } }

        public Vector2 Center { get { return new Vector2(Location.X + 0.5f, Location.Y + 0.5f); } }

        [ProtoMember(3)]
        public float Height { get; private set; }

        [ProtoMember(4,AsReference = true)]
        public LogicManager Logic { get; set; }


        /// <summary>
        /// TEMPORARY
        /// </summary>
        /// <returns></returns>
        public bool SpawnUnit(Player player)
        {
            Unit unit = new Unit(this, Logic, player);
            Logic.RegisterUnit(unit);

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

        public Tile(LogicManager logic, int x, int y)
        {
            this.Logic = logic;
            MapArea = new IntRect(x, y, 1, 1);
            MovementSpeedModifier = 2;
            PassingUnits = new List<Unit>();
        }

    }
}