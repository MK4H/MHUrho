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

		readonly List<Waypoint> waypoints;

		int targetWaypointIndex;

		Vector3 currentPosition;

		protected Path() {
			this.waypoints = new List<Waypoint>();
		}

		protected Path(Vector3 currentPosition, List<Waypoint> waypoints) {
			this.waypoints = waypoints;
			targetWaypointIndex = 0;
			this.currentPosition = currentPosition;
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

		public static Path CreateFrom(Vector3 currentPosition,List<Waypoint> waypoints) {
			return new Path(currentPosition, waypoints);
		}



		

		public static Path FromTo(	Vector3 source, 
									Vector3 target, 
									Map map, 
									GetTime getTime,
									GetMinimalAproxTime getMinimalTime)
		{

			return map.PathFinding.FindPath(source, target, getTime, getMinimalTime);
		}


		public void Update(Vector3 newPosition, float secondsFromLastUpdate)
		{
			switch (TargetWaypoint.MovementType) {
				case MovementType.Teleport:
					TargetWaypoint = TargetWaypoint.WithTimeToWapointChanged(-secondsFromLastUpdate);
					break;
				default:
					//Default to linear movement
					float speed = Vector3.Distance(newPosition, currentPosition) / secondsFromLastUpdate;
					float distToTarget = Vector3.Distance(newPosition, TargetWaypoint.Position);
					TargetWaypoint = TargetWaypoint.WithTimeToWaypointSet(distToTarget / speed);
					break;
			}

			currentPosition = newPosition;
		}

		public bool IsWaypointReached()
		{
			return TargetWaypoint.TimeToWaypoint <= 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>If there was next waypoint to target, or this was the end</returns>
		public bool TargetNextWaypoint()
		{
			TargetWaypoint = TargetWaypoint.WithTimeToWaypointSet(0.0f);
			if (targetWaypointIndex >= waypoints.Count - 1) {
				targetWaypointIndex = waypoints.Count;
				return false;
			}

			targetWaypointIndex++;
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

	}
}