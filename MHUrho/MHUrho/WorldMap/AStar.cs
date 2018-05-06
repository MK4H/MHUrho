﻿using System;
using System.Collections.Generic;
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
	/// from the center of <paramref name="from"/> to the center of <paramref name="to"/>
	///
	///<paramref name="from"/> and <paramref name="to"/> MUST BE ADJACENT at least by sharing a corner
	/// 
	/// HAS TO BE BETWEEN 1 and 5000
	/// </summary>
	/// <param name="across"></param>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <returns></returns>
	public delegate float GetMovementSpeed(ITile across, ITile from, ITile to);

	public class AStar : IPathFindAlg {

		public const float MaxSpeed = 5000;

		class Node : FastPriorityQueueNode {
			IntVector2 position;

			public NodeState State { get; set; }

			public IntVector2 Position => position;

			/// <summary>
			/// COPIES Position with new X coordinate
			/// </summary>
			public int X {
				get => position.X;
				set => position = new IntVector2(value, position.Y);
			}

			/// <summary>
			/// COPIES Position with new Y coordinate
			/// </summary>
			public int Y {
				get => position.Y;
				set => position = new IntVector2(position.X, value);
			}

			/// <summary>
			/// Time from start to the middle of the tile
			/// </summary>
			public float Time { get; private set; }

			public float Heuristic { get; private set; }

			public float Value => Time + Heuristic;

			public Node PreviousNode { get; private set; }

			public ITile Tile { get; private set; }


			GetMovementSpeed getMovementSpeed;

			static readonly float fsqrt2 = (float)Math.Sqrt(2);
			static readonly double dsqrt2 = Math.Sqrt(2);

			static readonly float fsqrt2d2 = fsqrt2 / 2;
			static readonly double dsqrt2d2 = dsqrt2 / 2;

			const float half = 1f / 2;


			/// <summary>
			/// Tests if the distance through the new Previous Node is less than
			/// the old distance, if true, then sets Time and PreviousNode
			/// </summary>
			/// <param name="newPreviousNode">New possible previous node, will be tested and if its shorter distance, set as previous</param>
			/// <returns>true if the new distance is lower and was set, false if not and was not</returns>
			public bool TestAndSetDistance(Node newPreviousNode) {

				//Little optimization, because there are only two possible distances, either 1 or sqrt(2), 
				// and i always need only half of this distance, i just have two constants from which i choose
				float halfRawDistance =
					(position.X == newPreviousNode.position.X || position.Y == newPreviousNode.position.Y) ? half : fsqrt2d2;

				//newTime is always by at least RawDistance(halfRawDistance * 2) bigger than newPreviousNode.Time
				// so A* works, because heuristic is RawDistance
				float newTime =
					(halfRawDistance / getMovementSpeed(newPreviousNode.Tile, newPreviousNode.Tile, Tile) + 
						halfRawDistance / getMovementSpeed(Tile, newPreviousNode.Tile, Tile)) 
					* MaxSpeed +
					newPreviousNode.Time;

				if (newTime > Time) return false;

				Time = newTime;
				PreviousNode = newPreviousNode;
				return true;
			}


			public override string ToString() {
				return $"X={X}, Y={Y}, Time={Time}, Heur={Heuristic}";
			}

			public Node(IntVector2 position,
						Node previousNode,
						ITile tile,
						float heuristic,
						GetMovementSpeed getMovementSpeed,
						NodeState state = NodeState.Opened) {

				this.position = position;
				PreviousNode = previousNode;
				this.Tile = tile;
				this.getMovementSpeed = getMovementSpeed;
				Heuristic = heuristic;
				State = state;
				if (previousNode != null) {
					Time = float.MaxValue;
					TestAndSetDistance(previousNode);
				}
				//start node
				else {
					Time = 0;
				}
			}

		}


		enum NodeState { Opened, Closed };

		readonly IMap map;

		public AStar(IMap map) {
			this.map = map;
		}

		/// <summary>
		/// Finds the fastest path through the map from units current possition to target
		/// </summary>
		/// <param name="target">Target coordinates</param>
		/// <returns>List of IntVector2s the unit should pass through</returns>
		public Path FindPath(Vector2 source, ITile target, CanGoToNeighbour canPassTo, GetMovementSpeed getMovementSpeed) {
			Node targetNode = FindPathInNodes(source,
											target,
											canPassTo,
											getMovementSpeed);
			//If path was not found, return null
			return targetNode == null ? null : MakePath(source, targetNode, getMovementSpeed);

		}

		public List<ITile> GetTileList(Vector2 source, 
										ITile target,
										CanGoToNeighbour canPassTo,
										GetMovementSpeed getMovementSpeed) {
			Node targetNode = FindPathInNodes(source,
											target,
											canPassTo,
											getMovementSpeed);
			//If path was not found, return null
			return targetNode == null ? null : MakeTileList(targetNode);
		}

		Node FindPathInNodes(Vector2 source,
								ITile target,
								CanGoToNeighbour canPassTo,
								GetMovementSpeed getMovementSpeed) {
			Dictionary<IntVector2, Node> touchedNodes = new Dictionary<IntVector2, Node>();
			ITile sourceTile = map.GetContainingTile(source);
			IntVector2 startPos = sourceTile.MapLocation;

			Node startNode = new Node(position: startPos,
									previousNode: null,
									tile: sourceTile,
									getMovementSpeed: getMovementSpeed,
									heuristic: Heuristic(startPos, target.MapLocation));

			FastPriorityQueue<Node> priorityQueue = new FastPriorityQueue<Node>(32 + (int)Math.Ceiling(startNode.Heuristic) * 4);

			// Enque the starting node
			priorityQueue.Enqueue(startNode, 0);
			// Add the starting node to touched nodes, so it does not get enqued again
			touchedNodes.Add(startPos, startNode);

			//Main loop
			while (priorityQueue.Count != 0) {
				Node sourceNode = priorityQueue.Dequeue();

				//If we hit the target, finish and return the sourceNode
				if (sourceNode.Position == target.MapLocation) {
					return sourceNode;
				}

				//If not finished, add untouched neighbours to the queue and touched nodes
				AddNeighbours(priorityQueue, touchedNodes, sourceNode, target.MapLocation, canPassTo, getMovementSpeed);

				sourceNode.State = NodeState.Closed;
			}

			//Did not find path
			return null;
		}

		/// <summary>
		/// Enques neighbour tiles in 3 by 3 square with sourceNode as a center to the queue,
		/// with priority dependent on heuristic, sourceNode value and tileType of the neighbour
		/// </summary>
		/// <param name="queue">The queue to which neighbours are added</param>
		/// <param name="touchedNodes">Nodes already touched, held for easy fast checking of Node state</param>
		/// <param name="sourceNode">center of the square from which tiles are taken</param>
		/// <param name="target">Coordinates of the target</param>
		/// <param name="unit">The unit going through the path, needed for speed calculation through different tile types</param>
		void AddNeighbours(
			FastPriorityQueue<Node> queue,
			Dictionary<IntVector2, Node> touchedNodes,
			Node sourceNode,
			IntVector2 target,
			CanGoToNeighbour canPassTo,
			GetMovementSpeed getMovementSpeed) {

			for (int dx = -1; dx < 2; dx++) {
				for (int dy = -1; dy < 2; dy++) {
					//Dont try adding source node again
					if (dx == 0 && dy == 0)
						continue;

					IntVector2 newPosition = new IntVector2(sourceNode.X + dx, sourceNode.Y + dy);

					//Check map boundaries
					if (!map.IsInside(newPosition)) {
						continue;
					}

					//If already opened or closed
					if (touchedNodes.TryGetValue(newPosition, out Node nextNode)) {
						//Already closed, either not passable or the best path there can be found
						if (nextNode.State == NodeState.Closed)
							continue;
						//if it is closer through the current sourceNode
						if (nextNode.TestAndSetDistance(sourceNode)) {
							queue.UpdatePriority(nextNode, nextNode.Value);
						}
					}
					else {
						// Get the next tile from the map
						var newTile = map.GetTileByMapLocation(newPosition);
						// Compute the heuristic for the new tile
						float heuristic = Heuristic(newPosition, target);

						if (!canPassTo(sourceNode.Tile, newTile)) {
							//Unit cannot pass this tile
							touchedNodes.Add(
								newPosition,
								new Node(
									newPosition,
									sourceNode,
									newTile,
									heuristic,
									getMovementSpeed,
									NodeState.Closed
									)
								);
						}
						else {
							//Unit can pass through this tile, enqueue it
							Node newNode = new Node(newPosition, sourceNode, newTile, heuristic, getMovementSpeed);
							if (queue.Count == queue.MaxSize) {
								queue.Resize(queue.MaxSize * 2);
							}
							queue.Enqueue(newNode, newNode.Value);
							touchedNodes.Add(newNode.Position, newNode);
						}
					}
				}
			}
		}

		float Heuristic(IntVector2 srcPoint, IntVector2 target) {
			return IntVector2.Distance(srcPoint, target);
		}

		/// <summary>
		/// Reconstructs the path when given the last Node
		/// </summary>
		/// <param name="target">Last Node of the path</param>
		/// <returns>Path in correct order, from first point to the last point</returns>
		Path MakePath(Vector2 source, Node target, GetMovementSpeed getMovementSpeed) {
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
														target.Time / MaxSpeed - previousNode.Time / MaxSpeed - entranceWapoint.TimeToWaypoint);
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
								(borderPoint - precedingTile.Center3).Length / getMovementSpeed(precedingTile, precedingTile, followingTile));
		}

		static readonly float SinCos45 = 1 / (float)Math.Sqrt(2);

		Path StartIsFinish(Node target, Vector3 source, GetMovementSpeed getMovementSpeed)
		{
			//Handle total equality, where Normalize would produce NaN
			if (target.Tile.Center3 == source) {
				return Path.CreateFrom(source, new List<Waypoint> {new Waypoint(source, 0)});
			}

			Vector2 XZDiff = (target.Tile.Center3 - source).XZ2();
			XZDiff.Normalize();

			XZDiff /= SinCos45;
			//Make it X in {-1,0,1} and Y in {-1,0,1}
			
			IntVector2 fakeTileCoords = IntVector2.Clamp(XZDiff.RoundToIntVector2(), new IntVector2(-1,-1),new IntVector2(1,1));


			ITile fakePreviousTile = map.GetTileByTopLeftCorner(target.Tile.TopLeft + fakeTileCoords);


			float distance = (target.Tile.Center3 - source).Length;
			List<Waypoint> waypoints = new List<Waypoint>{new Waypoint(target.Tile.Center3, distance / getMovementSpeed(target.Tile, fakePreviousTile, target.Tile))};
			return Path.CreateFrom(source, waypoints);
		}

	}
}