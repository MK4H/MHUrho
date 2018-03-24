﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Google.Protobuf;
using MHUrho.Control;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho;
using Urho.Physics;


namespace MHUrho.Logic
{
    /// <summary>
    /// Class representing unit, every action you want to do with the unit should go through this class
    /// </summary>
    public class Unit : Component
    {
        #region Public members

        /// <summary>
        /// ID of this unit
        /// Hides component member ID, but having two IDs would be more confusing
        /// 
        /// If you need component ID, just cast this to component and access ID
        /// </summary>
        public new int ID { get; private set; }

        public UnitType UnitType { get; private set;}

        /// <summary>
        /// Position in the level
        /// </summary>
        public Vector2 XZPosition {
            get {
                Debug.Assert(Node != null, nameof(Node) + " != null");
                return Node.Position.XZ2();
            }
            private set {
                Debug.Assert(Node != null, nameof(Node) + " != null");
                Node.Position = new Vector3(value.X, LevelManager.CurrentLevel.Map.GetHeightAt(value), value.Y);
            }
        }

        public Vector3 Position => Node.Position;

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


        private IUnitPlugin logic;

        /// <summary>
        /// Holds the image of this unit between the steps of loading
        /// After the last step, is set to null to free the resources
        /// In game is null
        /// </summary>
        StUnit storage;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes everything apart from the things referenced by their ID or position
        /// </summary>
        /// <param name="type">type of the loading unit</param>
        /// <param name="storedUnit">Image of the unit</param>
        protected Unit(UnitType type, StUnit storedUnit) {
            this.storage = storedUnit;
            this.ID = storedUnit.Id;
            this.UnitType = type;
        }

        /// <summary>
        /// If you want to spawn new unit, call <see cref="LevelManager.SpawnUnit(Logic.UnitType,ITile,IPlayer)"/>
        /// 
        /// Constructs new instance of Unit control component
        /// </summary>
        /// <param name="id">identifier unique between units </param>
        /// <param name="type">the type of the unit</param>
        /// <param name="tile">Tile where the unit spawned</param>
        /// <param name="player">Owner of the unit</param>
        protected Unit(int id, UnitType type, ITile tile, IPlayer player) {
            this.ID = id;
            this.Tile = tile;
            this.Player = player;
            this.UnitType = type;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Loads unit component from <paramref name="storedUnit"/> and all other needed components
        ///  and adds them to the <paramref name="node"/>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="packageManager">Package manager to get unitType</param>
        /// <param name="node">scene node of the unit</param>
        /// <param name="storedUnit">stored unit</param>
        /// <returns>Loaded unit component, already added to the node</returns>
        public static Unit Load(LevelManager level, PackageManager packageManager, Node node, StUnit storedUnit) {
            var type = packageManager.GetUnitType(storedUnit.TypeID);
            if (type == null) {
                throw new ArgumentException("Type of this unit was not loaded");
            }

            return type.LoadUnit(level, node, storedUnit);
        }

        /// <summary>
        /// Loads ONLY the unit component of <paramref name="type"/> from <paramref name="storedUnit"/> and adds it to the <paramref name="node"/> 
        /// If you use this, you still need to add Model, Materials and other behavior to the unit
        /// </summary>
        /// <param name="type"></param>
        /// <param name="node"></param>
        /// <param name="storedUnit"></param>
        /// <returns>Loaded unit component, already added to the node</returns>
        public static Unit Load(LevelManager level, UnitType type, Node node, StUnit storedUnit) {
            //TODO: Check arguments - node cant have more than one Unit component
            if (type.ID != storedUnit.TypeID) {
                throw new ArgumentException("provided type is not the type of the stored unit",nameof(type));
            }

            var unit = new Unit(type, storedUnit);
            node.AddComponent(unit);
            node.Position = new Vector3(storedUnit.Position.X, storedUnit.Position.Y, storedUnit.Position.Z);

            unit.logic = type.UnitLogic.LoadNewInstance(level, node, unit, new PluginDataWrapper(storedUnit.UserPlugin));
            //This is the main reason i add Unit to node right here, because i want to isolate the storedUnit reading
            // to this class, and for that i need to set the Position here
            
            return node.GetComponent<Unit>();
        }

        /// <summary>
        /// Creates new instance of the <see cref="Unit"/> component and ads it to the <paramref name="unitNode"/>
        /// </summary>
        /// <param name="id">The unique identifier of the unit, must be unique among other units</param>
        /// <param name="unitNode">Scene node of the unit</param>
        /// <param name="type">type of the unit</param>
        /// <param name="tile">tile where the unit will spawn</param>
        /// <param name="player">owner of the unit</param>
        /// <returns>the unit component, already added to the node</returns>
        public static Unit CreateNew(int id, Node unitNode, UnitType type, LevelManager level, ITile tile, IPlayer player) {
            //TODO: Check if there is already a Unit component on this node, if there is, throw exception
            var unit = new Unit(id, type, tile, player);
            unitNode.AddComponent(unit);
            unitNode.Position = tile.Center3;

            

            //TODO: Storing and loading
            var rigidBody = unitNode.CreateComponent<RigidBody>();
            rigidBody.CollisionLayer = (int)CollisionLayer.Unit; 
            rigidBody.CollisionMask =(int)( CollisionLayer.Arrow | CollisionLayer.Boulder);
            rigidBody.Kinematic = true;
            rigidBody.Mass = 1;
            rigidBody.UseGravity = false;

            var collider = unitNode.CreateComponent<CollisionShape>();
            collider.SetBox(new Vector3(1, 1, 1), new Vector3(-0.5f, -0.5f, -0.5f), Quaternion.Identity);

            unit.logic = type.UnitLogic.CreateNewInstance(level, unitNode, unit);

            return unit;
        }

        public StUnit Save() {
            var storedUnit = new StUnit();
            storedUnit.Id = ID;
            storedUnit.Position = new StVector3 {X = XZPosition.X, Y = Node.Position.Y, Z = XZPosition.Y};
            storedUnit.PlayerID = Player.ID;
            //storedUnit.Path = path.Save();
            //storedUnit.TargetUnitID = target.UnitID;
            storedUnit.TypeID = UnitType.ID;


            storedUnit.UserPlugin = new PluginData();
            logic.SaveState(new PluginDataWrapper(storedUnit.UserPlugin));

            foreach (var component in Node.Components) {
                var defaultComponent = component as DefaultComponent;
                if (defaultComponent != null) {
                    storedUnit.DefaultComponentData.Add(defaultComponent.Name, defaultComponent.SaveState());
                }
            }

            return storedUnit;
        }

        /// <summary>
        /// Continues loading by connecting references and loading components
        /// </summary>
        public void ConnectReferences(LevelManager level) {
            Player = level.GetPlayer(storage.PlayerID);
            Tile = level.Map.GetContainingTile(Position);
            //TODO: Connect other things

            foreach (var defaultComponent in storage.DefaultComponentData) {
                Node.AddComponent(level.DefaultComponentFactory.LoadComponent(defaultComponent.Key, defaultComponent.Value, level));
            }
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


        public void SetHeight(float newHeight) {
            Node.Position = new Vector3(Node.Position.X, newHeight, Node.Position.Z);
        }

        public bool MoveBy(Vector3 moveBy) {
            var newPosition = Node.Position + moveBy;

            return MoveTo(newPosition);
        }

        public bool MoveTo(Vector3 newPosition) {
            bool canMoveToTile = CheckTile(newPosition);
            if (!canMoveToTile) {
                return false;
            }

            Node.Position = newPosition;
            return true;
        }
        #endregion

        

        #region Private Methods

        private void InitializeNode() {
            var staticModel = Node.CreateComponent<StaticModel>();
        }

        private bool CheckTile(Vector3 newPosition) {
            ITile newTile;
            //Still in the same tile
            if ((newTile = Tile.Map.GetContainingTile(newPosition)) == Tile) {
                return true;
            }
            //New tile, but cant pass
            if (!CanPass(newTile)) {
                return false;
            }

            //New tile, but can pass
            Tile.RemoveUnit(this);
            Tile = newTile;
            //TODO: Add as owning unit
            Tile.AddPassingUnit(this);
            return true;
        }
        
        #endregion

     
    }
}