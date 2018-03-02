using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic
{
    public interface ITile {

        /// <summary>
        /// Unit that owns the tile, there can only be one
        /// </summary>
        Unit Unit { get; }

        /// <summary>
        /// Other units that are passing through the tile
        /// Units cannot stop in this tile if Unit is not null
        /// </summary>
        List<Unit> PassingUnits { get; }

        /// <summary>
        /// Default modifier of the movement speed of units passing through this tile
        /// </summary>
        float MovementSpeedModifier { get; }

        /// <summary>
        /// Tile type of this tile
        /// </summary>
        TileType Type { get; }

        /// <summary>
        /// The area in the map this tile represents
        /// </summary>
        IntRect MapArea { get; }

        /// <summary>
        /// X index in the Map array
        /// </summary>
        int XIndex { get; }

        /// <summary>
        /// Y index in the Map array
        /// </summary>
        int YIndex { get; }

        /// <summary>
        /// Location in the Map matrix
        /// </summary>
        IntVector2 Location { get; }

        /// <summary>
        /// Coords of the center of the tile
        /// </summary>
        Vector2 Center { get; }


        //TODO: Maybe height for every corner
        /// <summary>
        /// Heigth of the center of the tile
        /// </summary>
        float Height { get; }

        bool SpawnUnit(Player player);

        void AddPassingUnit(Unit unit);

        /// <summary>
        /// Tries to set unit as owning unit, succedes if there is not one already
        /// </summary>
        /// <param name="unit">The new owning unit</param>
        /// <returns>true if set, false if not set</returns>
        bool TryAddOwningUnit(Unit unit);

        /// <summary>
        /// Removes a unit from this tile, either the owning unit or one of the passing units
        /// </summary>
        /// <param name="unit">the unit to remove</param>
        void RemoveUnit(Unit unit);

        StTile Save();
    }
}
