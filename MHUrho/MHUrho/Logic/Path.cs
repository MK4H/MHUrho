using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace MHUrho
{
    /// <summary>
    /// Provides efficient storage of a path
    /// </summary>
    public class Path : IEnumerator<Point>
    {
        //TODO: Temporary
        List<Point> PathPoints;
        int CurrentIndex;

        //TODO: Range check
        public Tile Target { get; private set; }

        public Point Current { get { return PathPoints[CurrentIndex]; } }

        object IEnumerator.Current { get { return PathPoints[CurrentIndex]; } }

        public void Dispose()
        {
            PathPoints = null;
        }

        public bool MoveNext()
        {
            CurrentIndex++;
            return CurrentIndex < PathPoints.Count;
        }

        public void Reset()
        {
            CurrentIndex = -1;
        }

        public Path(List<Point> allPathPoints, Tile target)
        {
            PathPoints = allPathPoints;
            CurrentIndex = -1;
        }

        
    }
}