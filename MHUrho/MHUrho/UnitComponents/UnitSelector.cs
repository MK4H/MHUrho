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
    public delegate void UnitSelectedDelegate(Unit unit);
    public delegate void UnitDeselectedDelegate(Unit unit);
    public delegate void UnitOrderedToTileDelegate(Unit unit, ITile targetTile);
    public delegate void UnitOrderedToUnitDelegate(Unit unit, Unit targetUnit);
    public delegate void UnitOrderedToBuildingDelegate(Unit unit, Building targetBuilding);


    public class UnitSelector : Selector {
        public static string ComponentName = "UnitSelector";

        public override string Name => ComponentName;

        public override IPlayer Player => unit.Player;

        public event UnitSelectedDelegate UnitSelected;
        public event UnitDeselectedDelegate UnitDeselected;
        public event UnitOrderedToTileDelegate OrderedToTile;
        public event UnitOrderedToUnitDelegate OrderedToUnit;
        public event UnitOrderedToBuildingDelegate OrderedToBuilding;

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
        /// Orders this selected unit with target <paramref name="targetTile"/>
        /// 
        /// if the unit can do anything, returns true,the order is given and the unit will procede
        /// if the unit cant do anything, returns false
        /// </summary>
        /// <param name="targetTile">target tile</param>
        /// <returns>True if unit was given order, False if there is nothing the unit can do</returns>
        public override bool Order(ITile targetTile) {
            //TODO: EventArgs to get if the event was handled
            OrderedToTile?.Invoke(unit, targetTile);
        }

        public override bool Order(Unit targetUnit) {
            OrderedToUnit?.Invoke(unit, targetUnit);
        }

        public override bool Order(Building targetBuilding) {
            OrderedToBuilding?.Invoke(unit, targetBuilding);
        }

        public override void Select() {
            Selected = true;
            UnitSelected?.Invoke(unit);
        }

        public override void Deselect() {
            Selected = false;
            UnitDeselected?.Invoke(unit);
        }

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
