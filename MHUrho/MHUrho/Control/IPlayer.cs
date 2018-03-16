using System.Collections.Generic;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Control {
    public interface IPlayer {
        
        int ID { get; }

        void HandleRaycast(RayQueryResult rayQueryResult);

        void HandleRaycast(List<RayQueryResult> rayQueryResults);

        /// <summary>
        /// Processes a player click on a unit
        /// </summary>
        /// <param name="unit">The unit that was clicked</param>
        void Click(Unit unit);

        /// <summary>
        /// Processes user click on a tile
        /// </summary>
        /// <param name="tile">The tile clicked</param>
        void Click(ITile tile);

        void UISelect(TileType tileType);

        void UISelect(Unit unit);

        void UIDeselect();

        /// <summary>
        /// Clears list of currently selected 
        /// </summary>
        void ClearSelected();

        StPlayer Save();

        void ConnectReferences(LevelManager level);

        void FinishLoading();

        void AddUnit(Unit unit);

        void RemoveUnit(Unit unit);
    }
}