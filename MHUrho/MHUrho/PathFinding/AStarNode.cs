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

	

	public abstract class AStarNode : FastPriorityQueueNode, IProcessingNode, INode {

		public abstract NodeType NodeType { get; }

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

		public abstract void ProcessNeighbours(FastPriorityQueue<AStarNode> priorityQueue,
											List<AStarNode> touchedNodes,
											AStarNode targetNode,
											GetTime getTimeBetweenNodes,
											Func<Vector3, float> heuristic);



		public abstract bool IsItThisNode(Vector3 point);

		/// <summary>
		/// Returns waypoints to get from <see cref="previousNode"/> to this node
		/// </summary>
		/// <returns>Returns waypoints to get from <see cref="previousNode"/> to this node</returns>
		public abstract Waypoint GetWaypoint();

		public abstract TileNode GetTileNode();

		public override string ToString()
		{
			return $"Center={Position}, Time={Time}, Heur={heuristic}";
		}

		public abstract void AddNeighbour(AStarNode neighbour, MovementType movementType);

		public abstract bool RemoveNeighbour(AStarNode neighbour);

		public abstract MovementType GetMovementTypeToNeighbour(AStarNode neighbour);

		protected void ProcessNeighbour(AStarNode neighbour,
										FastPriorityQueue<AStarNode> priorityQueue,
										List<AStarNode> touchedNodes,
										AStarNode targetNode,
										GetTime getTime,
										Func<Vector3, float> getHeuristic)
		{
			IProcessingNode neighbourAsP = neighbour;
			//If already opened or closed
			if (neighbour.state == NodeState.Closed) {
				//Already closed, either not passable or the best path there can be found
				return;

			}
			else if (neighbour.state == NodeState.Opened) {
				//if it is closer through the current sourceNode

				if (getTime(this, neighbour, out float timeToTarget)) {
					float newTime = Time + timeToTarget;
					if (newTime < neighbour.Time) {
						neighbour.Time = newTime;
						neighbour.previousNode = this;
						priorityQueue.UpdatePriority(this, neighbourAsP.Value);
					}
				}

			}
			else /*NodeState.Untouched*/{
				// Compute the heuristic for the new tile
				float heuristic = getHeuristic(neighbour.Position);

				if (!getTime(this, neighbour, out float timeToTarget)) {
					//Unit cannot pass to target node from source node
					return;
				}
				else {
					neighbourAsP.State = NodeState.Opened;
					neighbourAsP.Heuristic = heuristic;
					neighbourAsP.PreviousNode = this;
					neighbourAsP.Time = Time + timeToTarget;

					//Unit can pass through this tile, enqueue it
					priorityQueue.Enqueue(neighbour, neighbourAsP.Value);
					touchedNodes.Add(neighbour);
				}

			}
		}
	}
}
