using System;
using System.Collections.Generic;
using System.Text;

using Urho;
using MHUrho.Logic;
using Priority_Queue;




namespace MHUrho
{
    class AStar : IPathFindAlg
    {
        private readonly Map map;
        

        private enum NodeState { Opened, Closed };

        private class Node : FastPriorityQueueNode
        {
            private IntVector2 position;

            public NodeState State { get; set; }

            public IntVector2 Position => position;
            
            /// <summary>
            /// COPIES Position with new X coordinate
            /// </summary>
            public int X {
                get
                {
                    return position.X;
                }
                set
                {
                    position = new IntVector2(value, position.Y);
                }
            }
           
            /// <summary>
            /// COPIES Position with new Y coordinate
            /// </summary>
            public int Y {
                get
                {
                    return position.Y;
                }
                set
                {
                    position = new IntVector2(position.X, value);
                }
            }

            /// <summary>
            /// Distance from start to the middle of the tile
            /// </summary>
            public float Distance { get; private set; }

            public float Heuristic { get; private set; }

            public Node PreviousNode { get; private set; }
            
            //Tile at the map[X][Y] coordinates
            private readonly Tile tile;
            private readonly Unit unit;

            private static readonly float fsqrt2 = (float)Math.Sqrt(2);
            private static readonly double dsqrt2 = Math.Sqrt(2);

            private static readonly float fsqrt2d2 = fsqrt2 / 2;
            private static readonly double dsqrt2d2 = dsqrt2 / 2;

            private static readonly float half = 1f / 2;


            /// <summary>
            /// Tests if the distance through the new Previous Node is less than
            /// the old distance, if true, then sets Distance and PreviousNode
            /// </summary>
            /// <param name="newPreviousNode">New possible previous node, will be tested and if its shorter distance, set as previous</param>
            /// <param name="unit">Unit for which the path is calculated, for speed calculation</param>
            /// <returns>true if the new distance is lower and was set, false if not and was not</returns>
            public bool TestAndSetDistance(Node newPreviousNode) {
                
                //Little optimization, because there are only two possible distances, either 1 or sqrt(2), 
                // and i always need only half of this distance, i just have two constants from which i choose
                float halfRawDistance =
                    (position.X == newPreviousNode.position.X || position.Y == newPreviousNode.position.Y) ? half : fsqrt2d2;
                float newDistance =
                    halfRawDistance * unit.MovementSpeed(tile) +
                    halfRawDistance * unit.MovementSpeed(newPreviousNode.tile) + 
                    newPreviousNode.Distance;

                if (newDistance > Distance) return false;

                Distance = newDistance;
                PreviousNode = newPreviousNode;
                return true;
            }


            public override string ToString()
            {
                return string.Format("X={0}, Y={1}, Dist={2}, Heur={3}", X, Y, Distance,Heuristic);
            }

            public Node(
                IntVector2 position,
                Node previousNode,
                Tile tile,
                float heuristic,
                Unit unit,
                NodeState state = NodeState.Opened
                )
            {
                this.position = position;
                PreviousNode = previousNode;
                this.tile = tile;
                this.unit = unit;
                Heuristic = heuristic;
                State = state;
                if (previousNode != null)
                {
                    Distance = float.MaxValue;
                    TestAndSetDistance(previousNode);
                }
                //start node
                else
                {
                    Distance = 0;
                }
            }

        }
        
        /// <summary>
        /// Finds the fastest path through the map from units current possition to target
        /// </summary>
        /// <param name="unit">The unit to find the path for, used for checking speed through tile types</param>
        /// <param name="target">Target coordinates</param>
        /// <returns>List of IntVector2s the unit should pass through</returns>
        public List<IntVector2> FindPath( Unit unit, IntVector2 target)
        {
            
            FastPriorityQueue<Node> PQueue = new FastPriorityQueue<Node>(32);
            Dictionary<IntVector2, Node> TouchedNodes = new Dictionary<IntVector2, Node>();
            IntVector2 Start = unit.Tile.Location;

            // Enque the starting node
            PQueue.Enqueue(
                new Node(
                    position: Start,
                    previousNode: null,
                    tile: unit.Tile,
                    heuristic: Heuristic(Start,target), //I know this is not right but it does not matter
                    unit: unit
                    ), 
                0);
            // Add the starting node to touched nodes, so it does not get enqued again
            TouchedNodes.Add(Start, PQueue.First);

            //Main loop
            while (PQueue.Count != 0)
            {
                Node sourceNode = PQueue.Dequeue();

                //If we hit the target, finish and return path
                if (sourceNode.Position == target)
                {
                    return MakePath(sourceNode);
                }

                //If not finished, add untouched neighbours to the queue and touched nodes
                AddNeighbours(PQueue, TouchedNodes, sourceNode, target, unit);
                
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
            Unit unit)
        {
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    //Dont try adding source node again
                    if (dx == 0 && dy == 0)
                        continue;

                    IntVector2 newPosition = new IntVector2(sourceNode.X + dx, sourceNode.Y + dy);
                    
                    //Check map boundaries
                    if (!map.IsInside(newPosition)) {
                        continue;
                    }

                    //If already opened or closed
                    if (touchedNodes.TryGetValue(newPosition, out Node nextNode))
                    {
                        //Already closed, either not passable or the best path there can be found
                        if (nextNode.State == NodeState.Closed)
                            continue;
                        //if it is closer through the current sourceNode
                        if (nextNode.TestAndSetDistance(sourceNode)) {
                            queue.UpdatePriority(nextNode, nextNode.Distance + nextNode.Heuristic);
                        }
                    }
                    else
                    {
                        // Get the next tile from the map
                        Tile newTile = map.GetTile(newPosition);
                        // Compute the heuristic for the new tile
                        float heuristic = Heuristic(newPosition, target);
                        
                        if (!unit.CanPass(newTile))
                        {
                            //Unit cannot pass this tile
                            touchedNodes.Add(
                                newPosition,
                                new Node(
                                    newPosition,
                                    sourceNode,
                                    newTile,
                                    heuristic,
                                    unit,
                                    NodeState.Closed
                                    )
                                );
                        }
                        else
                        {
                            //Unit can pass through this tile, enqueue it
                            Node newNode = new Node(newPosition, sourceNode, newTile, heuristic,unit);
                            if (queue.Count == queue.MaxSize) {
                                queue.Resize(queue.MaxSize * 2);
                            }
                            queue.Enqueue(newNode, newNode.Distance + newNode.Heuristic);
                            touchedNodes.Add(newNode.Position, newNode);
                        }
                    }
                }
            }
        }
       
        float Heuristic(IntVector2 srcPoint, IntVector2 target)
        {
            return IntVector2.Distance(srcPoint,target);
        }

        /// <summary>
        /// Reconstructs the path when given the last Node
        /// </summary>
        /// <param name="target">Last Node of the path</param>
        /// <returns>Path in correct order, from first point to the last point</returns>
        List<IntVector2> MakePath(Node target)
        {
            List<IntVector2> reversePath = new List<IntVector2>();
            while (target.PreviousNode != null)
            {
                reversePath.Add(target.Position);
                target = target.PreviousNode;
            }
            reversePath.Add(target.Position);
            reversePath.Reverse();
            return reversePath;
        }

        

        public AStar(Map map)
        {
            this.map = map;
        }
    }
}