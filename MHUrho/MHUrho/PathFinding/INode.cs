using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.PathFinding
{
	[Flags]
	public enum NodeType {None = 0, Tile = 1, TileEdge = 2, Building = 4}


	public interface INode
    {
		NodeType NodeType { get; }

		Vector3 Position { get; }
    }


	public static class NodeTypeExtensions {
		public static bool IsAnyOfType(this NodeType testedType, NodeType testTypes)
		{
			return (testedType & testTypes) != 0;
		}
	}
}
