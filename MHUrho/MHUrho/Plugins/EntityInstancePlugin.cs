using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;

namespace MHUrho.Plugins
{
	/// <summary>
	/// Base class for all entity instance plugins.
	/// </summary>
    public abstract class EntityInstancePlugin : InstancePlugin
    {
		/// <summary>
		/// The controlled entity.
		/// </summary>
		public IEntity Entity { get; private set; }

		protected EntityInstancePlugin(ILevelManager level, IEntity entity)
			: base(level)
		{
			this.Entity = entity;
		}
	}
}
