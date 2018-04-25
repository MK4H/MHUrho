using System;
using System.Collections.Generic;
using System.Text;
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

	public delegate float GetMovementSpeed(ITile tile);

	public class AStar : IPathFindAlg {
		private class Node : FastPriorityQueueNode {
			private IntVector2 position;

			public NodeState State { get; set; }

			public IntVector2 Position => position;

			/// <summary>
			/// COPIES Position with new X coordinate
			/// </summary>
			public int X {
				get {
					return position.X;
				}
				set {
					position = new IntVector2(value, position.Y);
				}
			}

			/// <summary>
			/// COPIES Position with new Y coordinate
			/// </summary>
			public int Y {
				get {
					return position.Y;
				}
				set {
					position = new IntVector2(position.X, value);
				}
			}

			/// <summary>
			/// Distance from start to the middle of the tile
			/// </summary>
			public float Distance { get; private set; }

			public float Heuristic { get; private set; }

			public float Value => Distance + Heuristic;

			public Node PreviousNode { get; private set; }

			public ITile Tile { get; private set; }


			private GetMovementSpeed getMovementSpeed;

			private static readonly float fsqrt2 = (float)Math.Sqrt(2);
			private static readonly double dsqrt2 = Math.Sqrt(2);

			private static readonly float fsqrt2d2 = fsqrt2 / 2;
			private static readonly double dsqrt2d2 = dsqrt2 / 2;

			private const float half = 1f / 2;


			/// <summary>
			/// Tests if the distance through the new Previous Node is less than
			/// the old distance, if true, then sets Distance and PreviousNode
			/// </summary>
			/// <param name="newPreviousNode">New possible previous node, will be tested and if its shorter distance, set as previous</param>
			/// <returns>true if the new distance is lower and was set, false if not and was not</returns>
			public bool TestAndSetDistance(Node newPreviousNode) {

				//Little optimization, because there are only two possible distances, either 1 or sqrt(2), 
				// and i always need only half of this distance, i just have two constants from which i choose
				float halfRawDistance =
					(position.X == newPreviousNode.position.X || position.Y == newPreviousNode.position.Y) ? half : fsqrt2d2;
				float newDistance =
					halfRawDistance * getMovementSpeed(Tile) +
					halfRawDistance * getMovementSpeed(newPreviousNode.Tile) +
					newPreviousNode.Distance;

				if (newDistance > Distance) return false;

				Distance = newDistance;
				PreviousNode = newPreviousNode;
				return true;
			}


			public override string ToString() {
				return string.Format("X={0}, Y={1}, Dist={2}, Heur={3}", X, Y, Distance, Heuristic);
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
					Distance = float.MaxValue;
					TestAndSetDistance(previousNode);
				}
				//start node
				else {
					Distance = 0;
				}
			}

		}


		private enum NodeState { Opened, Closed };

		private readonly IMap map;

		public AStar(IMap map) {
			this.map = map;
		}

		/// <summary>
		/// Finds the fastest path through the map from units current possition to target
		/// </summary>
		/// <param name="target">Target coordinates</param>
		/// <returns>List of IntVector2s the unit should pass through</returns>
		public Path FindPath(ITile source, ITile target, CanGoToNeighbour canPassTo, GetMovementSpeed getMovementSpeed) {
			Node targetNode = FindPathInNodes(source,
											target,
											canPassTo,
											getMovementSpeed);

			return MakePath(targetNode, getMovementSpeed);

		}

		public List<ITile> GetTileList(ITile source, 
											ITile target,
											CanGoToNeighbour canPassTo,
											GetMovementSpeed getMovementSpeed) {
			Node targetNode = FindPathInNodes(source,
											target,
											canPassTo,
											getMovementSpeed);

			return MakeTileList(targetNode);
		}

		private Node FindPathInNodes(ITile source,
								ITile target,
								CanGoToNeighbour canPassTo,
								GetMovementSpeed getMovementSpeed) {
			Dictionary<IntVector2, Node> touchedNodes = new Dictionary<IntVector2, Node>();
			IntVector2 startPos = source.MapLocation;

			Node startNode = new Node(position: startPos,
									previousNode: null,
									tile: source,
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
		private void AddNeighbours(
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

		private float Heuristic(IntVector2 srcPoint, IntVector2 target) {
			return IntVector2.Distance(srcPoint, target);
		}

		/// <summary>
		/// Reconstructs the path when given the last Node
		/// </summary>
		/// <param name="target">Last Node of the path</param>
		/// <returns>Path in correct order, from first point to the last point</returns>
		private Path MakePath(Node target, GetMovementSpeed getMovementSpeed) {
			List<Waypoint> reversedWaypoints = new List<Waypoint>();
			//If the source is the target
			if (target.PreviousNode == null) {
				//TODO: Properly calculate time
				reversedWaypoints.Add(new Waypoint(target.Tile.Center3, 0));
				return Path.CreateFrom(reversedWaypoints);
			}

			while (target.PreviousNode != null) {
				Waypoint tileCenter = new Waypoint(target.Tile.Center3, target.Distance);
				reversedWaypoints.Add(tileCenter);

				target = target.PreviousNode;

				reversedWaypoints.Add(GetBorderWaypoint(tileCenter, target, getMovementSpeed));
			}

			reversedWaypoints.Reverse();
			return Path.CreateFrom(reversedWaypoints);
		}


		private List<ITile> MakeTileList(Node target) {
			List<ITile> reversedTileList = new List<ITile>();
			for (;target != null; target = target.PreviousNode) {
				reversedTileList.Add(target.Tile);
			}

			reversedTileList.Reverse();
			return reversedTileList;
		}

		private static Waypoint GetBorderWaypoint(Waypoint followingWaypoint, Node previousNode, GetMovementSpeed getMovementSpeed) {
			Vector3 toWaypoint = (followingWaypoint.Position - previousNode.Tile.Center3) / 2;
			Vector3 borderPosition = previousNode.Tile.Center3 + toWaypoint;
			return new Waypoint(borderPosition,
								previousNode.Distance + getMovementSpeed(previousNode.Tile) * toWaypoint.Length);
		}

	}
}