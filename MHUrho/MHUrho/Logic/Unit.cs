using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MHUrho.Control;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;


namespace MHUrho.Logic
{
    public class Unit : Component
    {
        #region Public members

        public int UnitID { get; private set; }

        public UnitType UnitType { get; private set;}

        /// <summary>
        /// Position in the level
        /// </summary>
        public Vector2 Position {
            get {
                Debug.Assert(Node != null, nameof(Node) + " != null");
                return Node.Position.XZ2();
            }
            private set {
                Debug.Assert(Node != null, nameof(Node) + " != null");
                Node.Position = new Vector3(value.X, LevelManager.CurrentLevel.Map.GetHeightAt(value), value.Y);
            }
        }

        /// <summary>
        /// Tile this unit is standing on
        /// TODO: Maybe include all the tiles this unit touches, which may be up to 4 tiles
        /// </summary>
        public ITile Tile { get; private set; }

        /// <summary>
        /// Player owning this unit
        /// </summary>
        public IPlayer Player { get; private set; }

        #endregion

        #region Private members


        private readonly IUnitPlugin logic;

        /// <summary>
        /// Holds the image of this unit between the steps of loading
        /// After the last step, is set to null to free the resources
        /// In game is null
        /// </summary>
        StUnit storage;

        #endregion

        #region Public methods

        public static Unit Load(PackageManager packageManager, Node node, StUnit storedUnit) {
            var type = packageManager.GetUnitType(storedUnit.TypeID);
            if (type == null) {
                throw new ArgumentException("Type of this unit was not loaded");
            }
            return new Unit(type, storedUnit);
        }

        public static Unit Load(UnitType type, Node node, StUnit storedUnit) {
            //TODO: Check arguments
            return new Unit(type, storedUnit);
        }

        public StUnit Save() {
            var storedUnit = new StUnit();
            storedUnit.Id = UnitID;
            storedUnit.Position = new StVector2 {X = Position.X, Y = Position.Y};
            storedUnit.PlayerID = Player.ID;
            //storedUnit.Path = path.Save();
            //storedUnit.TargetUnitID = target.UnitID;
            storedUnit.TypeID = UnitType.ID;
            return storedUnit;
        }

        /// <summary>
        /// Continues loading by connecting references
        /// </summary>
        public void ConnectReferences() {
            if (storage.Path != null ) {
                var walker = GetComponent<WorldWalker>();
                if (walker == null) {
                    //TODO: Exception
                    throw new
                        Exception("Corrupted save file, unit has stored path even though it does not have a WorldWalker");
                }

                walker.GoAlong(Path.Load(storage.Path));
            }


            //TODO: Connect other things

        }

        public void FinishLoading() {
            storage = null;
        }


        public bool Order(ITile tile) {
            //Logic consumed order, return
            if (logic.Order(tile)) return true;

            var worldWalker = GetComponent<WorldWalker>();
            if (worldWalker != null) {
                var path = tile.GetPath(this);
                if (path != null) {
                    //Walker consumed order, return
                    worldWalker.GoAlong(path);
                    return true;
                }
            }

            //Nothing could be done with this order
            return false;
        }

        // TODO: differentiate between Meele and range units
        public bool Order(Unit unit)
        {
            // JUST MEELE UNITS FOR NOW
            if (unit.Player == Player)
            {
                throw new ArgumentException("Attacking my own units");
            }

            //target = unit;
            //// TODO: Maybe calculate where they will meet and pathfind there
            //Path NewPath = unit.Tile.GetPath(this);
            //if (NewPath == null)
            //{
            //    return false;
            //}

            //path.MoveNext();
            //Tile.RemoveUnit(this);
            //Tile.AddPassingUnit(this);
            //return true;
            throw new NotImplementedException();
        }

        
        //TODO: Link CanPass to TileType loaded from XML description
        //TODO: Load Passable terrain types from XML unit description
        public bool CanPass(ITile tile)
        {
            //TODO: This
            return true;
        }

        /// <summary>
        /// Gets units movements speed while moving through the tile
        /// </summary>
        /// <param name="tile">the tile on which the returned movementspeed applies</param>
        /// <returns>movement in tiles per second</returns>
        public float MovementSpeed(ITile tile) {
            //TODO: Route this through UnitType
            return tile.MovementSpeedModifier;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes everything apart from the things referenced by their ID or position
        /// </summary>
        /// <param name="storedUnit">Image of the unit</param>
        protected Unit(UnitType type, StUnit storedUnit) {
            this.storage = storedUnit;
            this.UnitID = storedUnit.Id;
            this.UnitType = type;
            this.Position = new Vector2(storedUnit.Position.X, storedUnit.Position.Y);
            this.logic = UnitType.UnitLogic.CreateNewInstance(LevelManager.CurrentLevel,Node, this);
        }
        
        public Unit(UnitType type, ITile tile, IPlayer player)
        {
            this.Tile = tile;
            this.Position = tile.Center;
            this.Player = player;
            this.UnitType = type;
        }

        #endregion

        #region Private Methods

        private void InitializeNode() {
            var staticModel = Node.CreateComponent<StaticModel>();
        }


        
        #endregion

     
    }
}