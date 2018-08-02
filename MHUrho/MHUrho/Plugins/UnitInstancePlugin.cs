using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;

namespace MHUrho.Plugins
{

	public abstract class UnitInstancePlugin : InstancePlugin {

		public IUnit Unit { get; protected set; }

		protected UnitInstancePlugin(ILevelManager level, IUnit unit) 
			:base(level)
		{
			this.Unit = unit;
		}




		public abstract void OnHit(IEntity other, object userData);
	}
}
