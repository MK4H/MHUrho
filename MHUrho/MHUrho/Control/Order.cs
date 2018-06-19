using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Control
{
    public abstract class Order
    {
		public bool Executed { get; set; }

		public bool PlatformOrder { get; protected set; }

		protected Order()
		{
			PlatformOrder = false;
		}
    }
}
