using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Control;
using MHUrho.DefaultComponents;
using MHUrho.Logic;
using MHUrho.PathFinding;
using ShowcasePackage.Levels;

namespace ShowcasePackage.Buildings
{
	class BFSRoofFormationController : IFormationController
	{
		//First tries to move them on top of continuous buildings
		// If not possible, starts moving units to connected ground points
		//  and possibly buildings connected to these ground points

		readonly Queue<INode> buildingNodes;

		//The connected ground points
		readonly Queue<INode> lowPrioNodes;

		readonly HashSet<INode> enqueuedNodes;
		readonly HashSet<UnitType> failedTypes;

		readonly LevelInstancePluginBase level;

		public BFSRoofFormationController(LevelInstancePluginBase levelPlugin,
										IBuildingNode startNode)
		{
			level = levelPlugin;
			buildingNodes = new Queue<INode>();
			lowPrioNodes = new Queue<INode>();
			enqueuedNodes = new HashSet<INode>(startNode.Algorithm.NodeEqualityComparer);
			failedTypes = new HashSet<UnitType>();

			buildingNodes.Enqueue(startNode);
			enqueuedNodes.Add(startNode);
		}


		public bool MoveToFormation(UnitSelector unit)
		{
			if (failedTypes.Contains(unit.Unit.UnitType)) {
				return false;
			}

			INode node;
			if ((node = PeekNextNode()) == null)
			{
				return false;
			}

			if (unit.Order(new MoveOrder(node)))
			{
				DequeNextNode(node);
				return true;
			}

			failedTypes.Add(unit.Unit.UnitType);
			return false;
		}

		public bool MoveToFormation(UnitGroup units)
		{
			bool executed = false;
			for (; units.IsValid(); units.TryMoveNext()) {
				if (MoveToFormation(units.Current))
				{
					executed = true;
				}
			}
			return executed;
		}

		INode PeekNextNode()
		{
			if (buildingNodes.Count != 0)
			{
				return buildingNodes.Peek();
			}

			if (lowPrioNodes.Count != 0)
			{
				return lowPrioNodes.Peek();
			}

			return null;
		}

		void DequeNextNode(INode node)
		{
			//If we are still searching the first building or buildings connected to it
			bool isHighPrio = buildingNodes.Count != 0;

			//Dequeue from the correct queue and check that it is the given node
			if (isHighPrio && node != buildingNodes.Dequeue()) {
				throw new NotImplementedException("Wrong implementation of GateFormationController, should deque the current node");
			}
			else if (!isHighPrio && node != lowPrioNodes.Dequeue()) {
				throw new NotImplementedException("Wrong implementation of GateFormationController, should deque the current node");
			}




			foreach (var neib in node.Neighbours)
			{
				if (enqueuedNodes.Contains(neib))
				{
					continue;
				}

				if (isHighPrio &&
					neib is IBuildingNode buildingNeib &&
					level.IsRoofNode(buildingNeib))
				{
					//The first building or a building connected to it
					buildingNodes.Enqueue(neib);
				}
				else
				{
					//Other ground nodes or nodes connected through them
					lowPrioNodes.Enqueue(neib);
				}

				enqueuedNodes.Add(neib);
			}
		}
	}
}
