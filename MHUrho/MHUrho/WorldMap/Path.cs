using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Storage;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using Urho;


namespace MHUrho.Logic
{
	/// <summary>
	/// Provides efficient storage of a path
	/// </summary>

	public class Path : IEnumerable<Waypoint>
	{
		public class PathEnumerator : IEnumerator<Waypoint> {
			public Waypoint Current {
				get {
					if (current == null) {
						//TODO: Exception text
						throw new InvalidOperationException("Enumeration after enumerator returned false from MoveNext");
					}

					return current;
				}
				private set => current = value;
			}


			object IEnumerator.Current => Current;

			public Path Path { get; private set; }

			private int currentIndex;

			private Waypoint current;

			public PathEnumerator(Path path) {
				this.Path = path;
				currentIndex = -1;
			}

			public bool MoveNext() {
				currentIndex++;
				if (currentIndex < Path.waypoints.Count) {
					Current = Path.waypoints[currentIndex];
					return true;
				}

				Current = null;
				return false;
			}

			public void Reset() {
				currentIndex = -1;
			}

			public void Dispose() {
				
			}

			public StPathEnumerator Save() {
				return new StPathEnumerator {Path = this.Path.Save(), Index = this.currentIndex};
			}
		}

		private readonly List<Waypoint> waypoints;



		protected Path() {
			this.waypoints = new List<Waypoint>();
		}

		protected Path(List<Waypoint> waypoints) {
			this.waypoints = waypoints;
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

		public static Path CreateFrom(List<Waypoint> waypoints) {
			return new Path(waypoints);
		}



		

		public static Path FromTo(	ITile source, 
									ITile target, 
									Map map, 
									CanGoToNeighbour canPass,
									GetMovementSpeed getMovementSpeed) {

			return map.PathFinding.FindPath(source, target, canPass, getMovementSpeed);
		}


		public IEnumerator<Waypoint> GetEnumerator() {
			return new PathEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public PathEnumerator GetPathEnumerator() {
			return new PathEnumerator(this);
		}

		public ITile GetTarget(IMap map) {
			return map.GetContainingTile(waypoints[waypoints.Count - 1].Position);
		}
	}
}