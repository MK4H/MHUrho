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
												GetTime getTime,
												Func<Vector3, float> heuristic)
		{
			State = NodeState.Closed;
			foreach (var neighbour in outgoingEdges.Keys) {
				if (neighbour.NodeType == NodeType.TileEdge) {
					neighbour.ProcessNeighbours(this, priorityQueue, touchedNodes, targetNode, getTime, heuristic);
				}
				else {
					ProcessNeighbour(neighbour, priorityQueue, touchedNodes, targetNode, getTime, heuristic);
				}
			}
		}

		public override Waypoint GetWaypoint()
		{
			return new Waypoint(this,
								Time - PreviousNode.Time,
								PreviousNode.GetMovementTypeToNeighbour(this));
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

		public override bool Accept(INodeVisitor visitor, INode target, out float time)
		{
			return target.Accept(visitor, this, out time);
		}

		public override bool Accept(INodeVisitor visitor, ITileNode source, out float time)
		{
			return visitor.Visit(source, this, out time);
		}

		public override bool Accept(INodeVisitor visitor, IBuildingNode source, out float time)
		{
			return visitor.Visit(source, this, out time);
		}

		public override bool Accept(INodeVisitor visitor, ITileEdgeNode source, out float time)
		{
			return visitor.Visit(source, this, out time);
		}

		public override bool Accept(INodeVisitor visitor, ITempNode source, out float time)
		{
			return visitor.Visit(source, this, out time);
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
