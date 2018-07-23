using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using Priority_Queue;
using Urho;

namespace MHUrho.PathFinding
{
    public class TileNode : AStarNode, ITileNode
    {
		public override NodeType NodeType => NodeType.Tile;

		public ITile Tile { get; private set; }

		public IEnumerable<AStarNode> NodesOnTile => nodesOnThisTile;

		readonly List<AStarNode> nodesOnThisTile;

		readonly Dictionary<ITileNode, TileEdgeNode> edgeToNeighbourTile;

		public TileNode(ITile tile, AStar aStar) 
			:base(aStar)
		{
			this.Tile = tile;
			this.Position = Tile.Center3;
			nodesOnThisTile = new List<AStarNode>();
			edgeToNeighbourTile = new Dictionary<ITileNode, TileEdgeNode>();
		}

		public void ConnectNeighbours()
		{
			int i = 0;
			List<TileNode> newTileNeighbours = new List<TileNode>();
			IntVector2 myLocation = Tile.MapLocation;
			for (int y = -1; y < 2; y++) {
				for (int x = -1; x < 2; x++) {
					if (x == 0 && y == 0) {
						continue;
					}
					IntVector2 newLocation = myLocation + new IntVector2(x, y);
					ITile neighbourTile = Tile.Map.GetTileByMapLocation(newLocation);
					if (neighbourTile != null) {
						newTileNeighbours.Add(AStar.GetTileNode(neighbourTile));
					}
					
				}
			}

			foreach (var tileNeighbour in newTileNeighbours) {
				if (!edgeToNeighbourTile.ContainsKey(tileNeighbour)) {
					TileEdgeNode tileEdge = new TileEdgeNode(this, tileNeighbour, AStar);
					edgeToNeighbourTile.Add(tileNeighbour, tileEdge);
					tileNeighbour.edgeToNeighbourTile.Add(this, tileEdge);
				}
			}
		}

		public void FixHeights()
		{
			Position = Tile.Center3;
			foreach (var tileNeighbour in edgeToNeighbourTile.Keys) {
				edgeToNeighbourTile[tileNeighbour].FixHeight();
			}
		}

		public override void ProcessNeighbours(AStarNode source,
												FastPriorityQueue<AStarNode> priorityQueue,
												List<AStarNode> touchedNodes,
												AStarNode targetNode,
												GetTime getTime,
												Func<Vector3, float> heuristic)
		{
			State = NodeState.Closed;
			foreach (var tileEdgeNode in edgeToNeighbourTile.Values) {
				tileEdgeNode.ProcessNeighbours(this, priorityQueue, touchedNodes, targetNode, getTime, heuristic);
			}

			foreach (var neighbour in outgoingEdges.Keys) {
				ProcessNeighbour(neighbour, priorityQueue, touchedNodes, targetNode, getTime, heuristic);
			}
		}

		public override Waypoint GetWaypoint()
		{
			return new Waypoint(this, Time - PreviousNode.Time, PreviousNode.GetMovementTypeToNeighbour(this));
		}

		public ITileEdgeNode GetEdgeNode(ITileNode neighbour)
		{
			try {
				return edgeToNeighbourTile[neighbour];
			}
			catch (IndexOutOfRangeException e) {
				throw new ArgumentException("Provided tile node is not a neighbour of this tile node", nameof(neighbour));
			}
		}

		public override MovementType GetMovementTypeToNeighbour(AStarNode neighbour)
		{
			//Movement type to tile edges is always linear, for other edges, look at what they were added with
			return outgoingEdges.TryGetValue(neighbour, out MovementType value) ? value : MovementType.Linear;
		}

		public AStarNode GetClosestNode(Vector3 pointOnThisTile)
		{
			AStarNode closestNode = this;
			float minDist = Vector3.Distance(Position, pointOnThisTile);
			foreach (var node in nodesOnThisTile) {
				float newDist = Vector3.Distance(node.Position, pointOnThisTile);
				if (newDist < minDist) {
					closestNode = node;
					minDist = newDist;
				}
			}

			return closestNode;

		}

		public void AddNodeOnThisTile(AStarNode aStarNode)
		{
			nodesOnThisTile.Add(aStarNode);
		}

		public bool RemoveNodeOnThisTile(AStarNode aStarNode)
		{
			return nodesOnThisTile.Remove(aStarNode);
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

	}
}
