using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.DefaultComponents;

namespace MHUrho.Control
{
    public class PlatformOrder : Order
    {
		public PlatformOrder()
		{
			PlatformOrder = true;
		}
    }

	public class MoveOrder : PlatformOrder {

		public INode Target { get; protected set; }

		public MoveOrder(INode target)
		{
			this.Target = target;
		}

	}

	public class AttackOrder : PlatformOrder {

		public IEntity Target { get; protected set; }

		public AttackOrder(IEntity target)
		{
			this.Target = target;
		}
	}

	public class ShootOrder : PlatformOrder {
		public IRangeTarget Target { get; protected set; }

		public ShootOrder(IRangeTarget target)
		{
			this.Target = target;
		}
	}
}
