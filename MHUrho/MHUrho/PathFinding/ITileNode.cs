using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;

namespace MHUrho.PathFinding
{
    public interface ITileNode : INode
    {
		ITile Tile { get; }

		/// <summary>
		/// Gets the position of the edge between this tile and the <paramref name="other"/> tile
		/// </summary>
		/// <param name="other">The other tile to which we want the edge</param>
		/// <returns>The position of the edge connecting the two tiles</returns>
		Vector3 GetEdgePosition(ITileNode other);
	}
}
