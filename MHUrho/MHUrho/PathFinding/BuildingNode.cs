using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Priority_Queue;
using Urho;

namespace MHUrho.PathFinding
{
    public class BuildingNode : AStarNode, IBuildingNode
    {
		public override NodeType NodeType => NodeType.Building;

		public IBuilding Building { get; private set; }

		public object Tag { get; private set; }

		public bool IsRemoved { get; private set; } = false;

		readonly List<INode> incomingEdges;

		public BuildingNode(IBuilding building, 
							Vector3 position,
							object tag,
							AStar aStar)
			: base(aStar)
		{
			this.Building = building;
			this.Position = position;
			this.Tag = tag;
			incomingEdges = new List<INode>();
			AStar.GetTileNode(position).AddNodeOnThisTile(this);
		}

		public override void ProcessNeighbours(AStarNode source,
												FastPriorityQueue<AStarNode> priorityQueue,
												List<AStarNode> touchedNodes,
												AStarNode targetNode,
												AStarNodeDistCalculator distCalc,
												Func<Vector3, float> heuristic)
		{
			State = NodeState.Closed;
			foreach (var neighbour in outgoingEdges.Keys) {
				ProcessNeighbour(neighbour, priorityQueue, touchedNodes, targetNode, distCalc, heuristic);
			}
		}

		public override IEnumerable<Waypoint> GetWaypoints(AStarNodeDistCalculator distCalc)
		{
			return new[] { new Waypoint(this,
										Time - PreviousNode.Time,
										PreviousNode.GetMovementTypeToNeighbour(this))};
		}

		public override MovementType GetMovementTypeToNeighbour(AStarNode neighbour)
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

		protected override void AddedAsTarget(AStarNode source)
		{
			incomingEdges.Add(source);
		}

		protected override void RemovedAsTarget(AStarNode source)
		{
			incomingEdges.Remove(source);
		}
	}
}
