using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Storage;
using Urho;


namespace MHUrho.Logic
{
	/// <summary>
	/// Provides efficient storage of a path
	/// </summary>
	public class Path : IEnumerator<IntVector2>
	{
		//TODO: Temporary
		List<IntVector2> pathPoints;
		int CurrentIndex;

		//TODO: Range check
		public ITile Target { get; private set; }

		public IntVector2 Current { get { return pathPoints[CurrentIndex]; } }

		object IEnumerator.Current { get { return pathPoints[CurrentIndex]; } }

		public StPath Save() {
			var storedPath = new StPath();
			storedPath.CurrentIndex = CurrentIndex;
			var storedPathPoints = storedPath.PathPoints;

			foreach (var point in pathPoints) {
				storedPathPoints.Add(new StIntVector2 {X = point.X, Y = point.Y});
			}

			return storedPath;
		}

		public static Path Load(StPath storedPath) {
			var newPath = new Path(storedPath);
			foreach (var pathPoint in storedPath.PathPoints) {
				newPath.pathPoints.Add(new IntVector2(pathPoint.X, pathPoint.Y));
			}
			return newPath;
		}

		public Path(StPath storedPath) {
			this.CurrentIndex = storedPath.CurrentIndex;
			this.pathPoints = new List<IntVector2>();
		}

		public Path(List<IntVector2> allPathPoints, ITile target) {
			pathPoints = allPathPoints;
			CurrentIndex = -1;
			this.Target = target;
		}

		public void Dispose()
		{
			pathPoints = null;
		}

		public bool MoveNext()
		{
			CurrentIndex++;
			return CurrentIndex < pathPoints.Count;
		}

		public void Reset()
		{
			CurrentIndex = -1;
		}

		

		
	}
}