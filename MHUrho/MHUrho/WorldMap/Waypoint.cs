using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Storage;
using Urho;

namespace MHUrho.WorldMap
{
	public interface IPositionOnlyWaypoint {
		Vector3 Position { get; }
	}

	public interface IReadOnlyWaypoint : IPositionOnlyWaypoint {
		float TimeToWaypoint { get; }
	}

    public struct Waypoint : IReadOnlyWaypoint
    {
		public Vector3 Position { get; private set; }
		public float TimeToWaypoint { get; set; }

		public Waypoint(Vector3 position, float timeToWaypoint) {
			this.Position = position;
			this.TimeToWaypoint = timeToWaypoint;
		}

		public Waypoint(StWaypoint storedWaypoint) {
			this.Position = storedWaypoint.Position.ToVector3();
			this.TimeToWaypoint = storedWaypoint.Time;
		}

		public StWaypoint ToStWaypoint() {
			return new StWaypoint {Position = this.Position.ToStVector3(), Time = TimeToWaypoint};
		}

		public override string ToString() {
			return $"{Position}, Time: {TimeToWaypoint}";
		}
	}
}
