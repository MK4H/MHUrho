using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.PathFinding
{
	[Flags]
	public enum NodeType {None = 0, Tile = 1, TileEdge = 2, Building = 4, Temp = 8}


	public interface INode
    {
		NodeType NodeType { get; }

		Vector3 Position { get; }

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

		bool Accept(INodeVisitor visitor, INode target, out float time);

		bool Accept(INodeVisitor visitor, ITileNode source, out float time);

		bool Accept(INodeVisitor visitor, IBuildingNode source, out float time);

		bool Accept(INodeVisitor visitor, ITileEdgeNode source, out float time);

		bool Accept(INodeVisitor visitor, ITempNode source, out float time);
	}


	public static class NodeTypeExtensions {
		public static bool IsAnyOfType(this NodeType testedType, NodeType testTypes)
		{
			return (testedType & testTypes) != 0;
		}
	}
}
