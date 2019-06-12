using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Control;
using MHUrho.DefaultComponents;
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

		readonly HashSet<INode> filledNodes;

		readonly LevelPluginBase level;

		public BFSRoofFormationController(LevelPluginBase levelPlugin,
										IBuildingNode startNode)
		{
			level = levelPlugin;
			buildingNodes = new Queue<INode>();
			lowPrioNodes = new Queue<INode>();
			filledNodes = new HashSet<INode>();

			buildingNodes.Enqueue(startNode);
		}


		public bool MoveToFormation(UnitSelector unit)
		{
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

			return false;
		}

		public bool MoveToFormation(UnitGroup units)
		{
			bool executed = false;
			while (units.IsValid())
			{
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
			if (node != PeekNextNode())
			{
				throw new NotImplementedException("Wrong implementation of GateFormationController, should deque the current node");
			}

			//If we are still searching the first building or buildings connected to it
			bool isHighPrio = buildingNodes.Count != 0;


			foreach (var neib in node.Neighbours)
			{
				if (filledNodes.Contains(neib))
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
			}
		}
	}
}
