using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Logic
{
    public class Waypoint
    {
		public Vector3 Position { get; private set; }
		public float TimeToWaypoint { get; private set; }

		public Waypoint(Vector3 position, float timeToWaypoint) {
			this.Position = position;
			this.TimeToWaypoint = timeToWaypoint;
		}
    }
}
