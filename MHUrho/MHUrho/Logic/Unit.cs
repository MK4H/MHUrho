using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;


namespace MHUrho.Logic
{
    public class Unit : IUnit
    {
        #region Public members

        public int ID { get; private set; }

        public UnitType Type { get; private set;}

        /// <summary>
        /// Position in the level
        /// </summary>
        public Vector2 Position { get; private set; }

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

        private readonly Node node;

        /// <summary>
        /// Flag to prevent double selection
        /// </summary>
        bool Selected;

        //TEMPORARY
        public bool IsSelected { get { return Selected; } }


        /// <summary>
        /// Current path this unit is following
        /// </summary>
        Path path;

        /// <summary>
        /// Current target this unit is trying to attack
        /// </summary>
        IUnit target;

        /// <summary>
        /// Holds the image of this unit between the steps of loading
        /// After the last step, is set to null to free the resources
        /// In game is null
        /// </summary>
        StUnit storage;

        #endregion

        #region Public methods

        public static Unit Load(StUnit storedUnit, Node node) {
            return new Unit(storedUnit, node);
        }

        public StUnit Save() {
            var storedUnit = new StUnit();
            storedUnit.Id = ID;
            storedUnit.Position = new StVector2 {X = Position.X, Y = Position.Y};
            storedUnit.PlayerID = Player.ID;
            storedUnit.Path = path.Save();
            storedUnit.TargetUnitID = target.ID;
            storedUnit.TypeID = Type.ID;
            return storedUnit;
        }

        /// <summary>
        /// Continues loading by connecting references
        /// </summary>
        public void ConnectReferences() {
            Type = PackageManager.Instance.GetUnitType(storage.TypeID);

            //TODO: Connect other things

        }

        public void FinishLoading() {
            storage = null;
        }

        /// <summary>
        /// Tries to select the unit, if not selected sets selected, if selected does nothing
        /// </summary>
        /// <returns>true if unit was not selected, false if unit was selected</returns>
        public bool Select()
        {
            //TODO: More processing
            if (!Selected)
            {
                Selected = true;
                return true;
            }
            return false;
        }

        public bool Order(ITile tile)
        {
            path = tile.GetPath(this);
            if (path == null)
            {
                return false;
            }

            return true;
        }

        // TODO: differentiate between Meele and range units
        public bool Order(IUnit unit)
        {
            // JUST MEELE UNITS FOR NOW
            if (unit.Player == Player)
            {
                throw new ArgumentException("Attacking my own units");
            }

            target = unit;
            // TODO: Maybe calculate where they will meet and pathfind there
            Path NewPath = unit.Tile.GetPath(this);
            if (NewPath == null)
            {
                return false;
            }

            path.MoveNext();
            Tile.RemoveUnit(this);
            Tile.AddPassingUnit(this);
            return true;
        }

        public void Deselect()
        {
            Selected = false;
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
        protected Unit(StUnit storedUnit, Node node) {
            this.node = node;
            this.Selected = false;
            this.storage = storedUnit;
            this.ID = storedUnit.Id;
            this.path = Path.Load(storedUnit.Path);
            this.Position = new Vector2(storedUnit.Position.X, storedUnit.Position.Y);
        }
        
        public Unit(UnitType type, Node node, Tile tile, Player player)
        {
            this.node = node;
            this.Tile = tile;
            this.Position = tile.Center;
            this.Player = player;
            this.Type = type;
            Selected = false;
        }

        #endregion

        #region Private Methods

        private void InitializeNode() {
            var staticModel = node.CreateComponent<StaticModel>();
        }


        
        #endregion

     
    }
}