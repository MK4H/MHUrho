using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.PathFinding
{
	public enum NodeType {None = 0, Tile = 1, Building = 2, Temp = 3}


	public interface INode
    {
		NodeType NodeType { get; }

		Vector3 Position { get; }

		IEnumerable<INode> Neighbours { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="movementType"></param>
		/// <returns>Returns this INode for call chaining</returns>
		INode CreateEdge(INode target, MovementType movementType);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <returns>Returns this INode for call chaining</returns>
		INode RemoveEdge(INode target);

		void Accept(INodeVisitor visitor, INode target);

		void Accept(INodeVisitor visitor, ITileNode source);

		void Accept(INodeVisitor visitor, IBuildingNode source);

		void Accept(INodeVisitor visitor, ITempNode source);
	}
}
