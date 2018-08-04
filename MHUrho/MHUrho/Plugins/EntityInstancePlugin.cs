using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;

namespace MHUrho.Plugins
{
    public abstract class EntityInstancePlugin : InstancePlugin
    {
		public IEntity Entity { get; private set; }

		protected EntityInstancePlugin(ILevelManager level, IEntity entity)
			: base(level)
		{
			this.Entity = entity;
		}
	}
}
