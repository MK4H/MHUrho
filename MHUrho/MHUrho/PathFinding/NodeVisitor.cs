using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.PathFinding
{
	/// <summary>
	/// Default Node visitor where every function just returns false with invalid time, which means the unit cannot pass the edge
	/// </summary>
    public abstract class NodeVisitor : INodeVisitor
    {
		public virtual bool Visit(ITileNode source, ITileNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(ITileNode source, IBuildingNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(ITileNode source, ITileEdgeNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(IBuildingNode source, ITileNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(IBuildingNode source, IBuildingNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(IBuildingNode source, ITileEdgeNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(ITileEdgeNode source, ITileNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(ITileEdgeNode source, IBuildingNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(ITileEdgeNode source, ITileEdgeNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(ITempNode source, ITileNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(ITempNode source, IBuildingNode target, out float time)
		{
			time = -1;
			return false;
		}

		public virtual bool Visit(ITempNode source, ITileEdgeNode target, out float time)
		{
			time = -1;
			return false;
		}
	}
}
