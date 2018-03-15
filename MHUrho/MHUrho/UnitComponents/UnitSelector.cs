using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;

namespace MHUrho.UnitComponents
{
    public class UnitSelector : Selector
    {
        private readonly Unit unit;
        private readonly LevelManager level;

        public UnitSelector(Unit unit, LevelManager level) {
            this.unit = unit;
            this.level = level;
        }

        /// <summary>
        /// Orders this selected unit with target <paramref name="tile"/>
        /// 
        /// if the unit can do anything, returns true,the order is given and the unit will procede
        /// if the unit cant do anything, returns false
        /// </summary>
        /// <param name="tile">target tile</param>
        /// <returns>True if unit was given order, False if there is nothing the unit can do</returns>
        public override bool Ordered(ITile tile) {
            return unit.Order(tile);
        }

        public override bool Ordered(Unit unit) {
            throw new NotImplementedException();
        }

        //public void Ordered(IBuilding building) {

        //}
    }
}
