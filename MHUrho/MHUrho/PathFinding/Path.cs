using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using MHUrho.PathFinding.AStar;
using Urho;


namespace MHUrho.PathFinding
{


	public class Path : IEnumerable<Waypoint> {


		public Waypoint TargetWaypoint {
			get => waypoints[targetWaypointIndex];
			set => waypoints[targetWaypointIndex] = value;
		} 

		public bool Finished => targetWaypointIndex == waypoints.Count;

		Waypoint PreviousWaypoint {
			get => waypoints[previousWaypointIndex];
			set => waypoints[previousWaypointIndex] = value;
		}

		readonly List<Waypoint> waypoints;

		int targetWaypointIndex;

		Vector3 currentPosition;

		/// <summary>
		/// Original time to TargetWaypoint on the call of <see cref="WaypointReached(INodeDistCalculator)"/>
		/// </summary>
		float originalTime;
		int previousWaypointIndex = 0;

		readonly IMap map;

		protected Path(IMap map) {
			this.waypoints = new List<Waypoint>();
			this.map = map;
		}

		protected Path(List<Waypoint> waypoints, IMap map) {
			this.waypoints = waypoints;
			targetWaypointIndex = 1;
			this.currentPosition = waypoints[0].Position;
			originalTime = TargetWaypoint.TimeToWaypoint;
			this.map = map;
		}


		public StPath Save() {
			var storedPath = new StPath();

			foreach (var waypoint in waypoints) {
				storedPath.Waypoints.Add(waypoint.ToStWaypoint());
			}

			return storedPath;
		}

		public static Path Load(StPath storedPath, ILevelManager level) {
			var newPath = new Path(level.Map);
			foreach (var waypoint in storedPath.Waypoints) {
				newPath.waypoints.Add(new Waypoint(waypoint, level));
			}
			return newPath;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="waypoints">List of waypoints, where waypoints[0] is the current Position with time 0 and MovementType.None and there is at least one more waypoint as a target</param>
		/// <returns></returns>
		public static Path CreateFrom(List<Waypoint> waypoints, IMap map) {
			return new Path(waypoints, map);
		}



		

		public static Path FromTo(	Vector3 source, 
									INode target, 
									IMap map, 
									INodeDistCalculator nodeDistCalculator)
		{

			return map.PathFinding.FindPath(source, target, nodeDistCalculator);
		}


		/// <summary>
		/// Updates the current position of the entity on the path.
		/// </summary>
		/// <param name="newPosition">New position on the path.</param>
		/// <param name="secondsFromLastUpdate">Time since the last update in seconds.</param>
		/// <param name="nodeDistCalculator">Calculator to recalculate the time to next node.</param>
		/// <returns>True if the path is still viable, false if the path cannot be followed.</returns>
		public bool Update(Vector3 newPosition, float secondsFromLastUpdate, INodeDistCalculator nodeDistCalculator)
		{
			switch (TargetWaypoint.MovementType) {
				case MovementType.None:
					return false;
				case MovementType.Teleport:
					//NOTHING
					break;
				default: /*Linear movement and unknown*/
					//Default to linear movement
					currentPosition = newPosition;
					break;
			}

			INode prevNode = waypoints[previousWaypointIndex].Node;
			INode targetNode = TargetWaypoint.Node;

			if (previousWaypointIndex != 0) {
				//When testing the ability to go to a node, we need to test it between two non temp nodes
				// so if prevNode is temp, we need to find the previous node
				// if there is no previous node, then we are starting the movement and can default to is able to pass
				INode testPrevNode = waypoints[previousWaypointIndex].Node;
				//Same as prevNode, we need to test the ability to pass between two non temp nodes
				INode testTargetNode = targetNode;
				if (testPrevNode.NodeType == NodeType.Temp || testTargetNode.NodeType == NodeType.Temp)
				{
					if (testPrevNode.NodeType == NodeType.Temp) {
						//We tested that previousWaypointIndex != 0
						testPrevNode = waypoints[previousWaypointIndex - 1].Node;
					}

					if (testTargetNode.NodeType == NodeType.Temp && targetWaypointIndex - 1 < waypoints.Count) {
						testTargetNode = waypoints[targetWaypointIndex + 1].Node;
					}

					//Cannot pass
					if (!nodeDistCalculator.GetTime(testPrevNode, testTargetNode, TargetWaypoint.MovementType, out var time)) {
						return false;
					}
				}
			}
		

			if (nodeDistCalculator.GetTime(prevNode, targetNode, TargetWaypoint.MovementType, out var newTime)) {
				//Still can move to TargetWaypoint

				//Scale the remaining time if the time from previous waypoint to targetWaypoint changed
				float remainingTime = TargetWaypoint.TimeToWaypoint * (newTime / originalTime);
				//Save the current time from waypoint to waypoint to scale in the next update
				originalTime = newTime;
				//Set the remaining time with the timeStep subtracted
				TargetWaypoint = TargetWaypoint.WithTimeToWaypointSet(remainingTime - secondsFromLastUpdate);
				return true;
			}
			//Can no longer move to target position
			return false;
		}

		public bool IsWaypointReached()
		{
			return TargetWaypoint.TimeToWaypoint <= 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>If there was next waypoint to target, or this was the end</returns>
		public bool WaypointReached(INodeDistCalculator nodeDistCalculator)
		{
			currentPosition = TargetWaypoint.Position;
			TargetWaypoint = TargetWaypoint.WithTimeToWaypointSet(0.0f);
			//If there is no next waypoint
			if (!MoveNext()) {
				targetWaypointIndex = waypoints.Count;
				//End of the path
				return false;
			}

			if (!nodeDistCalculator.GetTime(PreviousWaypoint.Node, TargetWaypoint.Node, TargetWaypoint.MovementType, out float newTimeToWaypoint))
			{
				//Cant get to the waypoint, path changed, end of the current path
				return false;
			}

			TargetWaypoint = TargetWaypoint.WithTimeToWaypointSet(newTimeToWaypoint);
			originalTime = newTimeToWaypoint;
			return true;
		}

		/// <summary>
		/// Enumerates current position and the waypoints that were not yet reached
		/// </summary>
		/// <returns></returns>
		public IEnumerator<Waypoint> GetEnumerator()
		{
			return GetEnumerator(Vector3.Zero);
		}

		public IEnumerator<Waypoint> GetEnumerator(Vector3 offset)
		{
			for (int i = 0; i < waypoints.Count; i++) {
				yield return waypoints[i].WithOffset(offset);
			}
		}
		/// <summary>
		/// Gets the rest of the path, beginning with the current position and
		/// continuing with the remaining waypoints in the path
		/// </summary>
		/// <returns>The rest of the path beginning with the current position</returns>
		public IEnumerable<Waypoint> GetRestOfThePath()
		{
			return GetRestOfThePath(Vector3.Zero);
		}

		/// <summary>
		/// Gets the rest of the path, beginning with the current position,
		/// all positions offseted by <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">Offset of every position along the path</param>
		/// <returns>The remaining part of the path, begining with the current position, all offseted by <paramref name="offset"/></returns>
		public IEnumerable<Waypoint> GetRestOfThePath(Vector3 offset)
		{
			yield return new Waypoint(new TempNode(currentPosition, map), 0, MovementType.Linear).WithOffset(offset);
			for (int i = targetWaypointIndex; i < waypoints.Count; i++) {
				yield return waypoints[i].WithOffset(offset);
			}
		}

		/// <summary>
		/// Enumerates the waypoints that were not yet reached
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public INode GetTarget() {
			return waypoints[waypoints.Count - 1].Node;
		}

		bool MoveNext()
		{
			if (targetWaypointIndex < waypoints.Count - 1) {
				previousWaypointIndex++;
				targetWaypointIndex++;
				return true;
			}
			return false;
		}

	}
}