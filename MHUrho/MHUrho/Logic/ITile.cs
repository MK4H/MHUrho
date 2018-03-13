using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic
{

    //TODO: Move it somewhere else
    public enum SplitDirection {
        TopRight,
        TopLeft
    };
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
        /// Location in the Map matrix
        /// </summary>
        IntVector2 Location { get; }

        /// <summary>
        /// Coords of the center of the tile
        /// </summary>
        Vector2 Center { get; }


        //TODO: Maybe height for every corner
        /// <summary>
        /// Heigth of the top left corner of the tile
        /// </summary>
        float Height { get; }

        /// <summary>
        /// Continues loading by connecting references
        /// </summary>
        void ConnectReferences();

        void FinishLoading();

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

        /// <summary>
        /// Should only be called by Map class (how i wish C# had friend functions)
        /// </summary>
        /// <param name="newType"></param>
        void ChangeType(TileType newType);

        /// <summary>
        /// Called by the Map to change height
        /// 
        /// If you want to change height, go through TODO:LINK MAP FUNCTION TO CHANGE TILE HEIGHT
        /// </summary>
        /// <param name="heightDelta"></param>
        void ChangeHeight(float heightDelta);

        /// <summary>
        /// Called by the Map to set height
        /// 
        /// If you want to set height, go through TODO:LINK MAP FUNCTION TO CHANGE TILE HEIGHT
        /// </summary>
        /// <param name="newHeight"></param>
        void SetHeight(float newHeight);

    }
}
