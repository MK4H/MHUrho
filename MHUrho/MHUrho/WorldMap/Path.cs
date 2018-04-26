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


namespace MHUrho.WorldMap
{


	public class Path : IEnumerable<Waypoint> {


		public Waypoint TargetWaypoint {
			get => waypoints[targetWaypointIndex];
			set => waypoints[targetWaypointIndex] = value;
		} 

		public bool Finished => targetWaypointIndex == waypoints.Count;

		private readonly List<Waypoint> waypoints;

		private int targetWaypointIndex;

		private Vector3 currentPosition;

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

		public static Path Load(StPath storedPath) {
			var newPath = new Path();
			foreach (var waypoint in storedPath.Waypoints) {
				newPath.waypoints.Add(new Waypoint(waypoint));
			}
			return newPath;
		}

		public static Path CreateFrom(Vector3 currentPosition,List<Waypoint> waypoints) {
			return new Path(currentPosition, waypoints);
		}



		

		public static Path FromTo(	Vector2 source, 
									ITile target, 
									Map map, 
									CanGoToNeighbour canPass,
									GetMovementSpeed getMovementSpeed)
		{

			return map.PathFinding.FindPath(source, target, canPass, getMovementSpeed);
		}


		public void Update(Vector3 newPosition, float secondsFromLastUpdate)
		{
			float speed = (newPosition - currentPosition).Length / secondsFromLastUpdate;
			float distToTarget = (TargetWaypoint.Position - newPosition).Length;
			TargetWaypoint = new Waypoint(TargetWaypoint.Position, distToTarget / speed);

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
			TargetWaypoint = new Waypoint(TargetWaypoint.Position, 0f);
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
			yield return new Waypoint(currentPosition, 0);
			for (int i = targetWaypointIndex; i < waypoints.Count; i++) {
				yield return waypoints[i];
			}
		}

		public IEnumerator<Waypoint> GetEnumerator(Vector3 offset)
		{
			yield return new Waypoint(currentPosition + offset, 0);
			for (int i = targetWaypointIndex; i < waypoints.Count; i++) {
				yield return new Waypoint(waypoints[i].Position + offset,waypoints[i].TimeToWaypoint);
			}
		}

		/// <summary>
		/// Enumerates the waypoints that were not yet reached
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}


		public ITile GetTarget(IMap map) {
			return map.GetContainingTile(waypoints[waypoints.Count - 1].Position);
		}

	}
}