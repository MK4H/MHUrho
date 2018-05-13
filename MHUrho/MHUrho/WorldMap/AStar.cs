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




namespace MHUrho.WorldMap {

	/// <summary>
	/// Predicate, if it is possible to go from source to the NEIGHBOURING target
	/// </summary>
	/// <param name="source">source tile</param>
	/// <param name="target">NEIGHBOUR of source</param>
	/// <returns>if it is possible to go from source to target</returns>
	public delegate bool CanGoToNeighbour(ITile source, ITile target);

	/// <summary>
	/// Gets movements speed across that tile as a multiplier of base movementSpeed
	///
	/// Get the movementSpeed across the <paramref name="across"/> tile, while going straight
	/// from <paramref name="from"/> to <paramref name="to"/>
	///
	///<paramref name="from"/> MUST BE IN THE <paramref name="across"/> tile
	/// 
	/// HAS TO BE BETWEEN 1 and 5000
	/// </summary>
	/// <param name="across"></param>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <returns></returns>
	public delegate float GetMovementSpeed(ITile across, Vector3 from, Vector3 to);

	public class AStar : IPathFindAlg {

		

		struct Edge {
			public Node Source;
			/// <summary>
			/// If there is no Edge, target will be null
			/// </summary>
			public Node Target;

			public Vector3 EdgeCenter;
			public float SourceToCenterDist;
			public float CenterToTargetDist;

			public Edge(Node source, Node target, IMap map) {
				this.Source = source;
				this.Target = target;
				EdgeCenter = new Vector3();
				SourceToCenterDist = 0;
				CenterToTargetDist = 0;
				FixHeight(map);
			}

			public float GetValue(GetMovementSpeed speedGetter) {
				return SourceToCenterDist / speedGetter(Source.Tile, Source.Center, EdgeCenter) +
						CenterToTargetDist / speedGetter(Target.Tile, EdgeCenter, Source.Center);
			}

			public void FixHeight(IMap map)
			{
				if (Target != null) {
					EdgeCenter = map.GetBorderBetweenTiles(Source.Tile, Target.Tile);
					SourceToCenterDist = (EdgeCenter - Source.Center).Length;
					CenterToTargetDist = (Target.Center - EdgeCenter).Length;
				}
			}

			public void Process(AStar aStar)
			{
				if (Target == null) {
					//There is no edge
					return;
				}

				//If already opened or closed
				if (Target.State == NodeState.Closed) {
					//Already closed, either not passable or the best path there can be found
					return;

				}
				else if (Target.State == NodeState.Opened) {
					//if it is closer through the current sourceNode
					float newTime = Source.Time + GetValue(aStar.getMovementSpeed);
					if (newTime < Target.Time) {
						Target.Time = newTime;
						Target.PreviousNode = Source;
						aStar.priorityQueue.UpdatePriority(Target, Target.Value);
					}
				}
				else /*NodeState.Untouched*/{
					// Compute the heuristic for the new tile
					float heuristic = aStar.Heuristic(Target.Center);

					if (!aStar.canPassTo(Source.Tile, Target.Tile)) {
						//Unit cannot pass this tile
						Target.State = NodeState.Closed;
						aStar.touchedNodes.Add(Target);
					}
					else {
						Target.State = NodeState.Opened;
						Target.Heuristic = heuristic;
						Target.PreviousNode = Source;
						Target.Time = Source.Time + GetValue(aStar.getMovementSpeed);

						//Unit can pass through this tile, enqueue it
						aStar.Enqueue(Target);
					}

				}
			}



		}

		struct Neighbours {
			Edge right;
			Edge upRight;
			Edge up;
			Edge upLeft;
			Edge left;
			Edge downLeft;
			Edge down;
			Edge downRight;


			public void Process(AStar aStar) {
				right.Process(aStar);
				upRight.Process(aStar);
				up.Process(aStar);
				upLeft.Process(aStar);
				left.Process(aStar);
				downLeft.Process(aStar);
				down.Process(aStar);
				downRight.Process(aStar);
			}

			public void FixHeight(IMap map)
			{
				right.FixHeight(map);
				right.Target?.Neighbours.left.FixHeight(map);

				upRight.FixHeight(map);
				upRight.Target?.Neighbours.downLeft.FixHeight(map);

				up.FixHeight(map);
				up.Target?.Neighbours.down.FixHeight(map);

				upLeft.FixHeight(map);
				upLeft.Target?.Neighbours.downRight.FixHeight(map);

				left.FixHeight(map);
				left.Target?.Neighbours.right.FixHeight(map);

				downLeft.FixHeight(map);
				downLeft.Target?.Neighbours.upRight.FixHeight(map);

				down.FixHeight(map);
				down.Target?.Neighbours.up.FixHeight(map);

				downRight.FixHeight(map);
				downRight.Target?.Neighbours.upLeft.FixHeight(map);
			}

			public Neighbours(IMap map,
							Node source,
							Node right,
							Node upRight,
							Node up,
							Node upLeft,
							Node left,
							Node downLeft,
							Node down,
							Node downRight) {
				this.right = new Edge(source, right, map);
				this.upRight = new Edge(source, upRight, map);
				this.up = new Edge(source, up, map);
				this.upLeft = new Edge(source, upLeft, map);
				this.left = new Edge(source, left, map);
				this.downLeft = new Edge(source, downLeft, map);
				this.down = new Edge(source, down, map);
				this.downRight = new Edge(source, downRight, map);
			}

		}


		class Node : FastPriorityQueueNode {

			public NodeState State;

			/// <summary>
			/// Time from start to the middle of the tile
			/// </summary>
			public float Time;

			public float Heuristic;

			public float Value => Time + Heuristic;

			public Node PreviousNode;

			public readonly ITile Tile;

			public Vector3 Center;

			public Neighbours Neighbours;

			AStar aStar;

			public Node(ITile tile, AStar aStar) {
				this.Tile = tile;
				this.aStar = aStar;
				State = NodeState.Untouched;
				Center = tile.Center3;
			}

			public void ConnectNeighbours()
			{
				int i = 0;
				Node[] neighbours = new Node[8];
				IntVector2 myLocation = Tile.MapLocation;
				for (int y = -1; y < 2; y++) {
					for (int x = -1; x < 2; x++) {
						if (x == 0 && y == 0) {
							continue;
						}
						IntVector2 newLocation = myLocation + new IntVector2(x, y);
						ITile neighbourTile = Tile.Map.GetTileByMapLocation(newLocation);
						neighbours[i++] = aStar.GetTileNode(neighbourTile);
						
					}
				}

				Neighbours = new Neighbours(aStar.map,
											 this,
											 neighbours[4],
											 neighbours[2],
											 neighbours[1],
											 neighbours[0],
											 neighbours[3],
											 neighbours[5],
											 neighbours[6],
											 neighbours[7]);
			}

			public void FixHeights()
			{
				Center = Tile.Center3;
				Neighbours.FixHeight(Tile.Map);
			}

			public void Reset()
			{
				PreviousNode = null;
				Time = 0;
				State = NodeState.Untouched;
				Heuristic = 0;
			}

			public override string ToString() {
				return $"Center={Center}, Time={Time}, Heur={Heuristic}";
			}
		}


		enum NodeState { Untouched, Opened, Closed };

		readonly IMap map;

		readonly Node[] nodeMap;

		readonly List<Node> touchedNodes;
		readonly FastPriorityQueue<Node> priorityQueue;
		Node targetNode;
		CanGoToNeighbour canPassTo;
		GetMovementSpeed getMovementSpeed;
		float maxMovementSpeed;

		public AStar(IMap map) {
			this.map = map;
			nodeMap = new Node[map.Width * map.Length];
			touchedNodes = new List<Node>();
			priorityQueue = new FastPriorityQueue<Node>(map.Width * map.Length / 4);

			FillNodeMap();
			map.TileHeightChanged += TileHeightChanged;
		}

		

		/// <summary>
		/// Finds the fastest path through the map from units current possition to target
		/// </summary>
		/// <param name="target">Target coordinates</param>
		/// <returns>List of IntVector2s the unit should pass through</returns>
		public Path FindPath(Vector2 source, 
							ITile target, 
							CanGoToNeighbour canPassFromTo, 
							GetMovementSpeed getMovementSpeed, 
							float maxMovementSpeed) 
		{
			Node realTargetNode = FindPathInNodes(map.GetContainingTile(source),
											target,
											canPassFromTo,
											getMovementSpeed,
											maxMovementSpeed);

			Debug.Assert(realTargetNode == null || realTargetNode == targetNode);

			//If path was not found, return null
			Path finalPath = realTargetNode == null
								? null
								: MakePath(source, realTargetNode, getMovementSpeed, maxMovementSpeed);
			Reset();
			return finalPath;

		}

		public List<ITile> GetTileList(Vector2 source, 
										ITile target,
										CanGoToNeighbour canPassTo,
										GetMovementSpeed getMovementSpeed,
										float maxMovementSpeed) {
			
			Node targetNode = FindPathInNodes(map.GetContainingTile(source),
											target,
											canPassTo,
											getMovementSpeed,
											maxMovementSpeed);
			//If path was not found, return null
			List<ITile> finalList = targetNode == null ? null : MakeTileList(targetNode);
			Reset();
			return finalList;
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

//			startNode.Time = ge

//			// Enque the starting node
//			priorityQueue.Enqueue(startNode, 0);
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

		float Heuristic(Vector3 from) {
			return Vector3.Distance(from, targetNode.Center) / maxMovementSpeed;
		}

		void TileHeightChanged(ITile tile)
		{
			GetTileNode(tile).FixHeights();
		}

		Node FindPathInNodes(ITile source,
							ITile target,
							CanGoToNeighbour canPassTo,
							GetMovementSpeed getMovementSpeed,
							float maxMovementSpeed) {

			Node startNode = GetTileNode(source);
			targetNode = GetTileNode(target);
			this.canPassTo = canPassTo;
			this.getMovementSpeed = getMovementSpeed;
			this.maxMovementSpeed = maxMovementSpeed;

			// Enque the starting node
			priorityQueue.Enqueue(startNode, 0);
			// Add the starting node to touched nodes, so it does not get enqued again
			touchedNodes.Add(startNode);

			//Main loop
			while (priorityQueue.Count != 0) {
				Node currentNode = priorityQueue.Dequeue();

				//If we hit the target, finish and return the sourceNode
				if (currentNode == targetNode) {
#if DEBUG
					//VisualizeTouchedNodes();
#endif
					return currentNode;
				}

				//If not finished, add untouched neighbours to the queue and touched nodes
				currentNode.Neighbours.Process(this);

				currentNode.State = NodeState.Closed;
			}
			//Did not find path
			return null;
		}



		void Enqueue(Node node)
		{
			priorityQueue.Enqueue(node, node.Value);
			touchedNodes.Add(node);
		}

		/// <summary>
		/// Reconstructs the path when given the last Node
		/// </summary>
		/// <param name="target">Last Node of the path</param>
		/// <returns>Path in correct order, from first point to the last point</returns>
		Path MakePath(Vector2 source, Node target, GetMovementSpeed getMovementSpeed, float maxMovementSpeed) {
			List<Waypoint> reversedWaypoints = new List<Waypoint>();
			Vector3 source3 = new Vector3(source.X, map.GetTerrainHeightAt(source), source.Y);
			//If the source is the target
			if (target.PreviousNode == null) {
				return StartIsFinish(target, source3, getMovementSpeed);
			}

			while (target.PreviousNode != null) {
				Node previousNode = target.PreviousNode;
				//Waypoint through which we enter the target.Tile
				Waypoint entranceWapoint = GetBorderWaypoint(target.Tile,
															 previousNode.Tile,
															 getMovementSpeed);
				//Waypoint at the center of the target.Tile, to which we go to from the entrance
				Waypoint centerWaypoint = new Waypoint(target.Tile.Center3, 
														target.Time / maxMovementSpeed - previousNode.Time / maxMovementSpeed - entranceWapoint.TimeToWaypoint);
				reversedWaypoints.Add(centerWaypoint);
				reversedWaypoints.Add(entranceWapoint);

				target = previousNode;
			}

			/*Because we are not going to the first (now last) entrance waypoint from the center of the tile
			 * we need to recalculate it with preceding position set to param source
			*/

			Waypoint firstEntrance = reversedWaypoints[reversedWaypoints.Count - 1];
			// v = d / t
			//TODO: This movement speed is for the direction between centers, but the direction between source and center may be different
			float movementSpeed = (target.Tile.Center3 - firstEntrance.Position).Length / firstEntrance.TimeToWaypoint;
			// Correct it with the correct distance
			firstEntrance.TimeToWaypoint = (firstEntrance.Position - source3).Length / movementSpeed;

			reversedWaypoints.Reverse();
			return Path.CreateFrom(source3, reversedWaypoints);
		}


		List<ITile> MakeTileList(Node target) {
			List<ITile> reversedTileList = new List<ITile>();
			for (;target != null; target = target.PreviousNode) {
				reversedTileList.Add(target.Tile);
			}

			reversedTileList.Reverse();
			return reversedTileList;
		}

		Waypoint GetBorderWaypoint(ITile followingTile, 
									ITile precedingTile,
									GetMovementSpeed getMovementSpeed)
		{

			Vector3 borderPoint = map.GetBorderBetweenTiles(followingTile, precedingTile);
			return new Waypoint(borderPoint,
								(borderPoint - precedingTile.Center3).Length / getMovementSpeed(precedingTile, precedingTile.Center3, borderPoint));
		}

		static readonly float SinCos45 = 1 / (float)Math.Sqrt(2);

		Path StartIsFinish(Node target, Vector3 source, GetMovementSpeed getMovementSpeed)
		{
			//Handle total equality, where Normalize would produce NaN
			if (target.Tile.Center3 == source) {
				return Path.CreateFrom(source, new List<Waypoint> {new Waypoint(source, 0)});
			}


			float distance = (target.Tile.Center3 - source).Length;
			List<Waypoint> waypoints = new List<Waypoint>{new Waypoint(target.Tile.Center3, distance / getMovementSpeed(target.Tile, source, target.Tile.Center3))};
			return Path.CreateFrom(source, waypoints);
		}

		void Reset()
		{
			foreach (var node in touchedNodes) {
				node.Reset();
			}

			touchedNodes.Clear();
			priorityQueue.Clear();
			targetNode = null;
			canPassTo = null;
			getMovementSpeed = null;
			maxMovementSpeed = 0;
		}

		void VisualizeTouchedNodes()
		{
			map.HighlightTileList(from node in touchedNodes select node.Tile, Color.Green);
		}

		void FillNodeMap()
		{
			for (int y = map.Top; y <= map.Bottom; y++) {
				for (int x = map.Left; x <= map.Right; x++) {
					nodeMap[GetNodeIndex(x,y)] = new Node(map.GetTileByMapLocation(x, y), this);
				}
			}

			foreach (var node in nodeMap) {
				node.ConnectNeighbours();
			}
		}

		Node GetTileNode(ITile tile)
		{
			return tile == null ? null : nodeMap[GetNodeIndex(tile.MapLocation.X, tile.MapLocation.Y)];
		}

		int GetNodeIndex(int x, int y)
		{
			return (x - map.Left) + (y - map.Top) * map.Width;
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
	}


}