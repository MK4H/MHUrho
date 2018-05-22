using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using Urho;

namespace MHUrho.PathFinding
{
    public class EdgeNode : AStarNode
    {
		public TileNode Tile1 { get; private set; }
		public TileNode Tile2 { get; private set; }

		public EdgeNode(TileNode tile1, TileNode tile2, AStar aStar)
			:base(aStar)
		{
			this.Tile1 = tile1;
			this.Tile2 = tile2;
			FixHeight();
		}

		public void FixHeight()
		{
			Position = Map.GetBorderBetweenTiles(Tile1.Tile, Tile2.Tile);
		}

		public override bool IsItThisNode(Vector3 point)
		{
			return point.IsNear(Position, 0.001f);
		}

		public override IEnumerable<AStarNode> GetNeighbours()
		{
			yield return Tile1;
			yield return Tile2;
		}

		public override IEnumerable<Waypoint> GetWaypoints()
		{
			if (previousNode.GetType() == typeof(TileNode)) {
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
			return Tile1;
		}

		public override IEnumerable<Waypoint> GetToNode(AStarNode node)
		{
			throw new NotImplementedException();
		}
	}
}
