using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.PathFinding
{
	public enum NodeType { Tile, TileEdge, Building}

	public interface INode
    {
		NodeType NodeType { get; }

		Vector3 Position { get; }
    }
}
