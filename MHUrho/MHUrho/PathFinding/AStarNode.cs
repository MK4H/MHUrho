using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Priority_Queue;
using Urho;

namespace MHUrho.PathFinding
{
	enum NodeState { Untouched, Opened, Closed };

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

		IEnumerable<AStarNode> GetNeighbours();

		IEnumerable<Waypoint> GetWaypoints();

		TileNode GetTileNode();
	}

	public abstract class AStarNode : FastPriorityQueueNode, IProcessingNode {

		NodeState IProcessingNode.State {
			get => state;
			set => state = value;
		}
		NodeState state;

		/// <summary>
		/// Time from start to the middle of the tile
		/// </summary>
		public float Time { get; set; }


		float IProcessingNode.Heuristic {
			get => heuristic;
			set => heuristic = value;
		}
		float heuristic;

		float IProcessingNode.Value => Time + heuristic;

		IProcessingNode IProcessingNode.PreviousNode {
			get => previousNode;
			set => previousNode = value.ThisNode;
		}
		protected AStarNode previousNode;

		AStarNode IProcessingNode.ThisNode => this;

		public Vector3 Position { get; protected set; }

		protected readonly AStar AStar;

		protected IMap Map => AStar.Map;

		protected AStarNode(AStar aStar)
		{
			this.AStar = aStar;
			state = NodeState.Untouched;

		}

		void IProcessingNode.Reset()
		{
			previousNode = null;
			Time = 0;
			state = NodeState.Untouched;
			heuristic = 0;
		}

		public abstract bool IsItThisNode(Vector3 point);

		public abstract IEnumerable<AStarNode> GetNeighbours();

		/// <summary>
		/// Returns waypoints to get from <see cref="previousNode"/> to this node
		/// </summary>
		/// <returns>Returns waypoints to get from <see cref="previousNode"/> to this node</returns>
		public abstract IEnumerable<Waypoint> GetWaypoints();

		public abstract TileNode GetTileNode();

		public override string ToString()
		{
			return $"Center={Position}, Time={Time}, Heur={heuristic}";
		}

		public abstract IEnumerable<Waypoint> GetToNode(AStarNode node);

	}
}
