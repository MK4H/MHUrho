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
    public class UnitLogic : Component
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

        public static UnitLogic Load(PackageManager packageManager, Node node, StUnit storedUnit) {
            var type = packageManager.GetUnitType(storedUnit.TypeID);
            if (type == null) {
                throw new ArgumentException("Type of this unit was not loaded");
            }
            return new UnitLogic(type, storedUnit);
        }

        public static UnitLogic Load(UnitType type, Node node, StUnit storedUnit) {
            //TODO: Check arguments
            return new UnitLogic(type, storedUnit);
        }

        public StUnit Save() {
            var storedUnit = new StUnit();
            storedUnit.Id = UnitID;
            storedUnit.Position = new StVector2 {X = Position.X, Y = Position.Y};
            storedUnit.PlayerID = Player.ID;
            storedUnit.Path = path.Save();
            storedUnit.TargetUnitID = target.ID;
            storedUnit.TypeID = UnitType.ID;
            return storedUnit;
        }

        /// <summary>
        /// Continues loading by connecting references
        /// </summary>
        public void ConnectReferences() {
            UnitType = PackageManager.Instance.GetUnitType(storage.TypeID);

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
        protected UnitLogic(UnitType type, StUnit storedUnit) {
            this.Selected = false;
            this.storage = storedUnit;
            this.UnitID = storedUnit.Id;
            this.UnitType = type;
            this.path = Path.Load(storedUnit.Path);
            this.Position = new Vector2(storedUnit.Position.X, storedUnit.Position.Y);
            this.logic = UnitType.UnitLogic.CreateNewInstance(LevelManager.CurrentLevel,Node, this);
        }
        
        public UnitLogic(UnitType type, ITile tile, IPlayer player)
        {
            this.Tile = tile;
            this.Position = tile.Center;
            this.Player = player;
            this.UnitType = type;
            Selected = false;
        }

        #endregion

        #region Private Methods

        private void InitializeNode() {
            var staticModel = Node.CreateComponent<StaticModel>();
        }


        
        #endregion

     
    }
}