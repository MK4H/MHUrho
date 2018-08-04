using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;

namespace MHUrho.Plugins
{

	public abstract class UnitInstancePlugin : EntityInstancePlugin {

		public IUnit Unit { get; private set; }

		protected UnitInstancePlugin(ILevelManager level, IUnit unit) 
			:base(level, unit)
		{
			this.Unit = unit;
		}




		public abstract void OnHit(IEntity other, object userData);
	}
}
