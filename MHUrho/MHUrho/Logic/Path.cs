using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Urho;


namespace MHUrho
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
        public Tile Target { get; private set; }

        public IntVector2 Current { get { return pathPoints[CurrentIndex]; } }

        object IEnumerator.Current { get { return pathPoints[CurrentIndex]; } }

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

        public Path(List<IntVector2> allPathPoints, Tile target)
        {
            pathPoints = allPathPoints;
            CurrentIndex = -1;
            this.Target = target;
        }

        
    }
}