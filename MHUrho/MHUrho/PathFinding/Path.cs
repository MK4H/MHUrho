using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;
using MHUrho.Helpers;
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
		/// Original time to TargetWaypoint on the call of <see cref="WaypointReached(GetTime)"/>
		/// </summary>
		float originalTime;
		int previousWaypointIndex = 0;

		protected Path() {
			this.waypoints = new List<Waypoint>();
		}

		protected Path(List<Waypoint> waypoints) {
			this.waypoints = waypoints;
			targetWaypointIndex = 1;
			this.currentPosition = waypoints[0].Position;
			originalTime = TargetWaypoint.TimeToWaypoint;
		}


		public StPath Save() {
			var storedPath = new StPath();

			foreach (var waypoint in waypoints) {
				storedPath.Waypoints.Add(waypoint.ToStWaypoint());
			}

			return storedPath;
		}

		public static Path Load(StPath storedPath, ILevelManager level) {
			var newPath = new Path();
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
		public static Path CreateFrom(List<Waypoint> waypoints) {
			return new Path(waypoints);
		}



		

		public static Path FromTo(	Vector3 source, 
									Vector3 target, 
									Map map, 
									GetTime getTime,
									GetMinimalAproxTime getMinimalTime)
		{

			return map.PathFinding.FindPath(source, target, getTime, getMinimalTime);
		}


		public bool Update(Vector3 newPosition, float secondsFromLastUpdate, GetTime getTime)
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

			if (getTime(waypoints[previousWaypointIndex].Node, TargetWaypoint.Node, out var newTime)) {
				//Still can teleport to TargetWaypoint

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
		public bool WaypointReached(GetTime getTime)
		{
			currentPosition = TargetWaypoint.Position;
			TargetWaypoint = TargetWaypoint.WithTimeToWaypointSet(0.0f);
			//If there is no next waypoint
			if (!MoveNext()) {
				targetWaypointIndex = waypoints.Count;
				//End of the path
				return false;
			}

			if (!getTime(PreviousWaypoint.Node, TargetWaypoint.Node, out float newTimeToWaypoint))
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
			yield return new Waypoint(new TempNode(currentPosition), 0, MovementType.Linear);
			for (int i = targetWaypointIndex; i < waypoints.Count; i++) {
				yield return waypoints[i];
			}
		}

		public IEnumerator<Waypoint> GetEnumerator(Vector3 offset)
		{
			yield return new Waypoint(new TempNode(currentPosition), 0, MovementType.Linear).WithOffset(offset);
			for (int i = targetWaypointIndex; i < waypoints.Count; i++) {
				yield return waypoints[i].WithOffset(offset);
			}
		}

		/// <summary>
		/// Enumerates the waypoints that were not yet reached
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}


		public Vector3 GetTarget() {
			return waypoints[waypoints.Count - 1].Position;
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