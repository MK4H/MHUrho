using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Storage;
using Urho;

namespace MHUrho.WorldMap
{
	public enum MovementType { Linear, Teleport }

	public interface IPositionOnlyWaypoint {
		Vector3 Position { get; }
	}

	public interface IReadOnlyWaypoint : IPositionOnlyWaypoint {
		float TimeToWaypoint { get; }

		MovementType MovementType { get; }
	}

    public struct Waypoint : IReadOnlyWaypoint
    {
		public Vector3 Position { get; private set; }
		public float TimeToWaypoint { get; set; }

		/// <summary>
		/// Type of movement to this waypoint
		///
		/// The value is not checked, so it can store other values than only those defined by names
		/// </summary>
		public MovementType MovementType { get; private set; }

		public Waypoint(Vector3 position, float timeToWaypoint, MovementType movementType) {
			this.Position = position;
			this.TimeToWaypoint = timeToWaypoint;
			this.MovementType = movementType;
		}

		public Waypoint(StWaypoint storedWaypoint) {
			this.Position = storedWaypoint.Position.ToVector3();
			this.TimeToWaypoint = storedWaypoint.Time;
			this.MovementType = (MovementType)storedWaypoint.MovementType;
		}

		public StWaypoint ToStWaypoint() {
			return new StWaypoint {Position = this.Position.ToStVector3(), Time = TimeToWaypoint, MovementType = (int)this.MovementType};
		}

		public override string ToString() {
			return $"{Position}, Time: {TimeToWaypoint}, MovementType: {MovementType}";
		}
	}
}
