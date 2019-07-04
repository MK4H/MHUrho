using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Priority_Queue;
using Urho;

namespace MHUrho.PathFinding.AStar
{
    public class BuildingNode : Node, IBuildingNode
    {
		public override NodeType NodeType => NodeType.Building;

		public IBuilding Building { get; }

		public object Tag { get; private set; }

		public bool IsRemoved { get; private set; } = false;

		readonly List<INode> incomingEdges;

		public BuildingNode(IBuilding building, 
							Vector3 position,
							object tag,
							AStarAlg aStar)
			: base(aStar, position)
		{
			this.Building = building;
			this.Tag = tag;
			incomingEdges = new List<INode>();
			AStar.GetTileNode(position).AddNodeOnThisTile(this);
		}

		public override int GetHashCode()
		{
			return Building.ID;
		}

		public override void ProcessNeighbours(Node source,
												FastPriorityQueue<Node> priorityQueue,
												List<Node> touchedNodes,
												Node targetNode,
												NodeDistCalculator distCalc,
												ref double minDistToTarget)
		{
			State = NodeState.Closed;
			foreach (var neighbour in outgoingEdges) {
				ProcessNeighbour(neighbour.Key,
								priorityQueue,
								touchedNodes,
								targetNode,
								distCalc,
								neighbour.Value,
								ref minDistToTarget);
			}
		}

		public override IEnumerable<Waypoint> GetWaypoints(NodeDistCalculator distCalc)
		{
			return new[] { new Waypoint(this,
										Time - PreviousNode.Time,
										PreviousNode.GetMovementTypeToNeighbour(this))};
		}

		public override MovementType GetMovementTypeToNeighbour(Node neighbour)
		{
			return outgoingEdges[neighbour];
		}

		public void Remove()
		{
			//Enumerates over copy of incoming edges
			// because with every RemoveEdge, the source calls RemoveAsTarget on this node
			// which changes the incomingEdges list
			foreach (var source in incomingEdges.ToArray()) {
				source.RemoveEdge(this);
			}

			AStar.GetTileNode(Position).RemoveNodeOnThisTile(this);
			IsRemoved = true;
		}

		public override void Accept(INodeVisitor visitor, INode target, MovementType movementType)
		{
			target.Accept(visitor, this, movementType);
		}

		public override void Accept(INodeVisitor visitor, ITileNode source, MovementType movementType)
		{
			visitor.Visit(source, this, movementType);
		}

		public override void Accept(INodeVisitor visitor, IBuildingNode source, MovementType movementType)
		{
			visitor.Visit(source, this, movementType);
		}

		public override void Accept(INodeVisitor visitor, ITempNode source, MovementType movementType)
		{
			visitor.Visit(source, this, movementType);
		}

		protected override void AddedAsTarget(Node source)
		{
			incomingEdges.Add(source);
		}

		protected override void RemovedAsTarget(Node source)
		{
			incomingEdges.Remove(source);
		}
	}
}
