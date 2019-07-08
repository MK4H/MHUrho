using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;

namespace MHUrho.PathFinding
{
    public interface IBuildingNode : INode
    {
		IBuilding Building { get; }

		object Tag { get; }

		bool IsRemoved { get; }

		void Remove();

		void ChangePosition(Vector3 newPosition);
	}
}
