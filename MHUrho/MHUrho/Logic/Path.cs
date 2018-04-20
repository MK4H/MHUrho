using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Storage;
using MHUrho.WorldMap;
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

		protected Path(StPath storedPath) {
			this.CurrentIndex = storedPath.CurrentIndex;
			this.pathPoints = new List<IntVector2>();
		}

		protected Path(List<IntVector2> allPathPoints, Map map) {
			pathPoints = allPathPoints;
			CurrentIndex = -1;
			this.Target = map.GetTileByMapLocation(allPathPoints[allPathPoints.Count - 1]);
		}


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

		public static Path CreateFrom(List<IntVector2> wayPoints, Map map) {
			return wayPoints == null ? null : new Path(wayPoints, map);
		}

		public static Path FromTo(IntVector2 sourceCoords,
								IntVector2 targetCoords,
								Map map,
								CanGoToNeighbour canPass,
								GetMovementSpeed getMovementSpeed) {
			var wayPoints = map.PathFinding.FindPath(sourceCoords, targetCoords, canPass, getMovementSpeed);
			return wayPoints == null ? null : new Path(wayPoints, map);
		}

		

		public static Path FromTo(	ITile source, 
									ITile target, 
									Map map, 
									CanGoToNeighbour canPass,
									GetMovementSpeed getMovementSpeed) {

			return FromTo(source.MapLocation, target.MapLocation, map, canPass, getMovementSpeed);
		}



		public static Path FromTo(	IntVector2 sourceCoords,
									ITile target,
									Map map,
									CanGoToNeighbour canPass,
									GetMovementSpeed getMovementSpeed) {
			return FromTo(sourceCoords, target.MapLocation, map, canPass, getMovementSpeed);
		}

		public static Path FromTo(ITile source,
								IntVector2 targetCoords,
								Map map,
								CanGoToNeighbour canPass,
								GetMovementSpeed getMovementSpeed) {
			return FromTo(source.MapLocation, targetCoords, map, canPass, getMovementSpeed);
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