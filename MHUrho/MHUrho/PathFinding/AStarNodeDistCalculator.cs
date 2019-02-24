using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.PathFinding
{
	public abstract class AStarNodeDistCalculator : INodeDistCalculator {
		public abstract bool GetTime(INode source, INode target, out float time);

		/// <summary>
		/// Used as a heuristic for the A* algorithm
		///
		/// It has to be admissible, which means it must not overestimate the time
		/// GetMinimalAproxTime has to always be lower than the optimal path time
		///
		/// But the closer you get it to the optimal time, the faster the A* will run
		///
		/// If it is not admissible, the returned path may not be optimal and the runtime of A* may be longer
		///
		/// If the <paramref name="source"/> and <paramref name="target"/> are in the same Tile, it should return the optimal time
		/// as that will be used for the movement
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public abstract float GetMinimalAproxTime(Vector3 source, Vector3 target);
	}
}
