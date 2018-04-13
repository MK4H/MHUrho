﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.UnitComponents
{
    internal delegate void UnitSelectedDelegate(UnitSelector unitSelector);
    internal delegate void UnitDeselectedDelegate(UnitSelector unitSelector);
    internal delegate void UnitOrderedToTileDelegate(UnitSelector unitSelector, ITile targetTile, OrderArgs orderArgs);
    internal delegate void UnitOrderedToUnitDelegate(UnitSelector unitSelector, Unit targetUnit, OrderArgs orderArgs);
    internal delegate void UnitOrderedToBuildingDelegate(UnitSelector unitSelector, Building targetBuilding, OrderArgs orderArgs);


    public class UnitSelector : Selector {

        public static string ComponentName = nameof(UnitSelector);
        public static DefaultComponents ComponentID = DefaultComponents.UnitSelector;

        public override string Name => ComponentName;
        public override DefaultComponents ID => ComponentID;

        public override IPlayer Player => unit.Player;

        internal event UnitSelectedDelegate UnitSelected;
        internal event UnitDeselectedDelegate UnitDeselected;
        internal event UnitOrderedToTileDelegate OrderedToTile;
        internal event UnitOrderedToUnitDelegate OrderedToUnit;
        internal event UnitOrderedToBuildingDelegate OrderedToBuilding;

        private Unit unit;
        private readonly ILevelManager level;


        public UnitSelector(ILevelManager level) {
            this.level = level;
        }

        public static UnitSelector Load(ILevelManager level, PluginData data) {
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
            var orderArgs = new OrderArgs();
            OrderedToTile?.Invoke(unit, targetTile, orderArgs);
            return orderArgs.Executed;
        }

        public override bool Order(Unit targetUnit) {
            var orderArgs = new OrderArgs();
            OrderedToUnit?.Invoke(unit, targetUnit, orderArgs);
            return orderArgs.Executed;
        }

        public override bool Order(Building targetBuilding) {
            var orderArgs = new OrderArgs();
            OrderedToBuilding?.Invoke(unit, targetBuilding, orderArgs);
            return orderArgs.Executed;
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
