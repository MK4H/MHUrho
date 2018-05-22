using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Priority_Queue;
using Urho;

namespace MHUrho.PathFinding
{
	public enum NodeState { Untouched, Opened, Closed };

	

	public abstract class AStarNode : FastPriorityQueueNode, INode {

		public abstract NodeType NodeType { get; }

		public AStarNode PreviousNode { get; protected set; }


		/// <summary>
		/// Time from start to the middle of the tile
		/// </summary>
		public float Time { get; set; }

		public Vector3 Position { get; protected set; }

		protected NodeState State;

		protected float Heuristic;

		protected float Value => Time + Heuristic;
	
		protected readonly AStar AStar;

		protected IMap Map => AStar.Map;

		protected AStarNode(AStar aStar)
		{
			this.AStar = aStar;
			State = NodeState.Untouched;

		}

		public void Reset()
		{
			PreviousNode = null;
			Time = 0;
			State = NodeState.Untouched;
			Heuristic = 0;
		}

		public abstract void ProcessNeighbours(AStarNode source,
											FastPriorityQueue<AStarNode> priorityQueue,
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
			return $"Center={Position}, Time={Time}, Heur={Heuristic}";
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
			//If already opened or closed
			if (neighbour.State == NodeState.Closed) {
				//Already closed, either not passable or the best path there can be found
				return;

			}
			else if (neighbour.State == NodeState.Opened) {
				//if it is closer through the current sourceNode

				if (getTime(this, neighbour, out float timeToTarget)) {
					float newTime = Time + timeToTarget;
					if (newTime < neighbour.Time) {
						neighbour.Time = newTime;
						neighbour.PreviousNode = this;
						priorityQueue.UpdatePriority(neighbour, neighbour.Value);
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
					neighbour.State = NodeState.Opened;
					neighbour.Heuristic = heuristic;
					neighbour.PreviousNode = this;
					neighbour.Time = Time + timeToTarget;

					//Unit can pass through this tile, enqueue it
					priorityQueue.Enqueue(neighbour, neighbour.Value);
					touchedNodes.Add(neighbour);
				}

			}
		}
	}
}
