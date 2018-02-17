namespace MHUrho.Logic {
    public interface IPlayer {
        /// <summary>
        /// Processes a player click on a unit
        /// </summary>
        /// <param name="unit">The unit that was clicked</param>
        void ClickUnit(IUnit unit);

        /// <summary>
        /// Processes user click on a tile
        /// </summary>
        /// <param name="tile">The tile clicked</param>
        void ClickTile(ITile tile);

        /// <summary>
        /// Clears list of currently selected 
        /// </summary>
        void ClearSelected();
    }
}