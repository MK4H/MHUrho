using MHUrho.Logic;
using MHUrho.Storage;

namespace MHUrho.Control {
    public interface IPlayer {
        
        int ID { get; }
        


        /// <summary>
        /// Processes a player click on a unit
        /// </summary>
        /// <param name="unit">The unit that was clicked</param>
        void Click(IUnit unit);

        /// <summary>
        /// Processes user click on a tile
        /// </summary>
        /// <param name="tile">The tile clicked</param>
        void Click(ITile tile);

        void UISelect(TileType tileType);

        void UISelect(IUnit unit);

        void UIDeselect();

        /// <summary>
        /// Clears list of currently selected 
        /// </summary>
        void ClearSelected();

        StPlayer Save();
    }
}