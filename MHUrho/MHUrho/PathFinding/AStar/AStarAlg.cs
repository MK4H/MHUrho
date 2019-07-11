using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.WorldMap;
using Urho;
using MHUrho.Logic;
using Priority_Queue;




namespace MHUrho.PathFinding.AStar {

	public enum Visualization { None, TouchedNodes, FinalPath }

	public class AStarAlg : IPathFindAlg {

		public IEqualityComparer<INode> NodeEqualityComparer { get; } = new Node.EqualityComparer();

		public IMap Map { get; private set; }

		/// <summary>
		/// Cuts off any nodes that are more than <see cref="Cutoff"/>
		/// farther away in the game world than the closest we have got.
		///
		/// 50 is size of a chunk, which is enough.
		/// Can be tweeked later.
		/// </summary>
		public double Cutoff { get; } = 50;

		readonly TileNode[] nodeMap;


		//Properties of one instance of the algorithm
		//Because the whole graph holds state, this algorithm cannot be run multiple times in parallel
		// so the global state here does not hurt anything
		readonly List<Node> touchedNodes;
		readonly FastPriorityQueue<Node> priorityQueue;
		Node targetNode;
		NodeDistCalculator distCalc;

		readonly Visualization visualization;

		public AStarAlg(IMap map, Visualization visualization = Visualization.None) {
			this.Map = map;
			nodeMap = new TileNode[map.Width * map.Length];
			touchedNodes = new List<Node>();
			priorityQueue = new FastPriorityQueue<Node>(map.Width * map.Length / 4);
			this.visualization = visualization;

			FillNodeMap();
			map.TileHeightChangeNotifier.TileHeightsChangedCol += TileHeightsChanged;
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
			Node realTargetNode = FindPathInNodes(source,
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
			
			Node targetNode = FindPathInNodes(source,
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


		public ITempNode CreateTempNode(Vector3 position)
		{
			return new TempNode(position, Map);
		}

		void TileHeightsChanged(IReadOnlyCollection<ITile> tiles)
		{
			foreach (var tile in tiles) {
				GetTileNode(tile).FixHeights();
			}
		}

		Node FindPathInNodes(Vector3 source,
							INode target,
							INodeDistCalculator nodeDistCalculator) {

			Node startNode = GetTileNode(source).GetClosestNode(source);
			distCalc = nodeDistCalculator as NodeDistCalculator;
			if (distCalc == null) {
				throw new ArgumentException("Cannot calculate node distances with calculator for a different algorithm", nameof(nodeDistCalculator));
			}

			targetNode = target as Node;
			if (targetNode == null) {
				throw new ArgumentException("Target node was not created by this pathfinding algorithm", nameof(target));
			}

			// Enque the starting node
			priorityQueue.Enqueue(startNode, 0);
			// Add the starting node to touched nodes, so it does not get enqued again
			touchedNodes.Add(startNode);
			double minDistToTarget = Vector3.Distance(startNode.Position, targetNode.Position);

			//Main loop
			while (priorityQueue.Count != 0) {
				Node currentNode = priorityQueue.Dequeue();

				//If we hit the target, finish and return the sourceNode
				if (currentNode == targetNode) {

					if (visualization == Visualization.TouchedNodes) {
						VisualizeTouchedNodes(targetNode.Time);
					}
					
					return currentNode;
				}

				//If not finished, add untouched neighbours to the queue and touched nodes
				currentNode.ProcessNeighbours(currentNode, priorityQueue, touchedNodes, targetNode, distCalc,ref minDistToTarget);
			}

			if (visualization == Visualization.TouchedNodes)
			{
				VisualizeTouchedNodes(targetNode.Time);
			}
			//Did not find path
			return null;
		}



		/// <summary>
		/// Reconstructs the path when given the last Node
		/// </summary>
		/// <param name="target">Last Node of the path</param>
		/// <returns>Path in correct order, from first point to the last point</returns>
		Path MakePath(Vector3 source, Node target)
		{
			List<Node> nodes = new List<Node>();
			while (target != null) {
				nodes.Add(target);
				target = target.PreviousNode;
			}

			//Reverse so that source is first and target is last
			nodes.Reverse();

			if (visualization == Visualization.FinalPath) {
				VisualizePath(nodes);
			}

			if (nodes.Count == 1) {
				return StartIsFinish(nodes[0], source);
			}

			List<Waypoint> waypoints = new List<Waypoint>();
			waypoints.Add(new Waypoint(new TempNode(source, Map), 0, MovementType.None));
			for (int i = 1; i < nodes.Count; i++) {
				waypoints.AddRange(nodes[i].GetWaypoints(distCalc));
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
					if (distCalc.GetTime(sourceWaypoint.Node, firstWaypoint.Node, MovementType.Linear, out float time)) {
						waypoints[1] = firstWaypoint.WithTimeToWaypointSet(time);
					}
					else {
						waypoints[1] = firstWaypoint.WithTimeToWaypointSet(distCalc.GetMinimalAproxTime(source, firstWaypoint.Position));
					}
					
					break;
			}

			return Path.CreateFrom(waypoints, Map);
		}


		List<ITile> MakeTileList(Node target) {
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

		Path StartIsFinish(Node target, Vector3 source)
		{
			TempNode sourceNode = new TempNode(source, Map);
			float time;
			//Check for total equality, which would probably break the user code in getTime
			if (sourceNode.Position == target.Position) {
				time = 0.0f;
			}
			else if (!distCalc.GetTime(sourceNode, target, MovementType.Linear, out time)) {
				return null;
			}
			
			List<Waypoint> waypoints = new List<Waypoint>{new Waypoint(sourceNode, 0.0f, MovementType.None),
															new Waypoint(target, time, MovementType.Linear)};
			return Path.CreateFrom(waypoints, Map);
		}

		void Reset()
		{
			foreach (Node node in touchedNodes) {
				node.Reset();
			}

			touchedNodes.Clear();
			priorityQueue.Clear();
			targetNode = null;
			distCalc = null;
		}

		void VisualizeTouchedNodes(float finalTime)
		{
			Map.HighlightTileList(from node in touchedNodes select GetTileNode(node).Tile,
								(tile) => {
									TileNode node = GetTileNode(tile);
									float time = node.Time;
									return new Color(time / finalTime, 1 - time / finalTime, 0, 1);
								});
		}

		void VisualizePath(IReadOnlyList<Node> pathNodes)
		{
			float finalTime = pathNodes[pathNodes.Count].Time;
			Map.HighlightTileList(from node in pathNodes select GetTileNode(node).Tile,
								(tile) => {
									TileNode node = GetTileNode(tile);
									float time = node.Time;
									return new Color(time / finalTime, 1 - time / finalTime, 0, 1);
								});
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
	}


}