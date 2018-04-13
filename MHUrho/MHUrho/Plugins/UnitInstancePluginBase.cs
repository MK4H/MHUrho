using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;

namespace MHUrho.Plugins
{
    //TODO: Either make one instance of this for every unit,
    // OR make just it a singleton, where all the data will be stored in Unit class
    public abstract class UnitInstancePluginBase {
        public virtual void OnUpdate(float timeStep) {
            //NOTHING
        }

        public abstract void SaveState(PluginDataWrapper pluginData);

        /// <summary>
        /// Loads instance into the state saved in <paramref name="pluginData"/>
        /// 
        /// DO NOT LOAD the default components the unit had when saving, that is done independently by
        /// the Unit class and the components themselfs, just load your own data
        /// 
        /// The default components will be loaded and present on the <see cref="Unit.Node"/>, so you 
        /// can get them by calling <see cref="Node.GetComponent{T}(bool)"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="unit"></param>
        /// <param name="pluginData">stored state of the unit plugin</param>
        /// <returns>Instance loaded into saved state</returns>
        public abstract void LoadState(ILevelManager level, Unit unit, PluginDataWrapper pluginData);

        public virtual bool CanGoFromTo(ITile fromTile, ITile toTile) {
            throw new NotImplementedException("You need to override CanGoFromTo to use WordlWalker");
        }

        public virtual void OnMovementStarted(WorldWalker walker, int tag) {
            //NOTHING
        }

        public virtual void OnMovementFinished(WorldWalker walker, int tag) {
            //NOTHING
        }

        public virtual void OnMovementFailed(WorldWalker walker, int tag) {
            //NOTHING
        }

        public virtual void OnUnitHit() {
            //NOTHING
        }

        public virtual void OnTaskStarted(ActionQueue queue, ActionQueue.WorkTask task) {
            //NOTHING
        }

        public virtual void OnTaskFinished(ActionQueue queue, ActionQueue.WorkTask task) {
            //NOTHING
        }

        public virtual void OnTaskCancelled(ActionQueue queue, ActionQueue.WorkTask task) {
            //NOTHING
        }

        public virtual void OnTargetAcquired(Shooter shooter) {
            //NOTHING
        }

        public virtual void OnShotFired(Shooter shooter) {
            //NOTHING
        }

        public virtual void OnUnitSelected(UnitSelector selector) {
            //NOTHING
        }

        public virtual void OnUnitDeselected(UnitSelector selector) {
            //NOTHING
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="targetTile"></param>
        /// <returns>True if the unit executed the order, false if the unit was not able to execute the order</returns>
        public virtual bool OnUnitOrderedToTile(UnitSelector selector, ITile targetTile) {
            return false;
        }

        public virtual bool OnUnitOrderedToUnit(UnitSelector selector, Unit targetUnit) {
            return false;
        }

        public virtual bool OnUnitOrderedToBuilding(UnitSelector selector, Building targetBuilding) {
            return false;
        }


        //TODO: Expand this
    }
}
