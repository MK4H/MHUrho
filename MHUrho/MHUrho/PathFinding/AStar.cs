using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MHUrho.Helpers;
using MHUrho.WorldMap;
using Urho;
using MHUrho.Logic;
using Priority_Queue;




namespace MHUrho.PathFinding {



	public class AStar : IPathFindAlg {

		public IMap Map { get; private set; }

		readonly TileNode[] nodeMap;


		//Properties of one instance of the algorithm
		//Because the whole graph holds state, this algorithm cannot be run multiple times in parallel
		// so the global state here does not hurt anything
		readonly List<AStarNode> touchedNodes;
		readonly FastPriorityQueue<AStarNode> priorityQueue;
		AStarNode targetNode;
		AStarNodeDistCalculator distCalc;

		public AStar(IMap map) {
			this.Map = map;
			nodeMap = new TileNode[map.Width * map.Length];
			touchedNodes = new List<AStarNode>();
			priorityQueue = new FastPriorityQueue<AStarNode>(map.Width * map.Length / 4);

			FillNodeMap();
			map.TileHeightChanged += TileHeightChanged;
		}

		

		/// <summary>
		/// Finds the fastest path through the map from units current possition to target
		/// </summary>
		/// <param name="target">Target coordinates</param>
		/// <returns>List of IntVector2s the unit should pass through</returns>
		public Path FindPath(Vector3 source, 
							INode target, 
							INodeDistCalculator nodeDistCalculator) 
		{
			AStarNode realTargetNode = FindPathInNodes(source,
											target,
											nodeDistCalculator);

			//If path was not found, return null
			Path finalPath = realTargetNode == null
								? null
								: MakePath(source, realTargetNode);
			Reset();
			return finalPath;

		}

		public List<ITile> GetTileList(Vector3 source, 
										INode target,
										INodeDistCalculator nodeDistCalculator) {
			
			AStarNode targetNode = FindPathInNodes(source,
											target,
											nodeDistCalculator);
			//If path was not found, return null
			List<ITile> finalList = targetNode == null ? null : MakeTileList(targetNode);
			Reset();
			return finalList;
		}

		public INode GetClosestNode(Vector3 position)
		{
			return GetTileNode(position).GetClosestNode(position);
		}

		ITileNode IPathFindAlg.GetTileNode(ITile tile)
		{
			return GetTileNode(tile);
		}

		public TileNode GetTileNode(ITile tile)
		{
			return tile == null ? null : nodeMap[GetTileNodeIndex(tile.MapLocation.X, tile.MapLocation.Y)];
		}

		public TileNode GetTileNode(Vector2 position)
		{
			return GetTileNode(Map.GetContainingTile(position));
		}

		public TileNode GetTileNode(Vector3 position)
		{
			return GetTileNode(Map.GetContainingTile(position));
		}

		public IBuildingNode CreateBuildingNode(IBuilding building, Vector3 position, object tag)
		{
			return new BuildingNode(building, position, tag, this);
		}

		//		public Path GetPathToIntersection(Vector2 source,
		//										Path targetPath,
		//										CanGoToNeighbour canPassFromTo,
		//										GetMovementSpeed getMovementSpeed,
		//										float maxMovementSpeed)
		//		{
		//			Dictionary<Node, float> waypoints = GetWaypointDictionary(targetPath);

		//			Node startNode = GetTileNode(map.GetContainingTile(source));
		//			this.canPassTo = canPassFromTo;
		//			this.getMovementSpeed = getMovementSpeed;
		//			this.maxMovementSpeed = maxMovementSpeed;

		//			// Add the starting node to touched nodes, so it does not get enqued again
		//			touchedNodes.Add(startNode);



		//			//Main loop
		//			while (priorityQueue.Count != 0) {
		//				Node currentNode = priorityQueue.Dequeue();

		//				//If we hit the target, finish and return the sourceNode
		//				if (currentNode == targetNode) {
		//#if DEBUG
		//					//VisualizeTouchedNodes();
		//#endif
		//					return currentNode;
		//				}

		//				//If not finished, add untouched neighbours to the queue and touched nodes
		//				currentNode.Neighbours.Process(this);

		//				currentNode.State = NodeState.Closed;
		//			}
		//			//Did not find path
		//			return null;

		//		}

		float Heuristic(Vector3 source)
		{
			return distCalc.GetMinimalAproxTime(source.XZ(), targetNode.Position.XZ());
		}

		void TileHeightChanged(ITile tile)
		{
			GetTileNode(tile).FixHeights();
		}

		AStarNode FindPathInNodes(Vector3 source,
							INode target,
							INodeDistCalculator nodeDistCalculator) {

			AStarNode startNode = GetTileNode(source).GetClosestNode(source);
			distCalc = nodeDistCalculator as AStarNodeDistCalculator;
			if (distCalc == null) {
				throw new ArgumentException("Cannot calculate node distances with calculator for a different algorithm", nameof(nodeDistCalculator));
			}

			targetNode = target as AStarNode;
			if (targetNode == null) {
				throw new ArgumentException("Target node was not created by this pathfinding algorithm", nameof(target));
			}

			// Enque the starting node
			priorityQueue.Enqueue(startNode, 0);
			// Add the starting node to touched nodes, so it does not get enqued again
			touchedNodes.Add(startNode);

			//Main loop
			while (priorityQueue.Count != 0) {
				AStarNode currentNode = priorityQueue.Dequeue();

				//If we hit the target, finish and return the sourceNode
				if (currentNode == targetNode) {
#if DEBUG
					//VisualizeTouchedNodes();
#endif
					return currentNode;
				}

				//If not finished, add untouched neighbours to the queue and touched nodes
				currentNode.ProcessNeighbours(currentNode, priorityQueue, touchedNodes, targetNode, distCalc, Heuristic);
			}
			//Did not find path
			return null;
		}



		/// <summary>
		/// Reconstructs the path when given the last Node
		/// </summary>
		/// <param name="target">Last Node of the path</param>
		/// <returns>Path in correct order, from first point to the last point</returns>
		Path MakePath(Vector3 source, AStarNode target)
		{
			List<AStarNode> nodes = new List<AStarNode>();
			while (target != null) {
				nodes.Add(target);
				target = target.PreviousNode;
			}

			if (nodes.Count == 1) {
				return StartIsFinish(nodes[0], source);
			}

			//Reverse so that source is first and target is last
			nodes.Reverse();

			List<Waypoint> waypoints = new List<Waypoint>();
			waypoints.Add(new Waypoint(new TempNode(source), 0, MovementType.None));
			for (int i = 1; i < nodes.Count; i++) {
				waypoints.Add(nodes[i].GetWaypoint());
			}

			switch (waypoints[1].MovementType) {
				case MovementType.None:
					throw new InvalidOperationException("Waypoint on a path cannot have a MovementType of None");
				case MovementType.Teleport:
					//NOTHING
					break;
				default:
					//Default to linear movement
					Waypoint sourceWaypoint = waypoints[0];
					Waypoint firstWaypoint = waypoints[1];
					if (distCalc.GetTime(sourceWaypoint.Node, firstWaypoint.Node, out float time)) {
						waypoints[1] = firstWaypoint.WithTimeToWaypointSet(time);
					}
					else {
						waypoints[1] = firstWaypoint.WithTimeToWaypointSet(distCalc.GetMinimalAproxTime(source, firstWaypoint.Position));
					}
					
					break;
			}

			return Path.CreateFrom(waypoints);
		}


		List<ITile> MakeTileList(AStarNode target) {
			List<ITile> reversedTileList = new List<ITile>();
			ITile previousTile = null;
			for (;target != null; target = target.PreviousNode) {
				ITile targetTile = GetTileNode(target).Tile;
				if (previousTile != targetTile) {
					reversedTileList.Add(targetTile);
					previousTile = targetTile;
				}
			}

			reversedTileList.Reverse();
			return reversedTileList;
		}

		Path StartIsFinish(AStarNode target, Vector3 source)
		{
			TempNode sourceNode = new TempNode(source);
			float time;
			//Check for total equality, which would probably break the user code in getTime
			if (sourceNode.Position == target.Position) {
				time = 0.0f;
			}
			else if (!distCalc.GetTime(sourceNode, target, out time)) {
				return null;
			}
			
			List<Waypoint> waypoints = new List<Waypoint>{new Waypoint(sourceNode, 0.0f, MovementType.None),
															new Waypoint(target, time, MovementType.Linear)};
			return Path.CreateFrom(waypoints);
		}

		void Reset()
		{
			foreach (AStarNode node in touchedNodes) {
				node.Reset();
			}

			touchedNodes.Clear();
			priorityQueue.Clear();
			targetNode = null;
			distCalc = null;
		}

		void VisualizeTouchedNodes()
		{
			Map.HighlightTileList(from node in touchedNodes select GetTileNode(node).Tile, Color.Green);
		}

		void FillNodeMap()
		{
			for (int y = Map.Top; y <= Map.Bottom; y++) {
				for (int x = Map.Left; x <= Map.Right; x++) {
					nodeMap[GetTileNodeIndex(x,y)] = new TileNode(Map.GetTileByMapLocation(x, y), this);
				}
			}

			foreach (var node in nodeMap) {
				node.ConnectNeighbours();
			}
		}

		int GetTileNodeIndex(int x, int y)
		{
			return (x - Map.Left) + (y - Map.Top) * Map.Width;
		}

		TileNode GetTileNode(INode node)
		{
			return GetTileNode(node.Position);
		}

		//Dictionary<Node, float> GetWaypointDictionary(Path path)
		//{
		//	Dictionary<Node, float> waypointDict = new Dictionary<Node, float>();
		//	bool center = false;
		//	float totalTime = 0;
		//	foreach (var waypoint in path) {
		//		totalTime += waypoint.TimeToWaypoint;

		//		if (center) {
		//			Node node = GetTileNode(map.GetContainingTile(waypoint.Position));
		//			waypointDict.Add(node, waypoint.TimeToWaypoint);
		//		}

		//		center = !center;
		//	}

		//	return waypointDict;
		//}

		//bool ProcessStartForIntersection(Node startNode, Vector2 source, IDictionary<Node, float> waypointDict)
		//{
		//	if (waypointDict.TryGetValue(startNode, out float targetTimeToStartNode)) {

		//	}
		//	else {
		//		startNode.Neighbours.Process(this);

				

		//		return false;
		//	}
		//}
	}


}