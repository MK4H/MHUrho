using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using Urho;

namespace MHUrho.PathFinding.AStar
{
    public class TempNode : ITempNode, IHashTileHeightObserver {
		public IPathFindAlg Algorithm => map.PathFinding;

		public NodeType NodeType => NodeType.Temp;

		public Vector3 Position { get; private set; }
		public IEnumerable<INode> Neighbours => Enumerable.Empty<INode>();

		readonly ITile containingTile;
		readonly IMap map;

		public TempNode(Vector3 position, IMap map)
		{
			this.Position = position;
			this.map = map;
			containingTile = map.GetContainingTile(Position.XZ());
			map.TileHeightChangeNotifier.WeakRegisterTileHeightObserver(this);							
		}

		public override int GetHashCode()
		{
			return containingTile.MapLocation.GetHashCode();
		}

		public INode CreateEdge(INode target, MovementType movementType)
		{
			throw new NotImplementedException();
		}

		public INode RemoveEdge(INode target)
		{
			throw new NotImplementedException();
		}

		public void Accept(INodeVisitor visitor, INode target, MovementType movementType)
		{
			target.Accept(visitor, this, movementType);
		}

		public void Accept(INodeVisitor visitor, ITileNode source, MovementType movementType)
		{
			visitor.Visit(source, this, movementType);
		}

		public void Accept(INodeVisitor visitor, IBuildingNode source, MovementType movementType)
		{
			visitor.Visit(source, this, movementType);
		}

		public void Accept(INodeVisitor visitor, ITempNode source, MovementType movementType)
		{
			visitor.Visit(source, this, movementType);
		}

		public void TileHeightsChanged(ImmutableHashSet<ITile> tiles)
		{
			if (tiles.Contains(containingTile)) {
				Position = Position.WithY(map.GetHeightAt(Position.XZ2()));
			}
		}
	}
}
