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

		public IBuilding Building { get; private set; }

		public object Tag { get; private set; }

		public bool IsRemoved { get; private set; } = false;

		readonly List<INode> incomingEdges;

		public BuildingNode(IBuilding building, 
							Vector3 position,
							object tag,
							AStarAlg aStar)
			: base(aStar)
		{
			this.Building = building;
			this.Position = position;
			this.Tag = tag;
			incomingEdges = new List<INode>();
			AStar.GetTileNode(position).AddNodeOnThisTile(this);
		}

		public override void ProcessNeighbours(Node source,
												FastPriorityQueue<Node> priorityQueue,
												List<Node> touchedNodes,
												Node targetNode,
												NodeDistCalculator distCalc)
		{
			State = NodeState.Closed;
			foreach (var neighbour in outgoingEdges.Keys) {
				ProcessNeighbour(neighbour, priorityQueue, touchedNodes, targetNode, distCalc);
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
			foreach (var source in incomingEdges) {
				source.RemoveEdge(this);
			}

			AStar.GetTileNode(Position).RemoveNodeOnThisTile(this);
			IsRemoved = true;
		}

		public override void Accept(INodeVisitor visitor, INode target)
		{
			target.Accept(visitor, this);
		}

		public override void Accept(INodeVisitor visitor, ITileNode source)
		{
			visitor.Visit(source, this);
		}

		public override void Accept(INodeVisitor visitor, IBuildingNode source)
		{
			visitor.Visit(source, this);
		}

		public override void Accept(INodeVisitor visitor, ITempNode source)
		{
			visitor.Visit(source, this);
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
