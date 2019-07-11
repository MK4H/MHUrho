using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.PathFinding
{
	public interface INodeDistCalculator
	{
		/// <summary>
		/// Gets time it will take to get from the <paramref name="source"/> node to <paramref name="target"/> node,
		/// returns false if target cannot be reached, returns true and the time in <paramref name="time"/> if the target can be reached
		/// </summary>
		/// <param name="source">Starting node</param>
		/// <param name="target">Target node</param>
		/// <param name="movementType">Type of movement between the two nodes.</param>
		/// <param name="time">Time it will get to reach the <paramref name="target"/> node from <paramref name="source"/> node in seconds</param>
		/// <returns>True if the <paramref name="target"/> node can be reached from the <paramref name="source"/> node</returns>
		bool GetTime(INode source, INode target, MovementType movementType, out float time);
	}
}
