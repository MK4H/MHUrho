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
    public class TempNode : ITempNode, IHashTileHeightObserver	
    {
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


		public INode CreateEdge(INode target, MovementType movementType)
		{
			throw new NotImplementedException();
		}

		public INode RemoveEdge(INode target)
		{
			throw new NotImplementedException();
		}

		public void Accept(INodeVisitor visitor, INode target)
		{
			target.Accept(visitor, this);
		}

		public void Accept(INodeVisitor visitor, ITileNode source)
		{
			visitor.Visit(source, this);
		}

		public void Accept(INodeVisitor visitor, IBuildingNode source)
		{
			visitor.Visit(source, this);
		}

		public void Accept(INodeVisitor visitor, ITempNode source)
		{
			visitor.Visit(source, this);
		}

		public void TileHeightsChanged(ImmutableHashSet<ITile> tiles)
		{
			if (tiles.Contains(containingTile)) {
				Position = Position.WithY(map.GetHeightAt(Position.XZ2()));
			}
		}
	}
}
