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

		public override void ProcessNeighbours(AStarNode source,
												FastPriorityQueue<AStarNode> priorityQueue,
												List<AStarNode> touchedNodes,
												AStarNode targetNode,
												GetTime getTimeBetweenNodes,
												Func<Vector3, float> heuristic)
		{
			if (getTimeBetweenNodes(source, this, out float time)) {
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

				ProcessNeighbour(Tile1, priorityQueue, touchedNodes, targetNode, getTimeBetweenNodes, heuristic);
				ProcessNeighbour(Tile2, priorityQueue, touchedNodes, targetNode, getTimeBetweenNodes, heuristic);

				foreach (var neighbour in outgoingEdges.Keys) {
					ProcessNeighbour(neighbour, priorityQueue, touchedNodes, targetNode, getTimeBetweenNodes, heuristic);
				}
			}
		}


		public override Waypoint GetWaypoint()
		{
			return new Waypoint(this, Time - PreviousNode.Time, PreviousNode.GetMovementTypeToNeighbour(this));
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
			//Movement between tiles is always linear, to other neighbours, check what they were added with
			return outgoingEdges.TryGetValue(neighbour, out MovementType value) ? value : MovementType.Linear;
		}

		
	}
}
