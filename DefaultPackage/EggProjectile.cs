using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using Urho;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;

namespace DefaultPackage
{
	public class EggProjectileType : ProjectileTypePluginBase {
		public float Speed { get; private set; }

		public override bool IsMyType(string typeName) {
			return typeName == "EggProjectile";
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			var speedElement = XmlHelpers.GetChild(extensionElement, "speed");
			Speed = XmlHelpers.GetFloat(speedElement);
		}

		public override ProjectileInstancePluginBase CreateNewInstance(ILevelManager level, Projectile projectile) {
			return new EggProjectileInstance(level, projectile, this);
		}

		public override ProjectileInstancePluginBase GetInstanceForLoading() {
			throw new NotImplementedException();
		}

		public override bool IsInRange(Vector3 source, IRangeTarget target) {
			return UnpoweredFlier.GetTimesAndAnglesForStaticTarget(target.CurrentPosition,
																		source, 
																		Speed,
																		out var loweTime,
																		out var lowVector,
																		out var highTime,
																		out var highVector);
		}
	}

	public class EggProjectileInstance : ProjectileInstancePluginBase, UnpoweredFlier.INotificationReceiver {

		private UnpoweredFlier flier;
		private EggProjectileType myType;

		public EggProjectileInstance(ILevelManager level, Projectile projectile, EggProjectileType myType)
			:base(level, projectile)
		{
			this.myType = myType;
			this.flier = UnpoweredFlier.GetInstanceFor(this, level);
			projectile.AddComponent(flier);
		}

		public override void SaveState(PluginDataWrapper pluginData) {
			throw new NotImplementedException();
		}

		public override void LoadState(ILevelManager level, Projectile projectile, PluginDataWrapper pluginData) {
			throw new NotImplementedException();
		}

		public override void ReInitialize(ILevelManager level) {
			
		}

		public override bool ShootProjectile(IRangeTarget target) {
			if (target.Moving) {
				return ShootMovingTarget(target);
			}

			return ShootStaticTarget(target);
		}

		public override bool ShootProjectile(Vector3 movement) {
			return false;
		}

		public void OnMovementStarted(UnpoweredFlier flier) {
			
		}

		public void OnGroundHit(UnpoweredFlier flier) {
			projectile.Despawn();
		}

		private bool ShootMovingTarget(IRangeTarget target) 
		{
			var waypoints = target.GetWaypoints().GetEnumerator();
			if (!waypoints.MoveNext()) {
				return false;
			}

			Waypoint current = waypoints.Current, next = null;
			float timeToNextWaypoint = 0;
			while (waypoints.MoveNext()) {

				next = waypoints.Current;
				timeToNextWaypoint += next.TimeToWaypoint;
				if (!UnpoweredFlier.GetTimesAndAnglesForStaticTarget(
																	 current.Position,
																	 next.Position,
																	 myType.Speed,
																	 out float lowTime,
																	 out Vector3 lowVector,
																	 out float highTime,
																	 out Vector3 highVector)) {
					//Out of range
					return false;
				}

				//Found the right two waypoints, that the projectile will hit
				if (timeToNextWaypoint > lowTime) {
					break;
				}
			}

			waypoints.Dispose();
			//Target is stationary
			if (next == null) {
				return ShootStaticTarget(target);
			}

			Vector3 targetMovement = (next.Position - current.Position) / next.TimeToWaypoint;

			//A position simulating a linear movement of target, so it arrives at the proper time to the next waypoint
			Vector3 fakePosition = next.Position - targetMovement * timeToNextWaypoint;

			int numSolutions = UnpoweredFlier.GetTimesAndAnglesForMovingTarget(
													fakePosition,
													targetMovement,
													projectile.Position,
													myType.Speed,
													out Vector3 finalLowVector,
													out Vector3 finalHighVector);

			if (numSolutions >= 1) {
				flier.StartFlight(finalLowVector);
				return true;
			}

			return false;
		}

		private bool ShootStaticTarget(IRangeTarget target) {
			if (UnpoweredFlier.GetTimesAndAnglesForStaticTarget(
									target.CurrentPosition,
									projectile.Position,
									myType.Speed,
									out var lowTime,
									out var lowVector,
									out var highTime,
									out var highVector)) {

				flier.StartFlight(lowVector);
				return true;
			}

			return false;
		}
	}

}
