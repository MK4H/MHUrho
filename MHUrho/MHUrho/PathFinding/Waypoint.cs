using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace MHUrho.PathFinding
{
	public enum MovementType { None, Linear, Teleport }

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

		public Vector3 Position => Node.Position + offset;

		public float TimeToWaypoint { get; set; }

		Vector3 offset;

		/// <summary>
		/// Type of movement to this waypoint
		///
		/// The value is not checked, so it can store other values than only those defined by names
		/// </summary>
		public MovementType MovementType { get; private set; }

		public Waypoint(INode node, float timeToWaypoint, MovementType movementType)
			:this(node, timeToWaypoint, movementType, Vector3.Zero)
		{ }

		public Waypoint(INode node, float timeToWaypoint, MovementType movementType, Vector3 offset)
		{
			this.Node = node;
			this.TimeToWaypoint = timeToWaypoint;
			this.MovementType = movementType;
			this.offset = offset;
		}

		public Waypoint(StWaypoint storedWaypoint, ILevelManager level)
		{
			Vector3 position = storedWaypoint.Position.ToVector3();
			this.offset = storedWaypoint.Offset.ToVector3();
			this.Node = storedWaypoint.Temporary ? 
							level.Map.PathFinding.CreateTempNode(position) :
							level.Map.PathFinding.GetClosestNode(position);
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
			return new Waypoint(Node, TimeToWaypoint, MovementType);
		}

		public StWaypoint ToStWaypoint()
		{
			return new StWaypoint
					{
						Position = this.Position.ToStVector3(),
						Offset = this.offset.ToStVector3(),
						Time = TimeToWaypoint,
						MovementType = (int) this.MovementType,
						Temporary = Node.NodeType == NodeType.Temp
					};
		}

		public override string ToString() {
			return $"{Position}, Time: {TimeToWaypoint}, MovementType: {MovementType}";
		}
	}
}
