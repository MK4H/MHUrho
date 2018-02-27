using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Storage;
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
        /// Level this unit is in
        /// </summary>
        public LevelManager Level { get; private set; }

        /// <summary>
        /// Player owning this unit
        /// </summary>
        public IPlayer Player { get; private set; }

        #endregion

        #region Private members
        
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

        public static Unit Load(LevelManager levelManager, StUnit storedUnit) {
            return new Unit(levelManager, storedUnit);
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
        /// Updates the unit, moves it according to the time since the last tick
        /// </summary>
        /// <param name="gameTime">The real time since the last update</param>
        public void Update(TimeSpan gameTime)
        {
            if (path != null)
            {
                if (target != null && target.Tile != path.Target)
                {
                    Order(target);
                }
                MoveAlongThePath((float)gameTime.TotalSeconds);
            }
            // if no path, then stay in the middle of the tile
            else if (!AmInTheMiddle())
            {
                MoveToMiddle((float)gameTime.TotalSeconds);
            }
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
            path = Level.GetPath(this,tile);
            if (path == null)
            {
                return false;
            }
            path.MoveNext();
            Tile.RemoveUnit(this);
            Tile.AddPassingUnit(this);
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
            Path NewPath = Level.GetPath(this, unit.Tile);
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
        /// <param name="levelManager">Manager of the loaded level</param>
        /// <param name="storedUnit">Image of the unit</param>
        protected Unit(LevelManager levelManager, StUnit storedUnit) {
            this.Level = levelManager;
            this.Selected = false;
            this.storage = storedUnit;
            this.ID = storedUnit.Id;
            this.path = Path.Load(storedUnit.Path);
            this.Position = new Vector2(storedUnit.Position.X, storedUnit.Position.Y);
        }
        
        public Unit(Tile tile, LevelManager level, Player player)
        {
            this.Level = level;
            this.Tile = tile;
            this.Position = tile.Center;
            this.Player = player;
            Selected = false;
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Moves unit to the middle of the Tile in Tile property
        /// </summary>
        /// <param name="elapsedSeconds"></param>
        void MoveToMiddle(float elapsedSeconds)
        {
            Vector2 NewPosition = Position + GetMoveVector(Tile.Center, elapsedSeconds);
            if (Vector2.Subtract(Position,Tile.Center).LengthSquared < Vector2.Subtract(Position, NewPosition).LengthSquared)
            {
                Position = Tile.Center;
            }
            else
            {
                Position = NewPosition;
            }
        }

        /// <summary>
        /// Moves unit towars the Tile that is next on the path
        /// </summary>
        /// <param name="elapsedSeconds"></param>
        void MoveAlongThePath(float elapsedSeconds)
        {
            if (path.Current == Tile.Location && AmInTheMiddle())
            {
                if (!path.MoveNext())
                {
                    path = null;
                }
                else
                {
                    //TODO: Make the path return the exact points which the unit should pass
                    MoveTowards(new Vector2(path.Current.X + 0.5f, path.Current.Y + 0.5f), elapsedSeconds);
                }
            }
            else if (path.Current == Tile.Location)
            {
                MoveToMiddle(elapsedSeconds);
            }
            else
            {
                MoveTowards(new Vector2(path.Current.X + 0.5f, path.Current.Y + 0.5f), elapsedSeconds);
            }
        }

        /// <summary>
        /// Moves unit towards the destination vector
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="elapsedSeconds"></param>
        void MoveTowards(Vector2 destination, float elapsedSeconds)
        {
            Position += GetMoveVector(destination, elapsedSeconds);
            IntVector2 TileIndex = new IntVector2((int)Position.X, (int)Position.Y);
            if (TileIndex == path.Current)
            {
                ITile NewTile = Level.TryMoveUnitThroughTileAt(this, TileIndex);
                if (NewTile == null)
                {
                    Order(path.Target);
                }
                else
                {
                    Tile = NewTile;
                }
            }
        }

        /// <summary>
        /// Calculates by how much should the unit move
        /// </summary>
        /// <param name="destination">The point in space that the unit is trying to get to</param>
        /// <param name="elapsedSeconds"> How many seconds passed since the last update</param>
        /// <returns></returns>
        Vector2 GetMoveVector(Vector2 destination, float elapsedSeconds)
        {
            Vector2 MovementDirection = destination - Position;
            MovementDirection.Normalize();
            return MovementDirection * Level.GameSpeed * elapsedSeconds;
        }

        /// <summary>
        /// Radius of the circle around the middle that counts as the middle
        /// For float rounding errors
        /// </summary>
        const float Tolerance = 0.1f;
        /// <summary>
        /// Checks if the unit is in the middle of the current tile
        /// </summary>
        /// <returns></returns>
        bool AmInTheMiddle()
        {
            return Vector2.Subtract(Tile.Center, Position).LengthFast < Tolerance;
        }

        
        #endregion
    }
}