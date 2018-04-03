using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.UnitComponents
{
    public class UnitSelector : Selector {
        public static string ComponentName = "UnitSelector";

        public override string Name => ComponentName;

        public override IPlayer Player => unit.Player;

        private Unit unit;
        private readonly LevelManager level;

        public UnitSelector(LevelManager level) {
            this.level = level;
        }

        public static UnitSelector Load(LevelManager level, PluginData data) {
            var sequentialData = new SequentialPluginDataReader(data);
            return new UnitSelector(level);
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

        //TODO: Hook up a reaction to unit death to deselect it from all tools

        public override PluginData SaveState() {
            var sequentialData = new SequentialPluginDataWriter();
            return sequentialData.PluginData;
        }

        public override void OnAttachedToNode(Node node) {
            base.OnAttachedToNode(node);

            unit = Node.GetComponent<Unit>();

            if (unit == null) {
                throw new
                    InvalidOperationException($"Cannot attach {nameof(UnitSelector)} to a node that does not have {nameof(Unit)} component");
            }
        }
    }
}
