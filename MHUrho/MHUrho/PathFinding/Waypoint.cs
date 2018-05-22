using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.PathFinding
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
		public INode Node { get; private set; }

		public Vector3 Position { get; private set; }

		public float TimeToWaypoint { get; set; }

		/// <summary>
		/// Type of movement to this waypoint
		///
		/// The value is not checked, so it can store other values than only those defined by names
		/// </summary>
		public MovementType MovementType { get; private set; }

		public Waypoint(INode node, float timeToWaypoint, MovementType movementType)
		{
			this.Node = node;
			this.Position = node.Position;
			this.TimeToWaypoint = timeToWaypoint;
			this.MovementType = movementType;
		}

		public Waypoint(StWaypoint storedWaypoint, ILevelManager level)
		{
			
			this.Position = storedWaypoint.Position.ToVector3();
			this.Node = level.Map.PathFinding.GetNode(Position);
			this.TimeToWaypoint = storedWaypoint.Time;
			this.MovementType = (MovementType)storedWaypoint.MovementType;
		}

	
		public Waypoint WithTimeToWaypointSet(float newTimeToWaypoint)
		{
			return new Waypoint(Node, newTimeToWaypoint, MovementType);
		}

		public Waypoint WithTimeToWapointChanged(float deltaTimeToWaypoint)
		{
			return new Waypoint(Node, TimeToWaypoint + deltaTimeToWaypoint, MovementType);
		}

		public Waypoint WithOffset(Vector3 offset)
		{
			var waypoint = new Waypoint(Node, TimeToWaypoint, MovementType);
			waypoint.Position += offset;
			return waypoint;
		}

		public StWaypoint ToStWaypoint() {
			return new StWaypoint {Position = this.Position.ToStVector3(), Time = TimeToWaypoint, MovementType = (int)this.MovementType};
		}

		public override string ToString() {
			return $"{Position}, Time: {TimeToWaypoint}, MovementType: {MovementType}";
		}
	}
}
