﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using MHUrho.Helpers;
using MHUrho.WorldMap;

namespace MHUrho.UnitComponents
{
    public class Shooter : DefaultComponent
    {

        public static string ComponentName = nameof(Shooter);
        public static DefaultComponents ComponentID = DefaultComponents.DirectShooter;
        public override string Name => ComponentName;
        public override DefaultComponents ID => ComponentID;

        /// <summary>
        /// Shots per minute
        /// </summary>
        public float RateOfFire { get; set; }

        private Vector3 target; //TODO: Change to Target component

        private float delay;

        private ProjectileType projectileType;

        /// <summary>
        /// Offset of the projectile source from Unit Node in the direction to the target
        /// </summary>
        private readonly float horizontalOffset;
        /// <summary>
        /// Offset of the projectile source from Unit Node vertically
        /// </summary>
        private readonly float verticalOffset;

        private ILevelManager level;

        private Map Map => level.Map;

        public Shooter(ILevelManager level,
                             Vector3 target,
                             ProjectileType projectileType,
                             float rateOfFire,
                             float horizontalOffset,
                             float verticalOffset) {
            this.level = level;
            this.target = target;
            this.projectileType = projectileType;
            this.RateOfFire = rateOfFire;
            this.horizontalOffset = horizontalOffset;
            this.verticalOffset = verticalOffset;
            this.delay = 60 / RateOfFire;
            ReceiveSceneUpdates = true;
        }

        public static Shooter Load(ILevelManager level, PluginData storedData) {
            var sequentialDataReader = new SequentialPluginDataReader(storedData);
            sequentialDataReader.MoveNext();
            var rateOfFire = sequentialDataReader.GetCurrent<float>();
            sequentialDataReader.MoveNext();
            var target = sequentialDataReader.GetCurrent<Vector3>();
            sequentialDataReader.MoveNext();
            var horizontalOffset = sequentialDataReader.GetCurrent<float>();
            sequentialDataReader.MoveNext();
            var verticalOffset = sequentialDataReader.GetCurrent<float>();
            sequentialDataReader.MoveNext();
            var projectileTypeID = sequentialDataReader.GetCurrent<int>();
            return new Shooter(level,
                                     target,
                                     level.PackageManager.ActiveGame.GetProjectileType(projectileTypeID),
                                     rateOfFire,
                                     horizontalOffset,
                                     verticalOffset);
        }

        /// <summary>
        /// Calculates the movement vectors for projectile with initial speed <paramref name="projectileSpeed"/>, to go from <paramref name="sourcePosition"/> to <paramref name="targetPosition"/>
        /// 
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <param name="sourcePosition"></param>
        /// <param name="projectileSpeed"></param>
        /// <param name="lowTime"></param>
        /// <param name="lowVector"></param>
        /// <param name="highTime"></param>
        /// <param name="highVector"></param>
        /// <returns>True if it is possible to hit the <paramref name="targetPosition"/> with the given <paramref name="projectileSpeed"/>,
        /// and the out parameters are valid, or false if it is not possible and the out params are invalid</returns>
        public static bool GetTimesAndAngles(Vector3 targetPosition, Vector3 sourcePosition, float projectileSpeed, out float lowTime, out Vector3 lowVector, out float highTime, out Vector3 highVector) {
            //Source https://blog.forrestthewoods.com/solving-ballistic-trajectories-b0165523348c
            // https://en.wikipedia.org/wiki/Projectile_motion

            //TODO: Try this https://gamedev.stackexchange.com/questions/114522/how-can-i-launch-a-gameobject-at-a-target-if-i-am-given-everything-except-for-it

            var diff = targetPosition - sourcePosition;
            Vector3 directionXZ = diff.XZ();
            directionXZ.Normalize();


            var v2 = projectileSpeed * projectileSpeed;
            var v4 = projectileSpeed * projectileSpeed * projectileSpeed * projectileSpeed;

            var y = diff.Y;
            var x = diff.XZ2().Length;

            var g = 10f; //TODO: Set gravity

            var root = v4 - g * (g * x * x + 2 * y * v2);

            if (root < 0) {
                //TODO: No solution, cant do
                lowTime = 0;
                lowVector = Vector3.Zero;
                highTime = 0;
                highVector = Vector3.Zero;
                return false;
            }

            root = (float)Math.Sqrt(root);

            float lowAngle = (float)Math.Atan2(v2 - root, g * x);
            float highAngle = (float)Math.Atan2(v2 + root, g * x);


            lowVector = (directionXZ * (float)Math.Cos(lowAngle) +
                         Vector3.UnitY * (float)Math.Sin(lowAngle)) * projectileSpeed;

            highVector = (directionXZ * (float)Math.Cos(highAngle) +
                          Vector3.UnitY * (float)Math.Sin(highAngle)) * projectileSpeed;

            lowTime = x / lowVector.XZ2().Length;
            highTime = x / highVector.XZ2().Length;


            return true;
        }

        public override PluginData SaveState() {
            var sequentialData = new SequentialPluginDataWriter();


            sequentialData.StoreNext<float>(RateOfFire);
            sequentialData.StoreNext<Vector3>(target);
            sequentialData.StoreNext<float>(horizontalOffset);
            sequentialData.StoreNext<float>(verticalOffset);
            sequentialData.StoreNext<int>(projectileType.ID);

            return sequentialData.PluginData;
        }



        protected override void OnUpdate(float timeStep) {
            base.OnUpdate(timeStep);
            if (!EnabledEffective) return;


            if (delay > 0) {
                delay -= timeStep;
                return;
            }

            //if (target == null) {
            //    //Check for target in range
            //}
            var directionXZ = Vector3.Normalize(target.XZ() - Node.Position.XZ());

            var offset = directionXZ * horizontalOffset + Vector3.UnitY * verticalOffset;
            if (GetTimesAndAngles(target,
                                  Node.Position + offset,
                                  projectileType.ProjectileSpeed, 
                                  out float lowTime, 
                                  out Vector3 lowVector, 
                                  out float highTime,
                                  out Vector3 highVector)) {

                projectileType.SpawnProjectile(level,level.Scene, Node.Position + offset, lowVector);

                projectileType.SpawnProjectile(level, level.Scene, Node.Position + offset, highVector);

            }

            delay = 60 / RateOfFire;
        }

        
    }
}
