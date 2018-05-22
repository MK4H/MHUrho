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

		IDictionary<AStarNode, MovementType> outgoingEdges;

		public BuildingNode(IBuilding building, 
							Vector3 position,
							AStar aStar)
			: base(aStar)
		{
			Building = building;
			Position = position;
			this.outgoingEdges = new Dictionary<AStarNode, MovementType>();
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

		public override bool IsItThisNode(Vector3 point)
		{
			throw new NotImplementedException();
		}



		public override Waypoint GetWaypoint()
		{
			return new Waypoint(Position,
								Time - PreviousNode.Time,
								PreviousNode.GetMovementTypeToNeighbour(this));
		}

		public override TileNode GetTileNode()
		{
			throw new NotImplementedException();
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
