using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;
using Priority_Queue;
using Urho;

namespace MHUrho.PathFinding
{
	/// <summary>
	/// Interface of members and methods used by the A* itself
	/// </summary>
	interface IProcessingNode {
		NodeState State { get; set; }

		float Time { get; set; }

		float Heuristic { get; set; }

		float Value { get; }

		IProcessingNode PreviousNode { get; set; }

		AStarNode ThisNode { get; }

		Vector3 Position { get; }

		void Reset();

		void ProcessNeighbours(FastPriorityQueue<AStarNode> priorityQueue, 
								List<AStarNode> touchedNodes,
								AStarNode targetNode,
								GetTime getTimeBetweenNodes,
								Func<Vector3,float> heuristic);

		Waypoint GetWaypoint();

		TileNode GetTileNode();
	}
}
