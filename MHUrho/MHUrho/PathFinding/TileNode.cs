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

		readonly Dictionary<TileNode, TileEdgeNode> edgeToNeighbourTile;

		readonly IDictionary<AStarNode, MovementType> outgoingEdges;

		

		public TileNode(ITile tile, AStar aStar) 
			:base(aStar)
		{
			this.Tile = tile;
			this.Position = Tile.Center3;
			nodesOnThisTile = new List<AStarNode>();
			edgeToNeighbourTile = new Dictionary<TileNode, TileEdgeNode>();
			outgoingEdges = new Dictionary<AStarNode, MovementType>();
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

					outgoingEdges.Add(tileEdge, MovementType.Linear);
					tileNeighbour.outgoingEdges.Add(tileEdge, MovementType.Linear);
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

		public override void ProcessNeighbours(FastPriorityQueue<AStarNode> priorityQueue,
												List<AStarNode> touchedNodes,
												AStarNode targetNode,
												GetTime getTime,
												Func<Vector3, float> heuristic)
		{
			foreach (var neighbour in outgoingEdges.Keys) {
				if (neighbour.NodeType == NodeType.TileEdge) {

				}
				else {
					ProcessNeighbour(neighbour, priorityQueue, touchedNodes, targetNode, getTime, heuristic);
				}
			}
		}

		public override Waypoint GetWaypoint()
		{
			return new Waypoint(Position, Time - previousNode.Time, previousNode.GetMovementTypeToNeighbour(this));
		}

		public override TileNode GetTileNode()
		{
			return this;
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

		public AStarNode GetNode(Vector3 pointOnThisTile)
		{
			foreach (var node in nodesOnThisTile) {
				if (node.IsItThisNode(pointOnThisTile)) {
					return node;
				}
			}
			return this;
		}

		public override bool IsItThisNode(Vector3 point)
		{
			return Tile.Map.GetContainingTile(point) == Tile &&
					FloatHelpers.FloatsEqual(Tile.Map.GetHeightAt(point.X, point.Z), point.Y);
		}



		
	}
}
