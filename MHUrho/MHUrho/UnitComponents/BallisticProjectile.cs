﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using Urho;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.UnitComponents
{
	public class BallisticProjectile : DefaultComponent
	{
		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => BallisticProjectile;

			public BallisticProjectile BallisticProjectile { get; private set; }

			public Loader() {

			}

			public static PluginData SaveState(BallisticProjectile ballisticProjectile) {
				var sequentialData = new SequentialPluginDataWriter();
				sequentialData.StoreNext(ballisticProjectile.Movement);
				sequentialData.StoreNext(ballisticProjectile.Enabled);
				return sequentialData.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePluginBase plugin, PluginData storedData) {
				var notificationReceiver = plugin as INotificationReceiver;
				if (notificationReceiver == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
				}

				var sequentialData = new SequentialPluginDataReader(storedData);
				var movement = sequentialData.GetCurrent<Vector3>();
				sequentialData.MoveNext();
				var enabled = sequentialData.GetCurrent<bool>();
				sequentialData.MoveNext();

				BallisticProjectile = new BallisticProjectile(notificationReceiver,
															level,
															movement,
															enabled);

			}

			public override void ConnectReferences(LevelManager level) {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
			}
		}


		public interface INotificationReceiver {

			void OnMovementStarted(BallisticProjectile flier);

			void OnGroundHit(BallisticProjectile flier);
		}

		public static DefaultComponents ComponentID = DefaultComponents.UnpoweredFlier;
		public static string ComponentName = nameof(BallisticProjectile);

		public override DefaultComponents ComponentTypeID => ComponentID;

		public override string ComponentTypeName => ComponentName;

		public Vector3 Movement { get; private set; }

		private INotificationReceiver notificationReceiver;

		private ILevelManager level;
		private Map Map => level.Map;

		protected BallisticProjectile(INotificationReceiver notificationReceiver,
							  ILevelManager level) {
			ReceiveSceneUpdates = true;
			this.notificationReceiver = notificationReceiver;
			this.level = level;
		}

		protected BallisticProjectile(INotificationReceiver notificationReceiver,
								 ILevelManager level,
								 Vector3 movement,
								 bool enabled) {
			ReceiveSceneUpdates = true;
			this.notificationReceiver = notificationReceiver;
			this.level = level;
			this.Movement = movement;
			this.Enabled = enabled;
		}

		public static BallisticProjectile GetInstanceFor<T>(T instancePlugin, 
													   ILevelManager level)
			where T : InstancePluginBase, INotificationReceiver
		{
			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new BallisticProjectile(instancePlugin, level);
		}

		/// <summary>
		/// Calculates the movement vectors for projectile with initial speed <paramref name="initialProjectileSpeed"/>, to go from <paramref name="sourcePosition"/> to <paramref name="targetPosition"/>
		/// 
		/// </summary>
		/// <param name="targetPosition"></param>
		/// <param name="sourcePosition"></param>
		/// <param name="initialProjectileSpeed"></param>
		/// <param name="lowTime"></param>
		/// <param name="lowVector"></param>
		/// <param name="highTime"></param>
		/// <param name="highVector"></param>
		/// <returns>True if it is possible to hit the <paramref name="targetPosition"/> with the given <paramref name="initialProjectileSpeed"/>,
		/// and the out parameters are valid, or false if it is not possible and the out params are invalid</returns>
		public static bool GetTimesAndVectorsForStaticTarget(Vector3 targetPosition,
																Vector3 sourcePosition,
																float initialProjectileSpeed,
																out float lowTime,
																out Vector3 lowVector,
																out float highTime,
																out Vector3 highVector) {
			//Source https://blog.forrestthewoods.com/solving-ballistic-trajectories-b0165523348c
			// https://en.wikipedia.org/wiki/Projectile_motion

			//TODO: Try this https://gamedev.stackexchange.com/questions/114522/how-can-i-launch-a-gameobject-at-a-target-if-i-am-given-everything-except-for-it

			var diff = targetPosition - sourcePosition;
			Vector3 directionXZ = diff.XZ();
			directionXZ.Normalize();


			var v2 = initialProjectileSpeed * initialProjectileSpeed;
			var v4 = initialProjectileSpeed * initialProjectileSpeed * initialProjectileSpeed * initialProjectileSpeed;

			var y = diff.Y;
			var x = diff.XZ2().Length;

			var g = 10f;

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
						 Vector3.UnitY * (float)Math.Sin(lowAngle)) * initialProjectileSpeed;

			highVector = (directionXZ * (float)Math.Cos(highAngle) +
						  Vector3.UnitY * (float)Math.Sin(highAngle)) * initialProjectileSpeed;

			lowTime = x / lowVector.XZ2().Length;
			highTime = x / highVector.XZ2().Length;


			return true;
		}

		public static int GetVectorsForMovingTarget(Vector3 targetPosition,
													Vector3 targetMovement,
													Vector3 sourcePosition,
													float initialProjectileSpeed,
													out Vector3 lowVector,
													out Vector3 highVector) {

			lowVector = Vector3.Zero;
			highVector = Vector3.Zero;


			double G = 10;

			double A = sourcePosition.X;
			double B = sourcePosition.Y;
			double C = sourcePosition.Z;
			double M = targetPosition.X;
			double N = targetPosition.Y;
			double O = targetPosition.Z;
			double P = targetMovement.X;
			double Q = targetMovement.Y;
			double R = targetMovement.Z;
			double S = initialProjectileSpeed;

			double H = M - A;
			double J = O - C;
			double K = N - B;
			double L = -.5f * G;

			// Quartic Coeffecients
			double c0 = L * L;
			double c1 = 2 * Q * L;
			double c2 = Q * Q + 2 * K * L - S * S + P * P + R * R;
			double c3 = 2 * K * Q + 2 * H * P + 2 * J * R;
			double c4 = K * K + H * H + J * J;

			// Solve quartic
			double[] times = new double[4];
			int numTimes = MathHelpers.SolveQuartic(c0, c1, c2, c3, c4, out times[0], out times[1], out times[2], out times[3]);

			// Sort so faster collision is found first
			Array.Sort(times);

			// Plug quartic solutions into base equations
			// There should never be more than 2 positive, real roots.
			Vector3[] solutions = new Vector3[2];
			int numSolutions = 0;

			for (int i = 0; i < numTimes && numSolutions < 2; ++i) {
				double t = times[i];
				if (t <= 0)
					continue;

				solutions[numSolutions].X = (float)((H + P * t) / t);
				solutions[numSolutions].Y = (float)((K + Q * t - L * t * t) / t);
				solutions[numSolutions].Z = (float)((J + R * t) / t);
				++numSolutions;
			}

			// Write out solutions
			if (numSolutions > 0) lowVector = solutions[0];
			if (numSolutions > 1) highVector = solutions[1];

			return numSolutions;
		}

		public static int GetVectorsForMovingTarget(IRangeTarget rangeTarget,
													Vector3 sourcePosition,
													float initialProjectileSpeed,
													out Vector3 lowVector,
													out Vector3 highVector)
		{
			if (!rangeTarget.Moving) {
				if (GetTimesAndVectorsForStaticTarget(rangeTarget.CurrentPosition,
													 sourcePosition,
													 initialProjectileSpeed,
													 out var lowTime,
													 out lowVector,
													 out var highTime,
													 out highVector)) {
					return 2;
				}

				return 0;
			}


			var waypoints = rangeTarget.GetWaypoints();
			if (!waypoints.MoveNext()) {
				lowVector = Vector3.Zero;
				highVector = Vector3.Zero;
				return 0;
			}

			Waypoint current = waypoints.Current;
			Waypoint? next = null;
			float timeToNextWaypoint = 0;
			while (waypoints.MoveNext()) {

				next = waypoints.Current;
				timeToNextWaypoint += next.Value.TimeToWaypoint;
				if (!GetTimesAndVectorsForStaticTarget(
						current.Position,
						next.Value.Position,
						initialProjectileSpeed,
						out float lowTime,
						out Vector3 dontCare1,
						out float dontCare2,
						out Vector3 dontCare3)) {
					
					//Out of range
					lowVector = Vector3.Zero;
					highVector = Vector3.Zero;
					return 0;
				}

				//Found the right two waypoints, that the projectile will hit
				if (timeToNextWaypoint > lowTime) {
					break;
				}
			}

			waypoints.Dispose();
			//Target is stationary
			if (!next.HasValue) {
				if (GetTimesAndVectorsForStaticTarget(current.Position,
													sourcePosition,
													initialProjectileSpeed,
													out var lowTime,
													out lowVector,
													out var highTime,
													out highVector)) {
					return 2;
				}

				return 0;
			}

			Vector3 targetMovement = (next.Value.Position - current.Position) / next.Value.TimeToWaypoint;

			//A position simulating a linear movement of target, so it arrives at the proper time to the next waypoint
			Vector3 fakePosition = next.Value.Position - targetMovement * timeToNextWaypoint;

			return GetVectorsForMovingTarget(fakePosition,
											targetMovement,
											sourcePosition,
											initialProjectileSpeed,
											out lowVector,
											out highVector);
		}

		public void StartFlight(Vector3 initialMovement) {
			Enabled = true;

			Movement = initialMovement;
		}

		public override PluginData SaveState()
		{
			return Loader.SaveState(this);
		}

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);

			if (!EnabledEffective) return;

			if (Map.IsInside(Node.Position)) {
				Node.Position += Movement * timeStep;
				Node.LookAt(Node.Position + Movement, Vector3.UnitY);

				Movement += (-Vector3.UnitY * 10) * timeStep;
			}
			else {
				//Stop movement
				Movement = Vector3.Zero;
				notificationReceiver.OnGroundHit(this);
			}
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			AddedToEntity(typeof(BallisticProjectile), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			return RemovedFromEntity(typeof(BallisticProjectile), entityDefaultComponents);
		}
	}
}
