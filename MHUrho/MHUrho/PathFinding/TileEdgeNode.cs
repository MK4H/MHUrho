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

		IDictionary<AStarNode, MovementType> outgoingEdges;

		public TileEdgeNode(TileNode tile1, TileNode tile2, AStar aStar)
			:base(aStar)
		{
			this.Tile1 = tile1;
			this.Tile2 = tile2;
			outgoingEdges = new Dictionary<AStarNode, MovementType>();
			FixHeight();
		}

		public void FixHeight()
		{
			Position = Map.GetBorderBetweenTiles(Tile1.Tile, Tile2.Tile);
		}

		public override void ProcessNeighbours(FastPriorityQueue<AStarNode> priorityQueue,
												List<AStarNode> touchedNodes,
												AStarNode targetNode,
												GetTime getTimeBetweenNodes,
												Func<AStarNode, float> heuristic)
		{
			throw new NotImplementedException();
		}

		public override bool IsItThisNode(Vector3 point)
		{
			return point.IsNear(Position, 0.001f);
		}

		public override Waypoint GetWaypoint()
		{
			return new Waypoint(Position, Time - previousNode.Time, previousNode.GetMovementTypeToNeighbour(this));
		}


		public override TileNode GetTileNode()
		{
			return Tile1;
		}

		public override void AddNeighbour(AStarNode neighbour, MovementType movementType)
		{
			outgoingEdges.Add(neighbour, movementType);
		}

		public override bool RemoveNeighbour(AStarNode neighbour)
		{
			return outgoingEdges.Remove(neighbour);
		}

		public override MovementType GetMovementTypeToNeighbour(AStarNode neighbour)
		{
			return outgoingEdges[neighbour];
		}

		
	}
}
