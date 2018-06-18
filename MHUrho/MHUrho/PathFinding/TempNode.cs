using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.PathFinding
{
    public class TempNode : ITempNode
    {
		public NodeType NodeType => NodeType.Temp;

		public Vector3 Position { get; private set; }

		public TempNode(Vector3 position)
		{
			this.Position = position;
		}


		public INode CreateEdge(INode target, MovementType movementType)
		{
			throw new NotImplementedException();
		}

		public INode RemoveEdge(INode target)
		{
			throw new NotImplementedException();
		}

		public bool Accept(INodeVisitor visitor, INode target, out float time)
		{
			return target.Accept(visitor, this, out time);
		}

		public bool Accept(INodeVisitor visitor, ITileNode source, out float time)
		{
			throw new InvalidOperationException("TempNode cannot be used as a target, only as a source");
		}

		public bool Accept(INodeVisitor visitor, IBuildingNode source, out float time)
		{
			throw new InvalidOperationException("TempNode cannot be used as a target, only as a source");
		}

		public bool Accept(INodeVisitor visitor, ITileEdgeNode source, out float time)
		{
			throw new InvalidOperationException("TempNode cannot be used as a target, only as a source");
		}

		public bool Accept(INodeVisitor visitor, ITempNode source, out float time)
		{
			throw new InvalidOperationException("TempNode cannot be used as a target, only as a source");
		}

	}
}
