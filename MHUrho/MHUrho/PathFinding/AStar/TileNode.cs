using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using Priority_Queue;
using Urho;

namespace MHUrho.PathFinding.AStar
{
    public class TileNode : Node, ITileNode
    {
		public override NodeType NodeType => NodeType.Tile;

		public ITile Tile { get;  }

		public IEnumerable<Node> NodesOnTile => nodesOnThisTile;

		readonly List<Node> nodesOnThisTile;

		public TileNode(ITile tile, AStarAlg aStar) 
			:base(aStar, tile.Center3)
		{
			this.Tile = tile;
			nodesOnThisTile = new List<Node>();
		}

		public override int GetHashCode()
		{
			return Tile.MapLocation.GetHashCode();
		}

		public void ConnectNeighbours()
		{
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
				CreateEdge(tileNeighbour, MovementType.Linear);
			}
		}

		public void FixHeights()
		{
			Position = Tile.Center3;
		}

		public Vector3 GetEdgePosition(ITileNode other)
		{
			//NOTE: If this is slow, cache it at construction
			return Map.GetBorderBetweenTiles(Tile, other.Tile);
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

		public override IEnumerable<Waypoint> GetWaypoints(NodeDistCalculator nodeDist)
		{
			if (PreviousNode.NodeType == NodeType.Tile && PreviousNode.GetMovementTypeToNeighbour(this) == MovementType.Linear) {
				var borderNode = new TempNode(GetEdgePosition((ITileNode) PreviousNode), Map);
				float totalTime = Time - PreviousNode.Time;

				if (!nodeDist.GetTime(PreviousNode, borderNode, MovementType.Linear, out float firstTime)) {
					throw new ArgumentException($"Wrong {nameof(nodeDist)} implementation, does not give the same result on path building.");
				}
				return new[]
						{
							new Waypoint(borderNode, firstTime, MovementType.Linear),
							new Waypoint(this, totalTime - firstTime, MovementType.Linear)
						};
			}
			else {
				return new[] { new Waypoint(this, Time - PreviousNode.Time, PreviousNode.GetMovementTypeToNeighbour(this)) };
			}		
		}

		public override MovementType GetMovementTypeToNeighbour(Node neighbour)
		{
			//Movement type to tile edges is always linear, for other edges, look at what they were added with
			return outgoingEdges.TryGetValue(neighbour, out MovementType value) ? value : MovementType.Linear;
		}

		const double PrioritizedDistance = 0.1;
		public Node GetClosestNode(Vector3 pointOnThisTile)
		{
			Node closestNode = this;
			float minDist = Vector3.Distance(Position, pointOnThisTile);
			foreach (var node in nodesOnThisTile) {
				float newDist = Vector3.Distance(node.Position, pointOnThisTile);
				if (closestNode.NodeType == node.NodeType) {
					if (newDist < minDist)
					{
						closestNode = node;
						minDist = newDist;
					}
				}
				else if (closestNode.NodeType == NodeType.Building) {
					if (newDist + PrioritizedDistance < minDist ) {
						closestNode = node;
						minDist = newDist;
					}
				}
				else if (node.NodeType == NodeType.Building) {
					if (newDist < minDist + PrioritizedDistance)
					{
						closestNode = node;
						minDist = newDist;
					}
				}
				else {
					if (newDist < minDist)
					{
						closestNode = node;
						minDist = newDist;
					}
				}
				
			}

			return closestNode;

		}

		public void AddNodeOnThisTile(Node aStarNode)
		{
			nodesOnThisTile.Add(aStarNode);
		}

		public bool RemoveNodeOnThisTile(Node aStarNode)
		{
			return nodesOnThisTile.Remove(aStarNode);
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

	}
}
