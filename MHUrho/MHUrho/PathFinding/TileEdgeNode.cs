using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using Priority_Queue;
using Urho;

namespace MHUrho.PathFinding
{
    public class TileEdgeNode : AStarNode,  ITileEdgeNode
    {
		public TileNode Tile1 { get; private set; }
		public TileNode Tile2 { get; private set; }

		public override NodeType NodeType => NodeType.TileEdge;

		public TileEdgeNode(TileNode tile1, TileNode tile2, AStar aStar)
			:base(aStar)
		{
			this.Tile1 = tile1;
			this.Tile2 = tile2;
			FixHeight();
		}

		public void FixHeight()
		{
			Position = Map.GetBorderBetweenTiles(Tile1.Tile, Tile2.Tile);
		}

		public override void ProcessNeighbours(AStarNode source,
												FastPriorityQueue<AStarNode> priorityQueue,
												List<AStarNode> touchedNodes,
												AStarNode targetNode,
												AStarNodeDistCalculator distCalc,
												Func<Vector3, float> heuristic)
		{
			if (distCalc.GetTime(source, this, out float time)) {
				float newTime = source.Time + time;
				if (State != NodeState.Untouched) {
					if (newTime > Time) {
						return;
					}
				}
				else {
					State = NodeState.Opened;
					touchedNodes.Add(this);
				}

				Time = newTime;
				PreviousNode = source;

				ProcessNeighbour(Tile1, priorityQueue, touchedNodes, targetNode, distCalc, heuristic);
				ProcessNeighbour(Tile2, priorityQueue, touchedNodes, targetNode, distCalc, heuristic);

				foreach (var neighbour in outgoingEdges.Keys) {
					ProcessNeighbour(neighbour, priorityQueue, touchedNodes, targetNode, distCalc, heuristic);
				}
			}
		}


		public override Waypoint GetWaypoint()
		{
			return new Waypoint(this, Time - PreviousNode.Time, PreviousNode.GetMovementTypeToNeighbour(this));
		}


		public override MovementType GetMovementTypeToNeighbour(AStarNode neighbour)
		{
			//Movement between tiles is always linear, to other neighbours, check what they were added with
			return outgoingEdges.TryGetValue(neighbour, out MovementType value) ? value : MovementType.Linear;
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

		public ITileNode GetOtherSide(ITileNode source)
		{
			if (source == Tile1) {
				return Tile2;
			}

			if (source == Tile2) {
				return Tile1;
			}

			throw new ArgumentException("Source tileNode was not one of the ends of this edge", nameof(source));
			
		}
	}
}
