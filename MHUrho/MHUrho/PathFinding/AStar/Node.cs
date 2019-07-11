using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Priority_Queue;
using Urho;

namespace MHUrho.PathFinding.AStar
{
	public enum NodeState { Untouched, Opened, Closed };

	

	public abstract class Node : FastPriorityQueueNode, INode {

		public class EqualityComparer : IEqualityComparer<INode>
		{
			public bool Equals(INode x, INode y)
			{
				return object.ReferenceEquals(x, y);
			}

			public int GetHashCode(INode obj)
			{
				return obj.GetHashCode();
			}
		}

		public IPathFindAlg Algorithm => AStar;

		public abstract NodeType NodeType { get; }

		public Node PreviousNode { get; protected set; }


		/// <summary>
		/// Time from start to the middle of the tile
		/// </summary>
		public float Time { get; set; }

		public Vector3 Position { get; protected set; }

		public IEnumerable<INode> Neighbours => outgoingEdges.Keys;

		protected NodeState State;

		protected float Heuristic;

		protected float Value => Time + Heuristic;
	
		protected readonly AStarAlg AStar;

		protected readonly IDictionary<Node, MovementType> outgoingEdges;

		protected IMap Map => AStar.Map;

		protected Node(AStarAlg aStar, Vector3 position)
		{
			this.AStar = aStar;
			this.Position = position;
			State = NodeState.Untouched;
			outgoingEdges = new Dictionary<Node, MovementType>(new EqualityComparer());
		}

		public void Reset()
		{
			PreviousNode = null;
			Time = 0;
			State = NodeState.Untouched;
			Heuristic = 0;
		}

		public abstract void ProcessNeighbours(Node source,
											FastPriorityQueue<Node> priorityQueue,
											List<Node> touchedNodes,
											Node targetNode,
											NodeDistCalculator distCalc,
											ref double minDistToTarget);


		/// <summary>
		/// Returns waypoints to get from <see cref="previousNode"/> to this node
		/// </summary>
		/// <returns>Returns waypoints to get from <see cref="previousNode"/> to this node</returns>
		public abstract IEnumerable<Waypoint> GetWaypoints(NodeDistCalculator nodeDist);

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
				Node aStarTarget = (Node) target;
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
				Node aStarTarget = (Node)target;
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

		public abstract MovementType GetMovementTypeToNeighbour(Node neighbour);

		public abstract void Accept(INodeVisitor visitor, INode target, MovementType movementType);

		public abstract void Accept(INodeVisitor visitor, ITileNode source, MovementType movementType);

		public abstract void Accept(INodeVisitor visitor, IBuildingNode source, MovementType movementType);

		public abstract void Accept(INodeVisitor visitor, ITempNode source, MovementType movementType);

		protected void ProcessNeighbour(Node neighbour,
										FastPriorityQueue<Node> priorityQueue,
										List<Node> touchedNodes,
										Node targetNode,
										NodeDistCalculator distCalc,
										MovementType movementType,
										ref double minDistToTarget)
		{
			if (neighbour.State == NodeState.Closed) {
				//Already closed, either not passable or the best path there can be found
				return;
			}

			double distance = Vector3.Distance(neighbour.Position, targetNode.Position);
			//If the neighbor is too far out of the way from the closest path we found yet
			if (distance > minDistToTarget + AStar.Cutoff) {
				if (neighbour.State == NodeState.Untouched)
				{
					touchedNodes.Add(neighbour);
				}
				neighbour.State = NodeState.Closed;		
				return;
			}
			else if (distance < minDistToTarget) {
				minDistToTarget = distance;
			}
			

			//If Unit can pass to target node from source node
			if (distCalc.GetTime(this, neighbour, movementType, out float timeToTarget)) {
				if (neighbour.State == NodeState.Untouched) {
					// Compute the heuristic for the new node
					float heuristic = distCalc.GetMinimalAproxTime(neighbour.Position.XZ(), targetNode.Position.XZ());
					neighbour.State = NodeState.Opened;
					neighbour.Heuristic = heuristic;
					neighbour.PreviousNode = this;
					neighbour.Time = Time + timeToTarget;

					//Unit can pass through this node, enqueue it
					priorityQueue.Enqueue(neighbour, neighbour.Value);
					touchedNodes.Add(neighbour);
				}
				else if (neighbour.State == NodeState.Opened) {
					float newTime = Time + timeToTarget;
					//if it is closer through the current sourceNode
					if (newTime < neighbour.Time)
					{
						neighbour.Time = newTime;
						neighbour.PreviousNode = this;
						priorityQueue.UpdatePriority(neighbour, neighbour.Value);
					}
				}
			}
		}

		protected virtual void AddedAsTarget(Node source)
		{
			//NOTHING
		}

		protected virtual void RemovedAsTarget(Node source)
		{
			//NOTHING
		}
	}
}
