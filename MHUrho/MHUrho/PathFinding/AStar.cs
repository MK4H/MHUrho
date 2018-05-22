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

	
	public delegate bool GetTime(INode from, INode to, out float time);

	/// <summary>
	/// Used as a heuristic for the A* algorithm
	///
	/// It has to be admissible, which means it must not overestimate the time
	/// GetMinimalAproxTime has to always be lower than the optimal path time
	///
	/// But the closer you get it to the optimal time, the faster the A* will run
	///
	/// If it is not admissible, the returned path may not be optimal and the runtime of A* may be longer
	///
	/// If the <paramref name="from"/> and <paramref name="to"/> are in the same Tile, it should return the optimal time
	/// as that will be used for the movement
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <returns></returns>
	public delegate float GetMinimalAproxTime(Vector3 from, Vector3 to);

	public class AStar : IPathFindAlg {

		

		public IMap Map { get; private set; }

		readonly TileNode[] nodeMap;

		readonly List<AStarNode> touchedNodes;
		readonly FastPriorityQueue<AStarNode> priorityQueue;
		AStarNode targetNode;
		GetTime getTime;
		GetMinimalAproxTime getMinimalTime;

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
							Vector3 target, 
							GetTime getTimeBetweenNodes, 
							GetMinimalAproxTime getMinimalTime) 
		{
			AStarNode realTargetNode = FindPathInNodes(source,
											target,
											getTimeBetweenNodes,
											getMinimalTime);

			Debug.Assert(realTargetNode == null || realTargetNode == targetNode);

			//If path was not found, return null
			Path finalPath = realTargetNode == null
								? null
								: MakePath(source, realTargetNode);
			Reset();
			return finalPath;

		}

		public List<ITile> GetTileList(Vector3 source, 
										Vector3 target,
										GetTime getTimeBetweenNodes,
										GetMinimalAproxTime getMinimalTime) {
			
			AStarNode targetNode = FindPathInNodes(source,
											target,
											getTimeBetweenNodes,
											getMinimalTime);
			//If path was not found, return null
			List<ITile> finalList = targetNode == null ? null : MakeTileList(targetNode);
			Reset();
			return finalList;
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

		float Heuristic(Vector3 from)
		{
			return getMinimalTime(from.XZ(), targetNode.Position.XZ());
		}

		void TileHeightChanged(ITile tile)
		{
			GetTileNode(tile).FixHeights();
		}

		AStarNode FindPathInNodes(Vector3 source,
							Vector3 target,
							GetTime getTimeBetweenNodes,
							GetMinimalAproxTime getMinimalTime) {

			AStarNode startNode = GetTileNode(source).GetNode(source);
			targetNode = GetTileNode(target).GetNode(target);
			this.getTime = getTimeBetweenNodes;
			this.getMinimalTime = getMinimalTime;

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
				currentNode.ProcessNeighbours(currentNode, priorityQueue, touchedNodes, targetNode, getTimeBetweenNodes, Heuristic);
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
			//Reverse so that source is first and target is last
			nodes.Reverse();

			List<Waypoint> waypoints = new List<Waypoint>();
			for (int i = 1; i < nodes.Count; i++) {
				waypoints.Add(nodes[i].GetWaypoint());
			}

			switch (waypoints[0].MovementType) {

				case MovementType.Teleport:
					//NOTHING
					break;
				default:
					//Default to linear movement
					Waypoint firstWaypoint = waypoints[0];
					waypoints[0] = new Waypoint(firstWaypoint.Position,
												getMinimalTime(source, firstWaypoint.Position),
												firstWaypoint.MovementType);
					break;
			}

			return Path.CreateFrom(source, waypoints);
		}


		List<ITile> MakeTileList(AStarNode target) {
			List<ITile> reversedTileList = new List<ITile>();
			ITile previousTile = null;
			for (;target != null; target = target.PreviousNode) {
				ITile targetTile = target.GetTileNode().Tile;
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
			//Handle total equality, where Normalize would produce NaN
			if (target.Position == source) {
				return Path.CreateFrom(source, new List<Waypoint> {new Waypoint(source, 0, MovementType.Linear)});
			}


			float distance = Vector3.Distance(target.Position, source);
			List<Waypoint> waypoints = new List<Waypoint>{new Waypoint(target.Position, getMinimalTime(source, target.Position), MovementType.Linear)};
			return Path.CreateFrom(source, waypoints);
		}

		void Reset()
		{
			foreach (AStarNode node in touchedNodes) {
				node.Reset();
			}

			touchedNodes.Clear();
			priorityQueue.Clear();
			targetNode = null;
			getTime = null;
			getMinimalTime = null;
		}

		void VisualizeTouchedNodes()
		{
			Map.HighlightTileList(from node in touchedNodes select node.GetTileNode().Tile, Color.Green);
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