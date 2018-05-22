using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using Urho;

namespace MHUrho.PathFinding
{
    public class TileNode : AStarNode
    {
		public struct TileEdge {

			public Vector3 EdgeCenter;
			public float SourceToCenterDist;
			public float CenterToTargetDist;

			public TileEdge(TileNode source, TileNode target, IMap map)
			{
				EdgeCenter = new Vector3();
				SourceToCenterDist = 0;
				CenterToTargetDist = 0;
				FixHeight(source, target, map);
			}

			public void FixHeight(TileNode source, TileNode target,IMap map)
			{
				EdgeCenter = map.GetBorderBetweenTiles(source.Tile, target.Tile);
				SourceToCenterDist = (EdgeCenter - source.Position).Length;
				CenterToTargetDist = (target.Position - EdgeCenter).Length;
			}

		}

		public IReadOnlyDictionary<TileNode, EdgeNode> EdgeToNeighbour => edgeToNeighbour;
		readonly Dictionary<TileNode, EdgeNode> edgeToNeighbour;
		List<AStarNode> otherNeighbours;

		public ITile Tile { get; private set; }

		public IEnumerable<AStarNode> NodesOnTile => nodesOnThisTile;

		readonly List<AStarNode> nodesOnThisTile;

		public TileNode(ITile tile, AStar aStar) 
			:base(aStar)
		{
			this.Tile = tile;
			this.Position = Tile.Center3;
			nodesOnThisTile = new List<AStarNode>();
			edgeToNeighbour = new Dictionary<TileNode, EdgeNode>();
			otherNeighbours = new List<AStarNode>();
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
				if (!edgeToNeighbour.ContainsKey(tileNeighbour)) {
					EdgeNode edge = new EdgeNode(this, tileNeighbour, AStar);
					edgeToNeighbour.Add(tileNeighbour, edge);
					tileNeighbour.edgeToNeighbour.Add(this, edge);
				}
			}
		}

		public void FixHeights()
		{
			Position = Tile.Center3;
			foreach (var tileNeighbour in edgeToNeighbour.Keys) {
				edgeToNeighbour[tileNeighbour].FixHeight();
			}
		}

		public override IEnumerable<Waypoint> GetWaypoints()
		{
			if (previousNode.GetType() == typeof(EdgeNode)) {
				yield return new Waypoint(Position, Time - previousNode.Time, MovementType.Linear);
			}
			else {
				foreach (var waypoint in previousNode.GetToNode(this)) {
					yield return waypoint;
				}
			}
		}

		public override TileNode GetTileNode()
		{
			return this;
		}

		public override IEnumerable<Waypoint> GetToNode(AStarNode node)
		{
			throw new NotImplementedException();
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

		public override IEnumerable<AStarNode> GetNeighbours()
		{
			foreach (var tileNeighbour in edgeToNeighbour.Keys) {
				yield return tileNeighbour;
			}

			foreach (var otherNeighbour in otherNeighbours) {
				yield return otherNeighbour;
			}
		}
	}
}
