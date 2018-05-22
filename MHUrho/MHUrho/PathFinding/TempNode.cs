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
	}
}
