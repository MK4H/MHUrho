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

		protected readonly IDictionary<AStarNode, MovementType> outgoingEdges;

		protected IMap Map => AStar.Map;

		protected AStarNode(AStar aStar)
		{
			this.AStar = aStar;
			State = NodeState.Untouched;
			outgoingEdges = new Dictionary<AStarNode, MovementType>();
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
											AStarNodeDistCalculator distCalc,
											Func<Vector3, float> heuristic);


		/// <summary>
		/// Returns waypoints to get from <see cref="previousNode"/> to this node
		/// </summary>
		/// <returns>Returns waypoints to get from <see cref="previousNode"/> to this node</returns>
		public abstract Waypoint GetWaypoint();

		public override string ToString()
		{
			return $"Center={Position}, Time={Time}, Heur={Heuristic}";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="movementType"></param>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException"/>
		public INode CreateEdge(INode target, MovementType movementType)
		{
			if (movementType == MovementType.None) {
				throw new ArgumentException("Movement type cannot be None", nameof(movementType));
			}

			try {
				AStarNode aStarTarget = (AStarNode) target;
				outgoingEdges.Add(aStarTarget, movementType);
				aStarTarget.AddedAsTarget(this);
			}
			catch (ArgumentNullException) {
				throw new ArgumentNullException(nameof(target), "TargetNode cannot be null");
			}
			catch (ArgumentException e) {
				throw new ArgumentException("This pathfinding algorithm does not support multiple edges between the same nodes",
											nameof(target), e);
			}
			catch (InvalidCastException e) {
				throw new ArgumentException("Target node was not from the same pathfinding algorithm", nameof(target), e);
			}
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException"/>
		public INode RemoveEdge(INode target)
		{
			try {
				AStarNode aStarTarget = (AStarNode)target;
				if (!outgoingEdges.Remove(aStarTarget)) {
					throw new ArgumentException("There was no edge to the target", nameof(target));
				}

				aStarTarget.RemovedAsTarget(this);
			}
			catch (InvalidCastException e) {
				throw new ArgumentException("Target node was not from the same pathfinding algorithm", nameof(target), e);
			}
			return this;
		}

		public abstract MovementType GetMovementTypeToNeighbour(AStarNode neighbour);

		public abstract bool Accept(INodeVisitor visitor, INode target, out float time);

		public abstract bool Accept(INodeVisitor visitor, ITileNode source, out float time);

		public abstract bool Accept(INodeVisitor visitor, IBuildingNode source, out float time);

		public abstract bool Accept(INodeVisitor visitor, ITileEdgeNode source, out float time);

		public abstract bool Accept(INodeVisitor visitor, ITempNode source, out float time);

		protected void ProcessNeighbour(AStarNode neighbour,
										FastPriorityQueue<AStarNode> priorityQueue,
										List<AStarNode> touchedNodes,
										AStarNode targetNode,
										AStarNodeDistCalculator distCalc,
										Func<Vector3, float> getHeuristic)
		{
			//If already opened or closed
			if (neighbour.State == NodeState.Closed) {
				//Already closed, either not passable or the best path there can be found
				return;

			}
			else if (neighbour.State == NodeState.Opened) {
				//if it is closer through the current sourceNode

				if (distCalc.GetTime(this, neighbour, out float timeToTarget)) {
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

				if (!distCalc.GetTime(this, neighbour, out float timeToTarget)) {
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

		protected virtual void AddedAsTarget(AStarNode source)
		{
			//NOTHING
		}

		protected virtual void RemovedAsTarget(AStarNode source)
		{
			//NOTHING
		}
	}
}
