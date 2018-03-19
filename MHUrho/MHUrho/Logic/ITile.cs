using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.WorldMap;
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

        /// <summary>
        /// 3D coords of the center of the tile
        /// </summary>
        Vector3 Center3 { get; }


        //TODO: Maybe height for every corner
        /// <summary>
        /// Heigth of the top left corner of the tile
        /// </summary>
        float Height { get; }

        Map Map { get; }

        /// <summary>
        /// Continues loading by connecting references
        /// </summary>
        void ConnectReferences(LevelManager level);

        void FinishLoading();

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
        /// If you want to change height, go through <see cref="Map.ChangeTileHeight(ITile, float)"/>
        /// </summary>
        /// <param name="heightDelta"></param>
        /// <param name="signalNeighbours">If <see cref="ChangeHeight(float, bool)"/> should signal neighbours automatically
        /// if false, you need to signal every tile that has a corner height change yourself by calling <see cref="CornerHeightChange"/></param>
        void ChangeHeight(float heightDelta, bool signalNeighbours = true);

        /// <summary>
        /// Sets the height of the top left corner of the tile to <paramref name="newHeight"/>
        /// </summary>
        /// <param name="newHeight">the height to set</param>
        /// <param name="signalNeighbours">If <see cref="SetHeight(float, bool)"/> should signal neighbours automatically
        /// if false, you need to signal every tile that has a corner height change yourself by calling <see cref="CornerHeightChange"/></param>
        void SetHeight(float newHeight, bool signalNeighbours = true);

        /// <summary>
        /// Is called every time any of the 4 corners of the tile change height
        /// </summary>
        void CornerHeightChange();

        Path GetPath(Unit forUnit);

    }
}
